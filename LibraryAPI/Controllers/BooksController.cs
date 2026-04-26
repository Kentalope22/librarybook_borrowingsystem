using LibraryAPI.DTOs;
using LibraryAPI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LibraryAPI.Controllers;

/// <summary>
/// Handles HTTP requests for the /api/books resource.
/// Layer: Controller
///
/// Responsibilities:
/// - Receive HTTP request data (route params, body)
/// - Call the appropriate IBookService method
/// - Translate service results or exceptions into the correct HTTP response
///
/// NOT responsible for: business logic, validation beyond [ApiController] model state,
/// direct database access, or constructing service/repository objects with new.
///
/// [ApiController] enables automatic 400 responses when the model state (data annotations
/// on BookRequestDto) is invalid — no need to manually check ModelState.IsValid.
/// [Route("api/[controller]")] maps this controller to /api/books.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly IBookService _service;

    /// <summary>
    /// Constructor injection — IBookService is provided by the DI container configured
    /// in Program.cs. We never call new BookService() here; that would tightly couple
    /// the controller to the implementation and make testing impossible.
    /// </summary>
    public BooksController(IBookService service)
    {
        _service = service;
    }

    /// <summary>
    /// GET /api/books
    /// Returns all books. Returns empty array (not 404) when no books exist.
    /// This endpoint is cached by IMemoryCache inside BookService.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var books = await _service.GetAllAsync();
        return Ok(books);
    }

    /// <summary>
    /// GET /api/books/{id}
    /// Returns a single book by id, or 404 if not found.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var book = await _service.GetByIdAsync(id);
            return Ok(book);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/books
    /// Creates a new book. [ApiController] returns 400 automatically if BookRequestDto
    /// annotations fail. Service validates AvailableCopies <= TotalCopies.
    /// Returns 201 Created with the new book in the body and a Location header.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BookRequestDto dto)
    {
        try
        {
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// PUT /api/books/{id}
    /// Updates an existing book. Returns 404 if not found, 400 if business rule fails.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] BookRequestDto dto)
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
        catch (BadRequestException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// DELETE /api/books/{id}
    /// Deletes a book. Returns 204 No Content on success, 404 if not found.
    /// 204 means "success but no body to return".
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
