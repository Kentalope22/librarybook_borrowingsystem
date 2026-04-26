namespace LibraryAPI.Models;

/// <summary>
/// Represents the current state of a borrow record.
/// Borrowed = the book is still out; Returned = the book has been brought back.
/// Kept as a separate file so it can be referenced by BorrowRecord without circular file
/// dependencies and is easy to locate.
/// </summary>
public enum BorrowStatus
{
    Borrowed,
    Returned
}
