using System.ComponentModel.DataAnnotations;

namespace LibraryAPI.DTOs;

/// <summary>
/// DTO used for POST /api/members (create) and PUT /api/members/{id} (update).
/// Layer: DTOs
///
/// MembershipDate is intentionally absent — it is set by the service to DateTime.UtcNow
/// so callers cannot fake or backdate their join date.
/// [EmailAddress] validates format at the HTTP boundary; uniqueness is checked in the
/// service because it requires a database query.
/// </summary>
public class MemberRequestDto
{
    [Required(ErrorMessage = "FullName is required")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Email must be a valid email address")]
    public string Email { get; set; } = string.Empty;
}
