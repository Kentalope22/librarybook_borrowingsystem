namespace LibraryAPI.DTOs;

/// <summary>
/// DTO returned by all book GET endpoints and as the body of 201/200 responses after
/// create/update. Layer: DTOs
///
/// Why not return the Book entity directly?
/// Returning raw entities leaks internal fields (RowVersion, navigation properties) and
/// couples the API contract to the database schema. If the schema changes, the API
/// response would silently change too. A dedicated response DTO gives us full control.
/// </summary>
public class BookResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
}
