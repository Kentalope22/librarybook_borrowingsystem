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
/// - DTO mapping (entity → response DTO)
/// - Business rule validation (AvailableCopies <= TotalCopies)
/// - Cache management (read from cache, write to cache, invalidate on write)
///
/// NOT responsible for: HTTP status codes, DbContext access.
///
/// IMemoryCache is injected here (not in the controller) because caching is a
/// business/performance concern, not an HTTP-transport concern. The cache stores
/// BookResponseDtos so we don't need to re-map entities on every cache hit.
/// </summary>
public class BookService : IBookService
{
    private readonly IBookRepository _repo;
    private readonly IMemoryCache _cache;

    // Constant so the same string is used for both Set and Remove — a typo in one place
    // would otherwise silently create a second cache entry instead of invalidating the first.
    private const string BooksCacheKey = "all_books";

    public BookService(IBookRepository repo, IMemoryCache cache)
    {
        _repo = repo;
        _cache = cache;
    }

    /// <summary>
    /// Returns all books. Serves from IMemoryCache when available to avoid a DB hit
    /// on every request. Cache expires after 5 minutes (TTL) AND is explicitly
    /// invalidated on any write operation so callers never see stale data after a change.
    /// </summary>
    public async Task<IEnumerable<BookResponseDto>> GetAllAsync()
    {
        if (_cache.TryGetValue(BooksCacheKey, out IEnumerable<BookResponseDto>? cached) && cached != null)
            return cached;

        var books = await _repo.GetAllAsync();
        var dtos = books.Select(MapToDto).ToList();

        // TTL is a safety net. Explicit invalidation (InvalidateCache) is the primary
        // mechanism — we don't want to wait 5 minutes for fresh data after a write.
        _cache.Set(BooksCacheKey, dtos, TimeSpan.FromMinutes(5));
        return dtos;
    }

    /// <summary>
    /// Returns a single book or throws NotFoundException.
    /// Does NOT use the list cache to avoid returning a stale single entry when only
    /// one book was updated; the list cache is a separate concern.
    /// </summary>
    public async Task<BookResponseDto> GetByIdAsync(int id)
    {
        var book = await _repo.GetByIdAsync(id);
        if (book == null)
            throw new NotFoundException($"Book with id {id} not found");

        return MapToDto(book);
    }

    /// <summary>
    /// Creates a new book after validating the AvailableCopies <= TotalCopies business rule.
    /// Invalidates the cache because the cached list no longer represents current data.
    /// </summary>
    public async Task<BookResponseDto> CreateAsync(BookRequestDto dto)
    {
        // Service-level business rule: you can't have more available copies than total copies.
        // The controller annotation only ensures AvailableCopies >= 0 and TotalCopies >= 1;
        // this cross-field rule must be checked here.
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

        InvalidateCache();
        return MapToDto(book);
    }

    /// <summary>
    /// Updates an existing book. Both existence and business-rule checks happen before any
    /// write. Cache is invalidated after the successful save.
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
    /// Deletes a book. Invalidates the cache so the list endpoint reflects the removal.
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
    /// Removes the all-books cache entry. Called after any write operation.
    /// Must be called AFTER SaveChangesAsync succeeds — we never want to serve stale
    /// data, but we also don't want to invalidate if the write failed.
    /// </summary>
    private void InvalidateCache()
    {
        _cache.Remove(BooksCacheKey);
    }

    /// <summary>
    /// Converts a Book entity to a BookResponseDto.
    /// Kept private in the service — mapping is a service concern, not a controller concern.
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
