using LibraryAPI.DTOs;
using LibraryAPI.Models;
using LibraryAPI.Repositories.Interfaces;
using LibraryAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LibraryAPI.Services;

/// <summary>
/// Handles all business logic for borrowing and returning books.
/// Layer: Service
///
/// This is the most complex service because it must:
/// 1. Validate book and member existence (404 if missing)
/// 2. Validate availability (409 if no copies)
/// 3. Prevent duplicate borrows (409 if member already has it)
/// 4. Handle concurrent borrow races via optimistic concurrency (409 on conflict)
/// 5. Keep AvailableCopies accurate on both borrow (decrement) and return (increment)
///
/// Optimistic concurrency works because Book.RowVersion is a [Timestamp] column.
/// When EF Core saves the decremented AvailableCopies, it includes
/// WHERE RowVersion = <value_we_read> in the UPDATE. If another request already
/// updated that row, the WHERE matches zero rows → DbUpdateConcurrencyException.
/// We catch it and surface it as a 409 so the client knows to retry.
/// This prevents AvailableCopies from ever going below 0 under concurrent load.
/// </summary>
public class BorrowService : IBorrowService
{
    private readonly IBorrowRepository _borrowRepo;
    private readonly IBookRepository _bookRepo;
    private readonly IMemberRepository _memberRepo;

    public BorrowService(
        IBorrowRepository borrowRepo,
        IBookRepository bookRepo,
        IMemberRepository memberRepo)
    {
        _borrowRepo = borrowRepo;
        _bookRepo = bookRepo;
        _memberRepo = memberRepo;
    }

    public async Task<IEnumerable<BorrowRecordResponseDto>> GetAllAsync()
    {
        var records = await _borrowRepo.GetAllWithDetailsAsync();
        return records.Select(MapToDto);
    }

    /// <summary>
    /// Returns history for a specific member, but first verifies the member exists.
    /// Returning an empty list for a non-existent member would be misleading — a 404
    /// is more honest and helps callers detect typos in the member id.
    /// </summary>
    public async Task<IEnumerable<BorrowRecordResponseDto>> GetByMemberIdAsync(int memberId)
    {
        var member = await _memberRepo.GetByIdAsync(memberId);
        if (member == null)
            throw new NotFoundException($"Member with id {memberId} not found");

        var records = await _borrowRepo.GetByMemberIdAsync(memberId);
        return records.Select(MapToDto);
    }

    /// <summary>
    /// Records a new borrow. Validates existence, availability, and uniqueness, then
    /// decrements AvailableCopies and saves both changes (book update + new record) in
    /// a single SaveChangesAsync call so they are atomic.
    ///
    /// The try/catch around SaveChangesAsync catches DbUpdateConcurrencyException that
    /// EF Core throws when the RowVersion WHERE clause matches zero rows — meaning
    /// another request updated the book between our read and our save.
    /// </summary>
    public async Task<BorrowRecordResponseDto> BorrowBookAsync(BorrowRequestDto dto)
    {
        var book = await _bookRepo.GetByIdAsync(dto.BookId);
        if (book == null)
            throw new NotFoundException($"Book with id {dto.BookId} not found");

        var member = await _memberRepo.GetByIdAsync(dto.MemberId);
        if (member == null)
            throw new NotFoundException($"Member with id {dto.MemberId} not found");

        // We check availability before the lock so the happy path is fast. The
        // concurrency catch below handles the race where two requests both see
        // AvailableCopies = 1 and both try to decrement.
        if (book.AvailableCopies <= 0)
            throw new ConflictException("No available copies");

        // Prevent duplicate open borrows: a member cannot borrow the same book twice
        // without returning the first copy.
        var existingBorrow = await _borrowRepo.GetActiveBorrowAsync(dto.BookId, dto.MemberId);
        if (existingBorrow != null)
            throw new ConflictException("Member already has this book borrowed");

        book.AvailableCopies--;

        var record = new BorrowRecord
        {
            BookId = dto.BookId,
            MemberId = dto.MemberId,
            BorrowDate = DateTime.UtcNow,
            Status = BorrowStatus.Borrowed
        };

        await _borrowRepo.AddAsync(record);

        try
        {
            // SaveChangesAsync persists both the decremented AvailableCopies and the new
            // BorrowRecord together. If the RowVersion check fails (concurrent update),
            // EF Core throws DbUpdateConcurrencyException — we catch it and convert to 409.
            await _borrowRepo.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another request updated the book (AvailableCopies changed) between our
            // read and our save. Surface as a conflict so the client can retry.
            throw new ConflictException("Could not complete borrow due to a conflict — please try again");
        }

        // After save, the record.Id is populated by EF Core. We load the navigation
        // properties from the in-memory variables we already have to avoid an extra DB query.
        record.Book = book;
        record.Member = member;

        return MapToDto(record);
    }

    /// <summary>
    /// Processes a book return. Validates the record exists and is still open, then
    /// increments AvailableCopies and marks the record as Returned.
    /// </summary>
    public async Task<BorrowRecordResponseDto> ReturnBookAsync(ReturnRequestDto dto)
    {
        var record = await _borrowRepo.GetByIdWithDetailsAsync(dto.BorrowRecordId);
        if (record == null)
            throw new NotFoundException($"Borrow record with id {dto.BorrowRecordId} not found");

        // We check Status rather than ReturnDate because Status is the canonical flag.
        // ReturnDate alone could be null for reasons other than "not yet returned" in
        // a more complex model.
        if (record.Status == BorrowStatus.Returned)
            throw new BadRequestException("This book has already been returned");

        record.Status = BorrowStatus.Returned;
        record.ReturnDate = DateTime.UtcNow;
        record.Book.AvailableCopies++;

        await _borrowRepo.SaveChangesAsync();

        return MapToDto(record);
    }

    /// <summary>
    /// Maps a BorrowRecord (with loaded Book and Member navigation props) to a response DTO.
    /// Status is serialized as the enum name ("Borrowed" / "Returned") rather than its
    /// integer value so JSON consumers get a readable string.
    /// </summary>
    private static BorrowRecordResponseDto MapToDto(BorrowRecord record) => new BorrowRecordResponseDto
    {
        Id = record.Id,
        BookId = record.BookId,
        BookTitle = record.Book.Title,
        MemberId = record.MemberId,
        MemberName = record.Member.FullName,
        BorrowDate = record.BorrowDate,
        ReturnDate = record.ReturnDate,
        Status = record.Status.ToString()
    };
}
