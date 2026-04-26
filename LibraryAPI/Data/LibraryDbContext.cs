using LibraryAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryAPI.Data;

/// <summary>
/// EF Core DbContext for the Library system.
/// Layer: Data
///
/// Exposes three DbSets (one per entity) that the repository layer uses to query and
/// persist data. All configuration lives in OnModelCreating so the entity classes stay
/// free of EF-specific attributes where possible.
///
/// The [Timestamp] attribute on Book.RowVersion already tells EF Core this is a
/// concurrency token, but we explicitly call .IsRowVersion() in model configuration
/// to make the intent clear and ensure it works with the in-memory provider via
/// optimistic concurrency tracking.
/// </summary>
public class LibraryDbContext : DbContext
{
    /// <param name="options">Injected by DI from Program.cs configuration (InMemory or SQLite).</param>
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options) { }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<BorrowRecord> BorrowRecords => Set<BorrowRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Explicitly configure RowVersion as a concurrency token for the Book entity.
        // This means EF Core will include a WHERE RowVersion = <original_value> clause
        // in UPDATE statements — if the row was changed by another request since we read
        // it, SaveChangesAsync throws DbUpdateConcurrencyException, which BorrowService
        // catches and converts to a 409 Conflict response.
        modelBuilder.Entity<Book>()
            .Property(b => b.RowVersion)
            .IsRowVersion();
    }
}
