using LibraryAPI.DTOs;
using LibraryAPI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LibraryAPI.Controllers;

/// <summary>
/// Handles HTTP requests for the /api/members resource.
/// Layer: Controller
///
/// Same structure as BooksController: receive request → call service → translate to HTTP.
/// No business logic, no database calls, no new-ing up services.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MembersController : ControllerBase
{
    private readonly IMemberService _service;

    public MembersController(IMemberService service)
    {
        _service = service;
    }

    /// <summary>
    /// GET /api/members
    /// Returns all members. Returns empty array when none exist.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var members = await _service.GetAllAsync();
        return Ok(members);
    }

    /// <summary>
    /// GET /api/members/{id}
    /// Returns a single member or 404.
    /// </summary>
    [HttpGet("{id}")]
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
    /// Creates a member. 400 on annotation failure, 409 on duplicate email.
    /// Returns 201 with Location header pointing to the new member.
    /// </summary>
    [HttpPost]
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
    /// Updates a member's name and/or email. 404 if not found, 409 if email conflict.
    /// </summary>
    [HttpPut("{id}")]
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
    /// Deletes a member. Returns 204 on success, 404 if not found.
    /// </summary>
    [HttpDelete("{id}")]
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
