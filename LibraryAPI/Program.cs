using LibraryAPI.Data;
using LibraryAPI.Middleware;
using LibraryAPI.Models;
using LibraryAPI.Repositories;
using LibraryAPI.Repositories.Interfaces;
using LibraryAPI.Services;
using LibraryAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
// UseInMemoryDatabase is suitable for development and testing. Data is lost when
// the process restarts — swap for UseSqlite("Data Source=library.db") to persist.
builder.Services.AddDbContext<LibraryDbContext>(options =>
    options.UseInMemoryDatabase("LibraryDb"));

// ── Caching ───────────────────────────────────────────────────────────────────
// AddMemoryCache registers IMemoryCache as a singleton. BookService depends on this
// to cache the GET /api/books response. Registered as Singleton because the cache
// itself must outlive individual HTTP requests.
builder.Services.AddMemoryCache();

// ── Repositories ──────────────────────────────────────────────────────────────
// Scoped lifetime: one instance per HTTP request. This matches the DbContext lifetime
// so the repository and its DbContext are always in sync within a single request.
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<IMemberRepository, MemberRepository>();
builder.Services.AddScoped<IBorrowRepository, BorrowRepository>();

// ── Services ──────────────────────────────────────────────────────────────────
// Scoped to match the repository scope they depend on. Using Transient would create
// a new service per injection point, which could lead to multiple DbContext instances
// within one request — Scoped avoids that.
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IMemberService, MemberService>();
builder.Services.AddScoped<IBorrowService, BorrowService>();

// ── Controllers ───────────────────────────────────────────────────────────────
// Override the default [ApiController] validation error response so it matches
// our { "error": "..." } shape instead of ASP.NET Core's default ProblemDetails
// format (which includes traceId and a verbose "errors" dictionary).
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .SelectMany(e => e.Value!.Errors)
                .Select(e => e.ErrorMessage);

            var message = string.Join("; ", errors);
            return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new { error = message });
        };
    });

var app = builder.Build();

// ── Seed demo data ────────────────────────────────────────────────────────────
// Pre-populate the in-memory DB with sample data so the API works immediately
// without needing manual POST requests. Only seeds if tables are empty.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
    SeedData(db);
}

// ── Middleware pipeline (order matters) ───────────────────────────────────────
// ExceptionHandlingMiddleware must be first so it wraps the entire pipeline and
// catches exceptions thrown anywhere downstream, including inside controllers.
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapControllers();

app.Run();

/// <summary>
/// Populates the in-memory database with sample books and members for development/demo.
/// Only seeds if the tables are empty to avoid duplicate data on hot-reload.
/// </summary>
static void SeedData(LibraryDbContext db)
{
    if (!db.Books.Any())
    {
        db.Books.AddRange(
            new Book { Title = "Clean Code", Author = "Robert C. Martin", ISBN = "978-0132350884", TotalCopies = 3, AvailableCopies = 3 },
            new Book { Title = "The Pragmatic Programmer", Author = "David Thomas", ISBN = "978-0135957059", TotalCopies = 2, AvailableCopies = 2 },
            new Book { Title = "Design Patterns", Author = "Gang of Four", ISBN = "978-0201633610", TotalCopies = 1, AvailableCopies = 1 }
        );
    }

    if (!db.Members.Any())
    {
        db.Members.AddRange(
            new Member { FullName = "Alice Johnson", Email = "alice@example.com", MembershipDate = DateTime.UtcNow.AddMonths(-6) },
            new Member { FullName = "Bob Smith", Email = "bob@example.com", MembershipDate = DateTime.UtcNow.AddMonths(-3) }
        );
    }

    db.SaveChanges();
}
