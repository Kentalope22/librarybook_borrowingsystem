# Library Book Borrowing System — Design Document

**Team Name:** [JKubed]
**Course:** [CPSC 449-01]
**Submission Date:** April 26, 2025

---

## 1. System Overview

The Library Book Borrowing System is a RESTful backend API built with ASP.NET Core 8. It manages a library's book catalog, member registry, and borrowing records. The system uses Entity Framework Core with an in-memory database for portability and ease of testing.

The design emphasizes clean layered architecture, consistent error handling, optimistic concurrency control, and in-memory caching for performance.

---

## 2. API Endpoints

| Method | Route | Description | Success | Errors |
|--------|-------|-------------|---------|--------|
| GET | /api/books | Get all books (cached) | 200 | — |
| POST | /api/books | Create a book | 201 | 400 |
| GET | /api/books/{id} | Get book by id | 200 | 404 |
| PUT | /api/books/{id} | Update a book | 200 | 400, 404 |
| DELETE | /api/books/{id} | Delete a book | 204 | 404 |
| GET | /api/members | Get all members | 200 | — |
| POST | /api/members | Create a member | 201 | 400, 409 |
| GET | /api/members/{id} | Get member by id | 200 | 404 |
| PUT | /api/members/{id} | Update a member | 200 | 404, 409 |
| DELETE | /api/members/{id} | Delete a member | 204 | 404 |
| POST | /api/borrow | Borrow a book | 201 | 404, 409 |
| POST | /api/return | Return a book | 200 | 400, 404 |
| GET | /api/borrow | Get all borrow records | 200 | — |
| GET | /api/borrow/member/{id} | Get member borrow history | 200 | 404 |

---

## 3. Architecture

The project uses a strict three-layer architecture:

**Controller Layer** handles HTTP only — reading route parameters and request bodies, calling the service, and returning the correct status code and response DTO. Controllers contain zero business logic.

**Service Layer** enforces all business rules — availability checks, duplicate prevention, cross-field validation, cache management, and DTO mapping. Services never access the database directly.

**Repository Layer** handles all EF Core database access. Repositories expose clean async methods and use `.Include()` for eager loading. Repositories contain no business rules.

Dependency Injection is registered in `Program.cs`. All services and repositories are Scoped so they share one DbContext per HTTP request.

---

## 4. DTO Design

Request DTOs (`BookRequestDto`, `MemberRequestDto`, `BorrowRequestDto`, `ReturnRequestDto`) carry only the fields the client must supply. Data annotation validators are applied here.

Response DTOs (`BookResponseDto`, `MemberResponseDto`, `BorrowRecordResponseDto`) expose only what clients need. `BorrowRecordResponseDto` includes `BookTitle` and `MemberName` as convenience fields so clients do not need extra requests.

Entities are never returned directly — this decouples the database schema from the API contract.

---

## 5. Validation Strategy

**Controller level** uses data annotations on request DTOs (`[Required]`, `[Range]`, `[EmailAddress]`). The `InvalidModelStateResponseFactory` override in `Program.cs` formats these errors as `{ "error": "..." }`.

**Service level** enforces business rules that require database reads or cross-field logic — availability checks, email uniqueness, and the AvailableCopies vs TotalCopies constraint.

---

## 6. Error Handling

`ExceptionHandlingMiddleware` is registered first in the pipeline and catches all unhandled exceptions. Custom types (`NotFoundException`, `ConflictException`, `BadRequestException`) map to 404, 409, and 400 respectively. Unexpected exceptions return 500. Stack traces are never exposed to clients.

All error responses use the format: `{ "error": "Descriptive message" }`

---

## 7. Concurrency Handling

The `Book` entity has a `[Timestamp]` `RowVersion` property. When EF Core saves a change to a book, it generates `WHERE RowVersion = original_value` in the SQL UPDATE. If two requests simultaneously try to borrow the last copy, only one UPDATE will match — the other gets `DbUpdateConcurrencyException`.

`BorrowService.BorrowBookAsync` catches this exception and throws a `ConflictException`, returning HTTP 409. This guarantees `AvailableCopies` never goes below 0 even under concurrent load.

---

## 8. Caching Strategy

**What is cached:** The result of `GET /api/books` (full book list as DTOs).

**Why:** The book catalog is read far more often than it changes. Caching eliminates database round-trips for the most-called read endpoint.

**How it works:** `BookService` injects `IMemoryCache`. On cache miss it queries the database, caches the result for 5 minutes, and returns it. Cache hits return instantly without touching the database.

**Invalidation:** Any write operation (create, update, delete) explicitly calls `_cache.Remove(BooksCacheKey)` after a successful save. Clients never see stale data after a change.

---

## 9. Team Contributions

**Team Name:** [Your Company Name]

| Member | Contributions |
|--------|--------------|
| [Kent Nguyen] | Models (Book, Member, BorrowRecord, BorrowStatus), DbContext, Exceptions, ExceptionHandlingMiddleware, Program.cs, README |
| [Jim Alvarez] | BooksController, BookService, BookRepository, Book DTOs, caching implementation, search endpoint |
| [Kayla Gutierrez] | MembersController, MemberService, MemberRepository, Member DTOs, email validation |
| [Nathaniel Llora] | BorrowController, BorrowService, BorrowRepository, Borrow DTOs, design document |
