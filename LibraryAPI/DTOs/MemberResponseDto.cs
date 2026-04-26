namespace LibraryAPI.DTOs;

/// <summary>
/// DTO returned by all member GET endpoints and after create/update.
/// Layer: DTOs
///
/// Exposes MembershipDate (set at creation) so clients can display when a member joined.
/// The entity itself has no extra sensitive fields in this project, but the DTO pattern
/// ensures we keep the entity-to-API contract explicit and changeable independently.
/// </summary>
public class MemberResponseDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime MembershipDate { get; set; }
}
