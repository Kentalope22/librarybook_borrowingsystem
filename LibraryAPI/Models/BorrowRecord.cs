namespace LibraryAPI.Models;

/// <summary>
/// EF Core entity that maps to the BorrowRecords table.
/// Layer: Models (data layer)
///
/// Each row represents one borrow event. ReturnDate is nullable because it is only
/// set when the book is actually returned. Status lets us quickly check whether a
/// record is still active without needing to evaluate ReturnDate.
///
/// Navigation properties (Book, Member) allow EF Core to join related rows in a single
/// query via .Include(), so the response DTO can include BookTitle and MemberName.
/// </summary>
public class BorrowRecord
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public int MemberId { get; set; }
    public DateTime BorrowDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public BorrowStatus Status { get; set; }

    // Navigation properties — used by EF Core for Include() queries
    public Book Book { get; set; } = null!;
    public Member Member { get; set; } = null!;
}
