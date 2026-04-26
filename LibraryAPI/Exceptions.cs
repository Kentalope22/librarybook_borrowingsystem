namespace LibraryAPI;

/// <summary>
/// Thrown by the service layer when a requested resource does not exist.
/// Controllers catch this and return 404 Not Found with { "error": message }.
///
/// Why a custom exception instead of returning null from services?
/// Using exceptions makes the "not found" path explicit and impossible to silently ignore.
/// Returning null requires every caller to add a null check — easy to forget. The
/// controller's catch block is the single place where null-like absence maps to HTTP 404.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

/// <summary>
/// Thrown by the service layer when an operation would violate a business constraint.
/// Controllers catch this and return 409 Conflict with { "error": message }.
///
/// Examples: no available copies remain, member already has the book borrowed.
/// </summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

/// <summary>
/// Thrown by the service layer when the caller's input is logically invalid even though
/// it passed annotation-level validation.
/// Controllers catch this and return 400 Bad Request with { "error": message }.
///
/// Example: attempting to return a book that has already been returned.
/// </summary>
public class BadRequestException : Exception
{
    public BadRequestException(string message) : base(message) { }
}
