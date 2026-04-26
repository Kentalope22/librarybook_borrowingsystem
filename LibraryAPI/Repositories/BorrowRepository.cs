using LibraryAPI.Data;
using LibraryAPI.Models;
using LibraryAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LibraryAPI.Repositories;

/// <summary>
/// Concrete EF Core implementation of IBorrowRepository.
/// Layer: Repository
///
/// All read methods use .Include() to eagerly load the Book and Member navigation
/// properties. Without Include, those properties would be null when accessed outside
/// a tracking context, causing NullReferenceExceptions in the service's MapToDto method.
/// </summary>
public class BorrowRepository : IBorrowRepository
{
    private readonly LibraryDbContext _context;

    public BorrowRepository(LibraryDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Loads all records with their related Book and Member in one query (JOIN).
    /// Needed so BorrowService can populate BookTitle and MemberName in the response DTO.
    /// </summary>
    public async Task<List<BorrowRecord>> GetAllWithDetailsAsync()
    {
        return await _context.BorrowRecords
            .Include(r => r.Book)
            .Include(r => r.Member)
            .ToListAsync();
    }

    /// <summary>
    /// Loads a single record with related entities, or null if the id doesn't exist.
    /// Used by the return operation.
    /// </summary>
    public async Task<BorrowRecord?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.BorrowRecords
            .Include(r => r.Book)
            .Include(r => r.Member)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    /// <summary>
    /// Returns all records for a member so the /borrow/member/{id} history endpoint
    /// can display both past and current borrows.
    /// </summary>
    public async Task<List<BorrowRecord>> GetByMemberIdAsync(int memberId)
    {
        return await _context.BorrowRecords
            .Include(r => r.Book)
            .Include(r => r.Member)
            .Where(r => r.MemberId == memberId)
            .ToListAsync();
    }

    /// <summary>
    /// Returns the open (Borrowed status) record for a given book+member pair, or null.
    /// BorrowService uses this to prevent a member from borrowing the same book twice
    /// without returning it first.
    /// </summary>
    public async Task<BorrowRecord?> GetActiveBorrowAsync(int bookId, int memberId)
    {
        return await _context.BorrowRecords
            .FirstOrDefaultAsync(r => r.BookId == bookId
                                   && r.MemberId == memberId
                                   && r.Status == BorrowStatus.Borrowed);
    }

    public async Task AddAsync(BorrowRecord record)
    {
        await _context.BorrowRecords.AddAsync(record);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
