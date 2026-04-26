using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.DTOs;

/// <summary>
/// DTO used for POST /api/books (create) and PUT /api/books/{id} (update).
/// Layer: DTOs
///
/// Why a separate DTO instead of the Book entity directly?
/// 1. We never let the client set the auto-generated Id.
/// 2. Data annotations here enforce HTTP-level validation; the [ApiController] attribute
///    on the controller automatically returns 400 if any annotation fails, so no manual
///    ModelState checks are needed.
/// 3. The entity has a RowVersion field that callers must never touch — keeping it out
///    of the DTO prevents accidental overwrites.
/// </summary>
public class BookRequestDto
{
    [Required(ErrorMessage = "Title is required")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Author is required")]
    public string Author { get; set; } = string.Empty;

    [Required(ErrorMessage = "ISBN is required")]
    public string ISBN { get; set; } = string.Empty;

    /// <summary>
    /// Must be at least 1 — a book with zero copies makes no sense.
    /// Checked here via annotation and again in the service for the
    /// AvailableCopies <= TotalCopies business rule.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "TotalCopies must be at least 1")]
    public int TotalCopies { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "AvailableCopies cannot be negative")]
    public int AvailableCopies { get; set; }
}
