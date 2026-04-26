# Library Book Borrowing System — Design Document

**Team Name:** [Your Company Name]  
**Course:** [Course Number and Section]  
**Submission Date:** April 26, 2025  

---

## 1. System Overview

The Library Book Borrowing System is a RESTful backend API built with ASP.NET Core 8. It provides endpoints for managing a library's book catalog, member registry, and borrowing records. The system is designed with clean layered architecture, emphasizing correctness, concurrency safety, and performance.

The API uses an in-memory database (Entity Framework Core InMemoryDatabase) for portability and ease of testing. Business logic is fully separated from data access, and all API responses follow a consistent format.

---

## 2. API Endpoints

| Method | Route | Description | Success Code | Error Codes |
|--------|-------|-------------|-------------|-------------|
| GET | /api/books | Get all books (cached) | 200 | — |
| POST | /api/books | Create a book | 201 | 400 |
| GET | /api/books/{id} | Get book by id | 200 | 404 |
| PUT | /api/books/{id} | Update a book | 200 | 400, 404 |
| DELETE | /api/books/{id} | Delete a book | 204 | 404 |
| GET | /api/members | Get all members | 200 | — |
| POST | /api/members | Create a member | 201 | 400, 409 |
| GET | /api/members/{id} | Get member by id | 200 | 404 |
| PUT | /api/members/{id} | Update a member | 200 | 400, 404, 409 |
| DELETE | /api/members/{id} | Delete a member | 204 | 404 |
| POST | /api/borrow | Borrow a book | 201 | 400, 404, 409 |
| POST | /api/borrow/return | Return a book | 200 | 400, 404 |
| GET | /api/borrow | Get all borrow records | 200 | — |
| GET | /api/borrow/member/{id} | Get member borrow history | 200 | 404 |

---

## 3. Architecture

The project follows a strict three-layer architecture to separate concerns and improve testability:

**Controller Layer** handles HTTP request/response translation. Controllers receive DTOs from the request body, call the appropriate service method, and return the correct HTTP status code with a response DTO. Controllers contain no business logic.

**Service Layer** enforces business rules. For example, `BorrowService` verifies that the book has available copies before allowing a borrow, and checks for duplicate open borrows. `BookService` manages the in-memory cache. Services do not access the database directly.

**Repository Layer** handles all database access via Entity Framework Core. Repositories expose clean async methods (`GetAllAsync`, `GetByIdAsync`, `AddAsync`, `SaveChangesAsync`) and use `.Include()` for eager loading of navigation properties. Repositories contain no business rules.

Dependency Injection is configured in `Program.cs`. All services and repositories are registered as Scoped so they share a single DbContext instance per HTTP request.

---

## 4. DTO Design

The API uses separate Request and Response DTOs to avoid exposing EF Core entities directly.

**Request DTOs** (`BookRequestDto`, `MemberRequestDto`, `BorrowRequestDto`, `ReturnRequestDto`) carry only the fields the client needs to supply. They include data annotation validators (`[Required]`, `[Range]`, `[EmailAddress]`).

**Response DTOs** (`BookResponseDto`, `MemberResponseDto`, `BorrowRecordResponseDto`) carry only the fields appropriate for the client to receive. For example, `BorrowRecordResponseDto` includes `BookTitle` and `MemberName` (denormalized for convenience) rather than requiring the client to make additional requests.

Entities are never returned directly from controllers. This decoupling means the database schema can change without breaking the public API contract.

---

## 5. Validation Strategy

Validation occurs at two layers:

**Controller level** uses ASP.NET Core data annotations on request DTOs. These catch malformed requests early — missing required fields, invalid email formats, out-of-range numbers. The `InvalidModelStateResponseFactory` override in `Program.cs` ensures these errors return `{ "error": "..." }` matching our error format.

**Service level** enforces business rules that span multiple fields or require database reads. Examples: `AvailableCopies <= TotalCopies` (cross-field rule), email uniqueness (requires DB lookup), borrow availability check (requires DB read of AvailableCopies), and duplicate borrow prevention (requires DB query for open borrows).

---

## 6. Error Handling

All errors are handled by `ExceptionHandlingMiddleware`, which is registered first in the middleware pipeline. It catches any unhandled exception and returns a consistent JSON response:

```json
{ "error": "Descriptive message here" }
```

Custom exception types (`NotFoundException`, `ConflictException`, `BadRequestException`) are mapped to their appropriate HTTP status codes (404, 409, 400). Unexpected exceptions return 500. Stack traces are never exposed to clients.

---

## 7. Concurrency Handling

The system handles the race condition where two users attempt to borrow the last copy of a book simultaneously.

The `Book` model has a `[Timestamp]` property (`RowVersion`). When Entity Framework Core saves a change to a book, it generates `WHERE RowVersion = <original_value>` in the SQL UPDATE. If another request already updated the row (changing `RowVersion`), EF Core throws `DbUpdateConcurrencyException`.

`BorrowService.BorrowBookAsync` catches this exception and throws a `ConflictException`, returning HTTP 409. This guarantees `AvailableCopies` never goes below 0, even under concurrent load. Only one of the two simultaneous borrow requests succeeds; the other fails safely with a clear error message.

---

## 8. Caching Strategy

**What is cached:** The result of `GET /api/books` (the full list of books as `BookResponseDto[]`).

**Why this endpoint:** The book catalog is read far more frequently than it changes. Every page load or app open might call this endpoint, while books are added/updated rarely.

**How it works:** `BookService` injects `IMemoryCache`. On `GetAllAsync`, it checks the cache first. On a cache miss, it queries the database, stores the result with a 5-minute TTL, and returns it. Subsequent calls within that window return the cached list instantly without hitting the database.

**Cache invalidation:** Any write operation (create, update, delete) explicitly calls `_cache.Remove(BooksCacheKey)` after `SaveChangesAsync` succeeds. This ensures clients never see stale data after a change, regardless of the TTL.

**Performance benefit:** Eliminates repeated DB round-trips for the most-called read endpoint. Under load, this reduces database pressure and improves response latency significantly.

---

## 9. Team Contributions

**Team Name:** [Your Company Name]

| Member | Role | Contributions |
|--------|------|--------------|
| [Person 1 Name] | Foundation | Models (Book, Member, BorrowRecord, BorrowStatus), DbContext, Exceptions, ExceptionHandlingMiddleware, Program.cs (DI registration + seed data), README |
| [Person 2 Name] | Books Feature | BooksController, BookService, IBookService, BookRepository, IBookRepository, BookRequestDto, BookResponseDto, caching implementation |
| [Person 3 Name] | Members Feature | MembersController, MemberService, IMemberService, MemberRepository, IMemberRepository, MemberRequestDto, MemberResponseDto, validation |
| [Person 4 Name] | Borrow Feature & Docs | BorrowController, BorrowService, IBorrowService, BorrowRepository, IBorrowRepository, BorrowRequestDto, BorrowRecordResponseDto, ReturnRequestDto, .http test suite, this design document |
