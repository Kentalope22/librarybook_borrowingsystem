using LibraryAPI.Data;
using LibraryAPI.Models;
using LibraryAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LibraryAPI.Repositories;

/// <summary>
/// Concrete EF Core implementation of IMemberRepository.
/// Layer: Repository
///
/// GetByEmailAsync enables the service-layer uniqueness check for member emails.
/// The comparison is case-insensitive (ToLower) because email addresses are
/// case-insensitive by RFC 5321 standard.
/// </summary>
public class MemberRepository : IMemberRepository
{
    private readonly LibraryDbContext _context;

    public MemberRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task<List<Member>> GetAllAsync()
    {
        return await _context.Members.ToListAsync();
    }

    public async Task<Member?> GetByIdAsync(int id)
    {
        return await _context.Members.FindAsync(id);
    }

    /// <summary>
    /// Used to enforce email uniqueness. Compares lowercased email to handle
    /// variations like "User@Example.com" vs "user@example.com".
    /// </summary>
    public async Task<Member?> GetByEmailAsync(string email)
    {
        return await _context.Members
            .FirstOrDefaultAsync(m => m.Email.ToLower() == email.ToLower());
    }

    public async Task AddAsync(Member member)
    {
        await _context.Members.AddAsync(member);
    }

    public void Delete(Member member)
    {
        _context.Members.Remove(member);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
