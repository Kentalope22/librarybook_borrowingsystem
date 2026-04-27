using LibraryAPI.DTOs;
using LibraryAPI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LibraryAPI.Controllers;

/// <summary>
/// Handles HTTP requests for borrowing and returning books.
/// Layer: Controller
///
/// Routes in this controller:
///   GET  /api/borrow                    — all borrow records
///   GET  /api/borrow/member/{memberId}  — borrow history for one member
///   POST /api/borrow                    — borrow a book
///   POST /api/return                    — return a borrowed book
///
/// Returning a book lives here (not in a separate controller) because
/// it is the natural counterpart to borrowing — same domain, same service.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BorrowController : ControllerBase
{
    private readonly IBorrowService _service;

    /// <summary>
    /// IBorrowService is injected by the DI container configured in Program.cs.
    /// The controller never creates service instances directly — that would tightly
    /// couple it to the implementation and prevent unit testing.
    /// </summary>
    public BorrowController(IBorrowService service)
    {
        _service = service;
    }

    /// <summary>
    /// GET /api/borrow
    /// Returns all borrow records, including book title and member name.
    /// Returns an empty array (not 404) when no records exist.
    /// </summary>
    /// <returns>200 OK with list of all borrow records.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<BorrowRecordResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var records = await _service.GetAllAsync();
        return Ok(records);
    }

    /// <summary>
    /// GET /api/borrow/member/{memberId}
    /// Returns all borrow records (past and current) for a specific member.
    /// Returns 404 if the member does not exist — an empty array would be ambiguous
    /// because it could mean "member exists but never borrowed" or "member not found".
    /// </summary>
    /// <param name="memberId">The id of the member whose history to retrieve.</param>
    /// <returns>200 OK with the member's borrow history, or 404 if member not found.</returns>
    [HttpGet("member/{memberId}")]
    [ProducesResponseType(typeof(IEnumerable<BorrowRecordResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    /// Records a new book borrow for a member. Decrements the book's AvailableCopies.
    /// Returns 404 if the book or member does not exist.
    /// Returns 409 if no copies are available or if the member already has this book borrowed.
    /// </summary>
    /// <param name="dto">Contains BookId and MemberId.</param>
    /// <returns>201 Created with the new borrow record.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(BorrowRecordResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
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
    /// Processes a book return. Increments AvailableCopies, sets ReturnDate, marks as Returned.
    /// Returns 404 if the borrow record does not exist.
    /// Returns 400 if the book has already been returned.
    ///
    /// Note: The route is /api/return (not /api/borrow/return) because the [Route] attribute
    /// on this action overrides the controller-level route for this specific method.
    /// </summary>
    /// <param name="dto">Contains the BorrowRecordId to return.</param>
    /// <returns>200 OK with the updated borrow record.</returns>
    [HttpPost("/api/return")]
    [ProducesResponseType(typeof(BorrowRecordResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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