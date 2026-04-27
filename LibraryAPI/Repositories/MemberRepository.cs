using LibraryAPI.Data;
using LibraryAPI.Models;
using LibraryAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LibraryAPI.Repositories;

/// <summary>
/// Concrete EF Core implementation of IMemberRepository.
/// Layer: Repository
///
/// This is the ONLY class allowed to directly use LibraryDbContext for member data.
/// All database access for members goes through this class — services must not
/// import or use DbContext directly.
///
/// GetByEmailAsync supports the email uniqueness check in MemberService.
/// Email comparison is case-insensitive per RFC 5321 (email addresses are case-insensitive).
/// </summary>
public class MemberRepository : IMemberRepository
{
    private readonly LibraryDbContext _context;

    public MemberRepository(LibraryDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Returns all members as a materialized list.
    /// ToListAsync executes the query immediately, preventing enumeration
    /// over a potentially disposed DbContext later.
    /// </summary>
    public async Task<List<Member>> GetAllAsync()
    {
        return await _context.Members.ToListAsync();
    }

    /// <summary>
    /// Looks up a member by primary key. FindAsync checks EF Core's identity cache
    /// before hitting the database, making repeated lookups within a request faster.
    /// Returns null if no member with that id exists.
    /// </summary>
    public async Task<Member?> GetByIdAsync(int id)
    {
        return await _context.Members.FindAsync(id);
    }

    /// <summary>
    /// Finds a member by email address using a case-insensitive comparison.
    /// Used by MemberService to enforce the one-email-per-member business rule.
    /// Returns null if no member has that email.
    ///
    /// ToLower() on both sides handles variations like "User@Example.com" vs "user@example.com"
    /// being treated as the same address.
    /// </summary>
    public async Task<Member?> GetByEmailAsync(string email)
    {
        return await _context.Members
            .FirstOrDefaultAsync(m => m.Email.ToLower() == email.ToLower());
    }

    /// <summary>
    /// Stages a new member for insertion. The INSERT runs on SaveChangesAsync.
    /// </summary>
    public async Task AddAsync(Member member)
    {
        await _context.Members.AddAsync(member);
    }

    /// <summary>
    /// Stages a member for deletion. The DELETE runs on SaveChangesAsync.
    /// Requires a tracked entity — caller must have fetched the member first.
    /// </summary>
    public void Delete(Member member)
    {
        _context.Members.Remove(member);
    }

    /// <summary>
    /// Persists all staged changes to the database in a single transaction.
    /// </summary>
    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}