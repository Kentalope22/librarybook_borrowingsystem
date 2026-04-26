using LibraryAPI.Data;
using LibraryAPI.Models;
using LibraryAPI.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LibraryAPI.Repositories;

/// <summary>
/// Concrete EF Core implementation of IBookRepository.
/// Layer: Repository
///
/// This is the ONLY class allowed to directly use LibraryDbContext for book data.
/// All database access for books goes through this class — services must not import DbContext.
///
/// The repository receives a scoped DbContext from DI. Because both the repository
/// and DbContext are Scoped, they share the same instance within one HTTP request,
/// which keeps change tracking consistent.
/// </summary>
public class BookRepository : IBookRepository
{
    private readonly LibraryDbContext _context;

    public BookRepository(LibraryDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Returns all books from the database as a materialized list.
    /// ToListAsync executes the query immediately so the caller does not
    /// enumerate a query over a disposed DbContext.
    /// </summary>
    public async Task<List<Book>> GetAllAsync()
    {
        return await _context.Books.ToListAsync();
    }

    /// <summary>
    /// Looks up a book by primary key. FindAsync checks the EF Core identity cache
    /// before hitting the database, making repeated lookups within a request faster
    /// than FirstOrDefaultAsync would be.
    /// Returns null if no book with that id exists.
    /// </summary>
    public async Task<Book?> GetByIdAsync(int id)
    {
        return await _context.Books.FindAsync(id);
    }

    /// <summary>
    /// Stages a new book for insertion. The actual INSERT runs on SaveChangesAsync.
    /// Grouping multiple changes before saving lets them execute in one transaction.
    /// </summary>
    public async Task AddAsync(Book book)
    {
        await _context.Books.AddAsync(book);
    }

    /// <summary>
    /// Stages a book for deletion. The DELETE runs on SaveChangesAsync.
    /// The caller must have already fetched the book — Remove needs a tracked entity.
    /// </summary>
    public void Delete(Book book)
    {
        _context.Books.Remove(book);
    }

    /// <summary>
    /// Persists all staged changes to the database in a single transaction.
    /// Must be called after AddAsync or Delete for changes to take effect.
    /// </summary>
    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
