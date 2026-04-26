using LibraryAPI.Models;

namespace LibraryAPI.Repositories.Interfaces;

/// <summary>
/// Contract for all Book database operations.
/// Layer: Repository interface
///
/// Defining an interface lets the service depend on an abstraction rather than the
/// concrete EF Core implementation. This makes unit testing possible (you can swap in a
/// fake) and enforces the rule that services never import DbContext directly.
///
/// All methods are async because EF Core I/O is always network/disk bound — blocking
/// threads on database calls would starve the ASP.NET Core thread pool under load.
/// </summary>
public interface IBookRepository
{
    /// <summary>Returns all books. Returns empty list (not null) when no books exist.</summary>
    Task<List<Book>> GetAllAsync();

    /// <summary>Returns the book with the given id, or null if not found.</summary>
    Task<Book?> GetByIdAsync(int id);

    /// <summary>Stages a new book for insertion. Call SaveChangesAsync to persist.</summary>
    Task AddAsync(Book book);

    /// <summary>Persists all pending changes (add / update / delete) to the database.</summary>
    Task SaveChangesAsync();

    /// <summary>Stages deletion of a book. Call SaveChangesAsync to persist.</summary>
    void Delete(Book book);
}
