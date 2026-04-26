using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.DTOs;

/// <summary>
/// DTO for POST /api/return. Caller identifies the borrow record to close.
/// Layer: DTOs
///
/// We use the BorrowRecord ID rather than BookId + MemberId because a member could
/// theoretically borrow the same book twice (after returning the first time), so
/// a pair of IDs would be ambiguous. The record ID uniquely identifies exactly which
/// borrow event is being returned.
/// </summary>
public class ReturnRequestDto
{
    [Required]
    public int BorrowRecordId { get; set; }
}
