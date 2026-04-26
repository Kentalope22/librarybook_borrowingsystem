using LibraryAPI.DTOs;
using LibraryAPI.Models;
using LibraryAPI.Repositories.Interfaces;
using LibraryAPI.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace LibraryAPI.Services;

/// <summary>
/// Handles all business logic for Book operations.
/// Layer: Service
///
/// Responsible for:
/// - DTO mapping (Book entity → BookResponseDto)
/// - Business rule validation (AvailableCopies must not exceed TotalCopies)
/// - Cache management: reading from cache, populating cache, invalidating on writes
///
/// NOT responsible for: HTTP status codes or direct database access.
///
/// Why cache here and not in the controller?
/// Caching is a performance/business concern, not an HTTP-transport concern.
/// The controller should not know where data comes from — only that it got data.
/// </summary>
public class BookService : IBookService
{
    private readonly IBookRepository _repo;
    private readonly IMemoryCache _cache;

    // A constant key prevents typos that would silently create a second cache entry
    // instead of hitting the one we intend.
    private const string BooksCacheKey = "all_books";

    public BookService(IBookRepository repo, IMemoryCache cache)
    {
        _repo = repo;
        _cache = cache;
    }

    /// <summary>
    /// Returns all books. Serves from IMemoryCache on subsequent calls to avoid
    /// a database round-trip on every request.
    ///
    /// Cache strategy:
    /// - TTL of 5 minutes as a safety net (in case invalidation is missed)
    /// - Explicit invalidation on every write (create, update, delete) as the
    ///   primary freshness mechanism — we don't want to wait 5 minutes after a change
    /// </summary>
    public async Task<IEnumerable<BookResponseDto>> GetAllAsync()
    {
        // Return cached list if available
        if (_cache.TryGetValue(BooksCacheKey, out IEnumerable<BookResponseDto>? cached) && cached != null)
            return cached;

        // Cache miss — query database, map to DTOs, store in cache
        var books = await _repo.GetAllAsync();
        var dtos = books.Select(MapToDto).ToList();

        _cache.Set(BooksCacheKey, dtos, TimeSpan.FromMinutes(5));
        return dtos;
    }

    /// <summary>
    /// Returns a single book by id. Does not use the list cache because we want
    /// accurate per-book data even if the list cache is slightly stale.
    /// Throws NotFoundException if the id does not exist.
    /// </summary>
    public async Task<BookResponseDto> GetByIdAsync(int id)
    {
        var book = await _repo.GetByIdAsync(id);
        if (book == null)
            throw new NotFoundException($"Book with id {id} not found");

        return MapToDto(book);
    }

    /// <summary>
    /// Creates a new book after enforcing the AvailableCopies <= TotalCopies business rule.
    /// Invalidates the list cache so the next GET /api/books reflects the new book.
    /// </summary>
    public async Task<BookResponseDto> CreateAsync(BookRequestDto dto)
    {
        // Cross-field business rule — cannot be expressed with a single data annotation
        if (dto.AvailableCopies > dto.TotalCopies)
            throw new BadRequestException("AvailableCopies cannot exceed TotalCopies");

        var book = new Book
        {
            Title = dto.Title,
            Author = dto.Author,
            ISBN = dto.ISBN,
            TotalCopies = dto.TotalCopies,
            AvailableCopies = dto.AvailableCopies
        };

        await _repo.AddAsync(book);
        await _repo.SaveChangesAsync();

        // Invalidate AFTER save succeeds — never invalidate if the write failed
        InvalidateCache();
        return MapToDto(book);
    }

    /// <summary>
    /// Updates an existing book. Enforces business rules before writing.
    /// Invalidates the cache so the list reflects the updated data immediately.
    /// </summary>
    public async Task<BookResponseDto> UpdateAsync(int id, BookRequestDto dto)
    {
        var book = await _repo.GetByIdAsync(id);
        if (book == null)
            throw new NotFoundException($"Book with id {id} not found");

        if (dto.AvailableCopies > dto.TotalCopies)
            throw new BadRequestException("AvailableCopies cannot exceed TotalCopies");

        book.Title = dto.Title;
        book.Author = dto.Author;
        book.ISBN = dto.ISBN;
        book.TotalCopies = dto.TotalCopies;
        book.AvailableCopies = dto.AvailableCopies;

        await _repo.SaveChangesAsync();

        InvalidateCache();
        return MapToDto(book);
    }

    /// <summary>
    /// Deletes a book. Invalidates the cache so the deleted book no longer appears
    /// in subsequent GET /api/books responses.
    /// </summary>
    public async Task DeleteAsync(int id)
    {
        var book = await _repo.GetByIdAsync(id);
        if (book == null)
            throw new NotFoundException($"Book with id {id} not found");

        _repo.Delete(book);
        await _repo.SaveChangesAsync();

        InvalidateCache();
    }

    /// <summary>
    /// Removes the cached book list. Called after any successful write operation.
    /// The next call to GetAllAsync will query the database and repopulate the cache.
    /// </summary>
    private void InvalidateCache()
    {
        _cache.Remove(BooksCacheKey);
    }

    /// <summary>
    /// Maps a Book entity to a BookResponseDto.
    /// Private to the service — mapping is a service concern, not a controller concern.
    /// </summary>
    private static BookResponseDto MapToDto(Book book) => new BookResponseDto
    {
        Id = book.Id,
        Title = book.Title,
        Author = book.Author,
        ISBN = book.ISBN,
        TotalCopies = book.TotalCopies,
        AvailableCopies = book.AvailableCopies
    };
}
