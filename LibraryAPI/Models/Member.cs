namespace LibraryAPI.Models;

/// <summary>
/// EF Core entity that maps to the Members table.
/// Layer: Models (data layer)
///
/// MembershipDate is set once at creation (DateTime.UtcNow in the service) and never
/// updated through the API — members cannot change when they joined.
/// </summary>
public class Member
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime MembershipDate { get; set; }
}
