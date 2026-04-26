using LibraryAPI.Models;

namespace LibraryAPI.Repositories.Interfaces;

/// <summary>
/// Contract for all BorrowRecord database operations.
/// Layer: Repository interface
///
/// GetAllWithDetailsAsync and GetByMemberIdAsync use EF Core .Include() to eagerly
/// load the Book and Member navigation properties so the service can build
/// BorrowRecordResponseDto without extra round-trips.
/// </summary>
public interface IBorrowRepository
{
    /// <summary>
    /// Returns all borrow records with their related Book and Member loaded.
    /// Returns empty list when none exist.
    /// </summary>
    Task<List<BorrowRecord>> GetAllWithDetailsAsync();

    /// <summary>
    /// Returns the borrow record with the given id plus related Book and Member,
    /// or null if not found.
    /// </summary>
    Task<BorrowRecord?> GetByIdWithDetailsAsync(int id);

    /// <summary>
    /// Returns all borrow records for a specific member with Book and Member loaded.
    /// Returns empty list when none exist.
    /// </summary>
    Task<List<BorrowRecord>> GetByMemberIdAsync(int memberId);

    /// <summary>
    /// Returns any active (Borrowed status) record where the given member currently
    /// has the given book, or null if no such record exists.
    /// Used to detect duplicate-borrow attempts.
    /// </summary>
    Task<BorrowRecord?> GetActiveBorrowAsync(int bookId, int memberId);

    /// <summary>Stages a new borrow record for insertion. Call SaveChangesAsync to persist.</summary>
    Task AddAsync(BorrowRecord record);

    /// <summary>Persists all pending changes to the database.</summary>
    Task SaveChangesAsync();
}
