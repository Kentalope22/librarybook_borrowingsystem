using LibraryAPI.Data;
using LibraryAPI.Models;
using LibraryAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LibraryAPI.Repositories;

/// <summary>
/// Concrete EF Core implementation of IBookRepository.
/// Layer: Repository
///
/// This is the ONLY class in the project allowed to use LibraryDbContext directly.
/// Services must not import DbContext — all DB access goes through repository interfaces.
///
/// Constructor injection receives the scoped DbContext provided by DI. Using scoped
/// lifetime for both the repository and the DbContext means each HTTP request gets its
/// own context instance, preventing cross-request state contamination.
/// </summary>
public class BookRepository : IBookRepository
{
    private readonly LibraryDbContext _context;

    public BookRepository(LibraryDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Returns all books as a list. ToListAsync materializes the query immediately so the
    /// caller doesn't accidentally enumerate over a disposed context later.
    /// </summary>
    public async Task<List<Book>> GetAllAsync()
    {
        return await _context.Books.ToListAsync();
    }

    /// <summary>
    /// FindAsync uses the primary-key cache before hitting the DB, making single-record
    /// lookups slightly faster than FirstOrDefaultAsync.
    /// </summary>
    public async Task<Book?> GetByIdAsync(int id)
    {
        return await _context.Books.FindAsync(id);
    }

    /// <summary>
    /// AddAsync only stages the entity — the INSERT happens on SaveChangesAsync.
    /// This lets the caller do multiple changes in one transaction.
    /// </summary>
    public async Task AddAsync(Book book)
    {
        await _context.Books.AddAsync(book);
    }

    public void Delete(Book book)
    {
        _context.Books.Remove(book);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
