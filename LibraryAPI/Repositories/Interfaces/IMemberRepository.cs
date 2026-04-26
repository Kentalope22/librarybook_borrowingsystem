using LibraryAPI.Models;

namespace LibraryAPI.Repositories.Interfaces;

/// <summary>
/// Contract for all Member database operations.
/// Layer: Repository interface
/// </summary>
public interface IMemberRepository
{
    /// <summary>Returns all members. Returns empty list (not null) when none exist.</summary>
    Task<List<Member>> GetAllAsync();

    /// <summary>Returns the member with the given id, or null if not found.</summary>
    Task<Member?> GetByIdAsync(int id);

    /// <summary>Returns a member with the given email, or null if none exists.</summary>
    Task<Member?> GetByEmailAsync(string email);

    /// <summary>Stages a new member for insertion. Call SaveChangesAsync to persist.</summary>
    Task AddAsync(Member member);

    /// <summary>Persists all pending changes to the database.</summary>
    Task SaveChangesAsync();

    /// <summary>Stages deletion of a member. Call SaveChangesAsync to persist.</summary>
    void Delete(Member member);
}
