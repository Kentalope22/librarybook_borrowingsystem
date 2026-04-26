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
/// - DTO mapping (entity → response DTO)
/// - Setting MembershipDate on creation (callers must not supply it)
/// - Email uniqueness enforcement
///
/// NOT responsible for: HTTP status codes, DbContext access.
/// </summary>
public class MemberService : IMemberService
{
    private readonly IMemberRepository _repo;

    public MemberService(IMemberRepository repo)
    {
        _repo = repo;
    }

    public async Task<IEnumerable<MemberResponseDto>> GetAllAsync()
    {
        var members = await _repo.GetAllAsync();
        return members.Select(MapToDto);
    }

    /// <summary>
    /// Returns a single member or throws NotFoundException.
    /// </summary>
    public async Task<MemberResponseDto> GetByIdAsync(int id)
    {
        var member = await _repo.GetByIdAsync(id);
        if (member == null)
            throw new NotFoundException($"Member with id {id} not found");

        return MapToDto(member);
    }

    /// <summary>
    /// Creates a member. MembershipDate is set here to DateTime.UtcNow, not from the
    /// request — callers must not be able to backdate their own membership.
    /// Email uniqueness is checked first to give a clear 409 rather than a DB-level error.
    /// </summary>
    public async Task<MemberResponseDto> CreateAsync(MemberRequestDto dto)
    {
        var existing = await _repo.GetByEmailAsync(dto.Email);
        if (existing != null)
            throw new ConflictException($"A member with email '{dto.Email}' already exists");

        var member = new Member
        {
            FullName = dto.FullName,
            Email = dto.Email,
            MembershipDate = DateTime.UtcNow
        };

        await _repo.AddAsync(member);
        await _repo.SaveChangesAsync();

        return MapToDto(member);
    }

    /// <summary>
    /// Updates a member. If the email is being changed, check that the new email is not
    /// already taken by a different member (comparing ids avoids false positive when the
    /// email is unchanged).
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

    public async Task DeleteAsync(int id)
    {
        var member = await _repo.GetByIdAsync(id);
        if (member == null)
            throw new NotFoundException($"Member with id {id} not found");

        _repo.Delete(member);
        await _repo.SaveChangesAsync();
    }

    private static MemberResponseDto MapToDto(Member member) => new MemberResponseDto
    {
        Id = member.Id,
        FullName = member.FullName,
        Email = member.Email,
        MembershipDate = member.MembershipDate
    };
}
