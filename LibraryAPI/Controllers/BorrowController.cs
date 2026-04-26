using LibraryAPI.DTOs;
using LibraryAPI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LibraryAPI.Controllers;

/// <summary>
/// Handles HTTP requests for the /api/borrow resource, including the return sub-route.
/// Layer: Controller
///
/// Routes in this controller:
///   GET  /api/borrow                        — all records
///   GET  /api/borrow/member/{memberId}      — history for one member
///   POST /api/borrow                        — borrow a book
///   POST /api/return                        — return a book
///
/// Note: POST /api/return lives here (not in a separate ReturnController) because
/// returning is the counterpart to borrowing and belongs to the same domain.
/// The [Route] attribute on ReturnBook overrides the controller-level route for that action.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BorrowController : ControllerBase
{
    private readonly IBorrowService _service;

    public BorrowController(IBorrowService service)
    {
        _service = service;
    }

    /// <summary>
    /// GET /api/borrow
    /// Returns all borrow records with book title and member name included.
    /// Returns empty array when none exist.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var records = await _service.GetAllAsync();
        return Ok(records);
    }

    /// <summary>
    /// GET /api/borrow/member/{memberId}
    /// Returns borrow history for a specific member.
    /// Returns 404 if the member doesn't exist — an empty array would be ambiguous.
    /// </summary>
    [HttpGet("member/{memberId}")]
    public async Task<IActionResult> GetByMember(int memberId)
    {
        try
        {
            var records = await _service.GetByMemberIdAsync(memberId);
            return Ok(records);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/borrow
    /// Borrows a book for a member. Decrements AvailableCopies on success.
    /// 404 if book or member not found, 409 if no copies available or duplicate borrow.
    /// Returns 201 with the new borrow record.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Borrow([FromBody] BorrowRequestDto dto)
    {
        try
        {
            var record = await _service.BorrowBookAsync(dto);
            return StatusCode(201, record);
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
    /// POST /api/return
    /// Returns a borrowed book. Increments AvailableCopies, sets ReturnDate and Status.
    /// 404 if record not found, 400 if already returned.
    /// The route is /api/return (not /api/borrow/return) to match ENDPOINTS.md spec.
    /// </summary>
    [HttpPost("/api/return")]
    public async Task<IActionResult> Return([FromBody] ReturnRequestDto dto)
    {
        try
        {
            var record = await _service.ReturnBookAsync(dto);
            return Ok(record);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
