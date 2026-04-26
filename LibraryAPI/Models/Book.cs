using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.Models;

/// <summary>
/// EF Core entity that maps to the Books table.
/// Layer: Models (data layer)
///
/// AvailableCopies tracks how many copies can currently be borrowed.
/// TotalCopies is the physical count owned by the library — it never changes on borrow/return.
///
/// RowVersion enables optimistic concurrency: EF Core adds a WHERE clause on this column
/// when saving changes, so two concurrent borrows of the last copy will produce a
/// DbUpdateConcurrencyException instead of silently decrementing below zero.
/// </summary>
public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }

    /// <summary>
    /// Concurrency token managed by EF Core. Updated automatically by the database on every
    /// write. If the row was modified between our read and our SaveChanges call, EF Core
    /// throws DbUpdateConcurrencyException, which BorrowService catches to prevent
    /// AvailableCopies going negative.
    /// </summary>
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
