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
/// This is the most complex service because it must coordinate across
/// three repositories (Book, Member, BorrowRecord) and handle:
///
/// 1. Existence validation — 404 if book or member not found
/// 2. Availability validation — 409 if no copies available
/// 3. Duplicate borrow prevention — 409 if member already has this book
/// 4. Concurrent borrow race handling — 409 via optimistic concurrency
/// 5. AvailableCopies accuracy — decrement on borrow, increment on return
///
/// How concurrency is handled:
/// Book has a [Timestamp] RowVersion column. When EF Core saves AvailableCopies--,
/// it adds WHERE RowVersion = original_value to the UPDATE. If another request
/// already updated the row (changing RowVersion), EF Core throws
/// DbUpdateConcurrencyException — we catch it and surface it as 409.
/// This guarantees AvailableCopies never drops below 0 under concurrent load.
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

    /// <summary>
    /// Returns all borrow records with book and member details included.
    /// </summary>
    public async Task<IEnumerable<BorrowRecordResponseDto>> GetAllAsync()
    {
        var records = await _borrowRepo.GetAllWithDetailsAsync();
        return records.Select(MapToDto);
    }

    /// <summary>
    /// Returns borrow history for a specific member.
    /// Verifies the member exists first — returning an empty list for a
    /// non-existent member would be misleading to the caller.
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
    /// Records a new borrow. Validates all business rules, decrements AvailableCopies,
    /// and saves both changes atomically in a single SaveChangesAsync call.
    ///
    /// The try/catch around SaveChangesAsync handles the race condition where two
    /// requests both read AvailableCopies = 1, both decrement to 0, and both try to save.
    /// Only one succeeds — the other gets DbUpdateConcurrencyException → 409.
    /// </summary>
    public async Task<BorrowRecordResponseDto> BorrowBookAsync(BorrowRequestDto dto)
    {
        var book = await _bookRepo.GetByIdAsync(dto.BookId);
        if (book == null)
            throw new NotFoundException($"Book with id {dto.BookId} not found");

        var member = await _memberRepo.GetByIdAsync(dto.MemberId);
        if (member == null)
            throw new NotFoundException($"Member with id {dto.MemberId} not found");

        // Check availability before the save — fast path for the common case
        if (book.AvailableCopies <= 0)
            throw new ConflictException("No available copies");

        // Prevent a member from borrowing the same book twice without returning it
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
            // Single SaveChangesAsync persists both the AvailableCopies decrement
            // and the new BorrowRecord atomically.
            await _borrowRepo.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another concurrent request updated the book row between our read and save.
            // Surface as 409 so the client knows to retry.
            throw new ConflictException("Could not complete borrow due to a conflict — please try again");
        }

        // Populate navigation properties from already-loaded objects to avoid an extra DB round-trip
        record.Book = book;
        record.Member = member;

        return MapToDto(record);
    }

    /// <summary>
    /// Processes a book return. Marks the record as Returned, sets ReturnDate,
    /// and increments AvailableCopies on the book.
    /// Throws BadRequestException if the record is already returned.
    /// </summary>
    public async Task<BorrowRecordResponseDto> ReturnBookAsync(ReturnRequestDto dto)
    {
        var record = await _borrowRepo.GetByIdWithDetailsAsync(dto.BorrowRecordId);
        if (record == null)
            throw new NotFoundException($"Borrow record with id {dto.BorrowRecordId} not found");

        // Status is the canonical flag — not ReturnDate — because Status is explicit and
        // unambiguous even if the model is extended later.
        if (record.Status == BorrowStatus.Returned)
            throw new BadRequestException("This book has already been returned");

        record.Status = BorrowStatus.Returned;
        record.ReturnDate = DateTime.UtcNow;
        record.Book.AvailableCopies++;

        await _borrowRepo.SaveChangesAsync();

        return MapToDto(record);
    }

    /// <summary>
    /// Maps a BorrowRecord (with loaded navigation properties) to a response DTO.
    /// Status is serialized as the enum name ("Borrowed" / "Returned") so clients
    /// receive a readable string instead of an integer.
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