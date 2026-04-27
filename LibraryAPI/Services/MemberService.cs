using LibraryAPI.DTOs;
using LibraryAPI.Models;
using LibraryAPI.Repositories.Interfaces;
using LibraryAPI.Services.Interfaces;

namespace LibraryAPI.Services;

/// <summary>
/// Handles all business logic for Member operations.
/// Layer: Service
///
/// Responsible for:
/// - DTO mapping (Member entity → MemberResponseDto)
/// - Setting MembershipDate on creation (callers must not supply it)
/// - Enforcing email uniqueness across all members
///
/// NOT responsible for: HTTP status codes or direct database access.
///
/// Why enforce email uniqueness here instead of the database?
/// The in-memory database does not support unique constraints natively.
/// Even with a real DB, checking here first lets us return a clear 409 message
/// rather than a raw database constraint violation error.
/// </summary>
public class MemberService : IMemberService
{
    private readonly IMemberRepository _repo;

    public MemberService(IMemberRepository repo)
    {
        _repo = repo;
    }

    /// <summary>
    /// Returns all members mapped to response DTOs.
    /// Returns an empty list (not an exception) when no members exist.
    /// </summary>
    public async Task<IEnumerable<MemberResponseDto>> GetAllAsync()
    {
        var members = await _repo.GetAllAsync();
        return members.Select(MapToDto);
    }

    /// <summary>
    /// Returns a single member by id, or throws NotFoundException.
    /// Returning null would force every caller to null-check — exceptions
    /// let the controller handle it in one place.
    /// </summary>
    public async Task<MemberResponseDto> GetByIdAsync(int id)
    {
        var member = await _repo.GetByIdAsync(id);
        if (member == null)
            throw new NotFoundException($"Member with id {id} not found");

        return MapToDto(member);
    }

    /// <summary>
    /// Creates a new member. Enforces email uniqueness before writing.
    /// MembershipDate is always set to the current UTC time — callers cannot
    /// backdate their own membership.
    /// </summary>
    public async Task<MemberResponseDto> CreateAsync(MemberRequestDto dto)
    {
        // Check uniqueness before insert to give a clear 409 message
        var existing = await _repo.GetByEmailAsync(dto.Email);
        if (existing != null)
            throw new ConflictException($"A member with email '{dto.Email}' already exists");

        var member = new Member
        {
            FullName = dto.FullName,
            Email = dto.Email,
            MembershipDate = DateTime.UtcNow  // Set here — not from the request
        };

        await _repo.AddAsync(member);
        await _repo.SaveChangesAsync();

        return MapToDto(member);
    }

    /// <summary>
    /// Updates a member's name and email. If the email is changing, verifies
    /// the new email is not already taken by a different member.
    /// Comparing ids (emailOwner.Id != id) prevents a false positive when the
    /// member submits their own existing email unchanged.
    /// </summary>
    public async Task<MemberResponseDto> UpdateAsync(int id, MemberRequestDto dto)
    {
        var member = await _repo.GetByIdAsync(id);
        if (member == null)
            throw new NotFoundException($"Member with id {id} not found");

        var emailOwner = await _repo.GetByEmailAsync(dto.Email);
        if (emailOwner != null && emailOwner.Id != id)
            throw new ConflictException($"Email '{dto.Email}' is already used by another member");

        member.FullName = dto.FullName;
        member.Email = dto.Email;

        await _repo.SaveChangesAsync();

        return MapToDto(member);
    }

    /// <summary>
    /// Deletes a member from the system. Throws NotFoundException if the id is invalid.
    /// </summary>
    public async Task DeleteAsync(int id)
    {
        var member = await _repo.GetByIdAsync(id);
        if (member == null)
            throw new NotFoundException($"Member with id {id} not found");

        _repo.Delete(member);
        await _repo.SaveChangesAsync();
    }

    /// <summary>
    /// Maps a Member entity to a MemberResponseDto.
    /// Private to the service — controllers should never do entity-to-DTO mapping.
    /// </summary>
    private static MemberResponseDto MapToDto(Member member) => new MemberResponseDto
    {
        Id = member.Id,
        FullName = member.FullName,
        Email = member.Email,
        MembershipDate = member.MembershipDate
    };
}