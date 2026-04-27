using LibraryAPI.DTOs;
using LibraryAPI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LibraryAPI.Controllers;

/// <summary>
/// Handles HTTP requests for the /api/members resource.
/// Layer: Controller
///
/// Responsibilities:
/// - Receive HTTP request data (route params, body)
/// - Call the appropriate IMemberService method
/// - Translate service results into the correct HTTP response
///
/// NOT responsible for: business logic, email uniqueness checks, or database access.
/// Those concerns live in MemberService and MemberRepository.
///
/// [ApiController] enables automatic 400 responses when MemberRequestDto
/// data annotations fail — for example, a missing FullName or an invalid Email format.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MembersController : ControllerBase
{
    private readonly IMemberService _service;

    /// <summary>
    /// IMemberService is injected by the DI container configured in Program.cs.
    /// Constructor injection means the controller declares its dependencies explicitly
    /// and never creates them with new — making the class testable and loosely coupled.
    /// </summary>
    public MembersController(IMemberService service)
    {
        _service = service;
    }

    /// <summary>
    /// GET /api/members
    /// Returns all registered members. Returns an empty array (not 404) when none exist.
    /// </summary>
    /// <returns>200 OK with list of all members.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<MemberResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var members = await _service.GetAllAsync();
        return Ok(members);
    }

    /// <summary>
    /// GET /api/members/{id}
    /// Returns a single member by their id.
    /// </summary>
    /// <param name="id">The member's database id.</param>
    /// <returns>200 OK with the member, or 404 if not found.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(MemberResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var member = await _service.GetByIdAsync(id);
            return Ok(member);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/members
    /// Creates a new library member. [ApiController] returns 400 automatically if
    /// MemberRequestDto annotations fail (missing FullName, invalid email format).
    /// MemberService enforces email uniqueness and returns 409 on duplicate.
    /// MembershipDate is set automatically — callers do not supply it.
    /// </summary>
    /// <param name="dto">The member data to create.</param>
    /// <returns>201 Created with the new member and a Location header.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(MemberResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] MemberRequestDto dto)
    {
        try
        {
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ConflictException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// PUT /api/members/{id}
    /// Updates an existing member's name and/or email.
    /// Returns 409 if the new email is already used by a different member.
    /// </summary>
    /// <param name="id">The id of the member to update.</param>
    /// <param name="dto">The updated member data.</param>
    /// <returns>200 OK with updated member, 404 if not found, 409 if email conflict.</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(MemberResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, [FromBody] MemberRequestDto dto)
    {
        try
        {
            var updated = await _service.UpdateAsync(id, dto);
            return Ok(updated);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// DELETE /api/members/{id}
    /// Removes a member from the system.
    /// </summary>
    /// <param name="id">The id of the member to delete.</param>
    /// <returns>204 No Content on success, 404 if not found.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}