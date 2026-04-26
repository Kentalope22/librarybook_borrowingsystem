using LibraryAPI.DTOs;

namespace LibraryAPI.Services.Interfaces;

/// <summary>
/// Contract for all Book business operations.
/// Layer: Service interface
///
/// Controllers depend on this interface, never on BookService directly.
/// This means we could swap in a mock during testing without changing controllers.
/// </summary>
public interface IBookService
{
    /// <summary>Returns all books (from cache after first call).</summary>
    Task<IEnumerable<BookResponseDto>> GetAllAsync();

    /// <summary>
    /// Returns the book with the given id.
    /// Throws NotFoundException if id doesn't exist.
    /// </summary>
    Task<BookResponseDto> GetByIdAsync(int id);

    /// <summary>
    /// Creates a book. Invalidates the books cache.
    /// Throws BadRequestException if AvailableCopies > TotalCopies.
    /// Returns the created book DTO with its generated Id.
    /// </summary>
    Task<BookResponseDto> CreateAsync(BookRequestDto dto);

    /// <summary>
    /// Updates a book. Invalidates the books cache.
    /// Throws NotFoundException if id doesn't exist.
    /// Throws BadRequestException if AvailableCopies > TotalCopies.
    /// Returns the updated book DTO.
    /// </summary>
    Task<BookResponseDto> UpdateAsync(int id, BookRequestDto dto);

    /// <summary>
    /// Deletes a book. Invalidates the books cache.
    /// Throws NotFoundException if id doesn't exist.
    /// </summary>
    Task DeleteAsync(int id);
}
