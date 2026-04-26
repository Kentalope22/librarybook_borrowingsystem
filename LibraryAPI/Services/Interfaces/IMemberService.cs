using LibraryAPI.DTOs;

namespace LibraryAPI.Services.Interfaces;

/// <summary>
/// Contract for all Member business operations.
/// Layer: Service interface
/// </summary>
public interface IMemberService
{
    /// <summary>Returns all members.</summary>
    Task<IEnumerable<MemberResponseDto>> GetAllAsync();

    /// <summary>
    /// Returns the member with the given id.
    /// Throws NotFoundException if not found.
    /// </summary>
    Task<MemberResponseDto> GetByIdAsync(int id);

    /// <summary>
    /// Creates a member. Sets MembershipDate = DateTime.UtcNow.
    /// Throws ConflictException if email already exists.
    /// Returns the created member DTO with generated Id.
    /// </summary>
    Task<MemberResponseDto> CreateAsync(MemberRequestDto dto);

    /// <summary>
    /// Updates a member.
    /// Throws NotFoundException if id doesn't exist.
    /// Throws ConflictException if the new email is already used by another member.
    /// Returns the updated member DTO.
    /// </summary>
    Task<MemberResponseDto> UpdateAsync(int id, MemberRequestDto dto);

    /// <summary>
    /// Deletes a member.
    /// Throws NotFoundException if id doesn't exist.
    /// </summary>
    Task DeleteAsync(int id);
}
