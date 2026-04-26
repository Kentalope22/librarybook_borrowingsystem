namespace LibraryAPI.DTOs;

/// <summary>
/// DTO returned by all borrow-record endpoints (GET /api/borrow, GET /api/borrow/member/{id},
/// POST /api/borrow, POST /api/return). Layer: DTOs
///
/// Includes BookTitle and MemberName as denormalized strings so the client doesn't need
/// to make extra requests to look up those names. These are populated from the navigation
/// properties (Book and Member) loaded via EF Core .Include() in the repository.
///
/// Status is a string ("Borrowed" / "Returned") rather than the enum int, so JSON
/// consumers get a readable value without having to know our internal enum numbering.
/// </summary>
public class BorrowRecordResponseDto
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public int MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public DateTime BorrowDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public string Status { get; set; } = string.Empty;
}
