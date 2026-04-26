using LibraryAPI.DTOs;
using LibraryAPI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LibraryAPI.Controllers;

/// <summary>
/// Handles HTTP requests for the /api/books resource.
/// Layer: Controller
///
/// Responsibilities:
/// - Receive HTTP request data (route params, query strings, body)
/// - Call the appropriate IBookService method
/// - Translate service results into the correct HTTP response
///
/// NOT responsible for: business logic, database access, or cache management.
/// Those concerns live in BookService and BookRepository respectively.
///
/// [ApiController] enables automatic 400 responses when BookRequestDto
/// data annotations fail — no need to manually check ModelState.IsValid.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly IBookService _service;

    /// <summary>
    /// IBookService is injected by the DI container configured in Program.cs.
    /// The controller never calls new BookService() — that would tightly couple
    /// the controller to the implementation and make testing impossible.
    /// </summary>
    public BooksController(IBookService service)
    {
        _service = service;
    }

    /// <summary>
    /// GET /api/books
    /// Returns all books. Returns an empty array (not 404) when no books exist.
    /// Response is served from IMemoryCache when available — see BookService.GetAllAsync.
    /// </summary>
    /// <returns>200 OK with list of all books.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<BookResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var books = await _service.GetAllAsync();
        return Ok(books);
    }

    /// <summary>
    /// GET /api/books/{id}
    /// Returns a single book by its id.
    /// </summary>
    /// <param name="id">The book's database id.</param>
    /// <returns>200 OK with the book, or 404 if not found.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(BookResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    /// GET /api/books/search?title=clean
    /// Searches books by title (case-insensitive, partial match).
    /// Returns empty array if no matches found — not 404.
    /// </summary>
    /// <param name="title">The title search term.</param>
    /// <returns>200 OK with matching books.</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<BookResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string title)
    {
        var books = await _service.GetAllAsync();
        var results = books.Where(b => b.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
        return Ok(results);
    }

    /// <summary>
    /// POST /api/books
    /// Creates a new book. [ApiController] returns 400 automatically if BookRequestDto
    /// annotations fail (missing title, negative copies, etc.).
    /// BookService validates that AvailableCopies does not exceed TotalCopies.
    /// </summary>
    /// <param name="dto">The book data to create.</param>
    /// <returns>201 Created with the new book and a Location header.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(BookResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
    /// Updates an existing book's data.
    /// </summary>
    /// <param name="id">The id of the book to update.</param>
    /// <param name="dto">The updated book data.</param>
    /// <returns>200 OK with updated book, 404 if not found, 400 if business rule fails.</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(BookResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    /// Deletes a book from the catalog. Also invalidates the books cache.
    /// </summary>
    /// <param name="id">The id of the book to delete.</param>
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
