using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.DTOs;

/// <summary>
/// DTO for POST /api/borrow. Caller supplies which book and which member.
/// Layer: DTOs
///
/// Both IDs are [Required] so the [ApiController] attribute returns 400 automatically
/// if either is missing, before the service is even called.
/// Existence validation (book/member actually in DB) is done in BorrowService because
/// it requires database access — controllers must not touch the DB.
/// </summary>
public class BorrowRequestDto
{
    [Required]
    public int BookId { get; set; }

    [Required]
    public int MemberId { get; set; }
}
