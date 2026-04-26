# 📚 Library Book Borrowing System — Team Work Split Guide

> **Submission due: April 26** | **4 members** | **Each person must have visible Git commits**

---

## 🚀 Quick Start (Everyone does this first)

1. **One person** creates the GitHub repo and pushes this codebase. Add everyone else as collaborators.
2. **Everyone** clones the repo:
   ```bash
   git clone https://github.com/YOUR_ORG/LibraryBookBorrowingSystem.git
   cd LibraryBookBorrowingSystem
   ```
3. **Install prerequisites** (if not already installed):
   - [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
   - [VS Code](https://code.visualstudio.com/) + open the folder → install recommended extensions when prompted
4. **Run the project** to confirm it works:
   ```bash
   cd LibraryAPI
   dotnet run
   ```
   Open browser → `http://localhost:5000/api/books` — should return `[]`

---

## 👥 Work Split

### Person 1 — **Foundation & Infrastructure**
*Files to own and commit:*
- `LibraryAPI/Models/` — all 4 model files (`Book.cs`, `Member.cs`, `BorrowRecord.cs`, `BorrowStatus.cs`)
- `LibraryAPI/Data/LibraryDbContext.cs`
- `LibraryAPI/Exceptions.cs`
- `LibraryAPI/Middleware/ExceptionHandlingMiddleware.cs`
- `LibraryAPI/Program.cs`
- `LibraryAPI/LibraryAPI.csproj`
- `LibraryAPI/appsettings.json`
- `.gitignore`
- `README.md` ← **write the project README**

**Your job:** Understand and be able to explain every foundational piece — models, DB context, DI registration, global error handling. You're the person who makes sure the project compiles and runs. You'll present the **Project Structure** section of the demo.

**What to do to get commits:**
1. Add XML doc comments to any model that's missing them
2. In `Program.cs`, add a comment block explaining *why* each service is registered as Scoped
3. Add a `SeedData` method to `LibraryDbContext.cs` or `Program.cs` that pre-populates a few books and members on startup (great for demos)
4. Write a complete `README.md` with setup instructions

---

### Person 2 — **Books Feature (Controller + Service + Repository)**
*Files to own and commit:*
- `LibraryAPI/Controllers/BooksController.cs`
- `LibraryAPI/Services/BookService.cs`
- `LibraryAPI/Services/Interfaces/IBookService.cs`
- `LibraryAPI/Repositories/BookRepository.cs`
- `LibraryAPI/Repositories/Interfaces/IBookRepository.cs`
- `LibraryAPI/DTOs/BookRequestDto.cs`
- `LibraryAPI/DTOs/BookResponseDto.cs`

**Your job:** Own the entire Book vertical slice. You need to understand caching (IMemoryCache), DTO validation ([Required], [Range]), and the layered architecture for Books. You'll present the **Borrow Flow** and **Caching Behavior** in the demo.

**What to do to get commits:**
1. Add `[ProducesResponseType]` attributes to every controller action (e.g., `[ProducesResponseType(StatusCodes.Status200OK)]`)
2. Add a `GET /api/books/search?title=...` optional search endpoint in the controller + service + repo (this is a **bonus feature** in the rubric)
3. Add XML doc comments to the controller methods explaining what each endpoint does
4. Verify caching works — add a log message to `BookService.GetAllAsync()` so you can see in the console when it hits DB vs. cache

---

### Person 3 — **Members Feature (Controller + Service + Repository)**
*Files to own and commit:*
- `LibraryAPI/Controllers/MembersController.cs`
- `LibraryAPI/Services/MemberService.cs`
- `LibraryAPI/Services/Interfaces/IMemberService.cs`
- `LibraryAPI/Repositories/MemberRepository.cs`
- `LibraryAPI/Repositories/Interfaces/IMemberRepository.cs`
- `LibraryAPI/DTOs/MemberRequestDto.cs`
- `LibraryAPI/DTOs/MemberResponseDto.cs`

**Your job:** Own the entire Member vertical slice. You need to understand email uniqueness enforcement, DTO validation, and the layered architecture for Members. You'll present the **Validation Example** and **Error Handling Example** in the demo.

**What to do to get commits:**
1. Add `[ProducesResponseType]` attributes to every controller action
2. Add an email format validator — confirm `[EmailAddress]` in `MemberRequestDto.cs` is working by testing a bad email via the `.http` file
3. Add a `GET /api/members/{id}/borrows` endpoint (proxy to BorrowService — coordinate with Person 4) so member borrow history is accessible from the member controller too
4. Add XML doc comments to all controller methods

---

### Person 4 — **Borrow Feature + Design Document + Demo**
*Files to own and commit:*
- `LibraryAPI/Controllers/BorrowController.cs`
- `LibraryAPI/Services/BorrowService.cs`
- `LibraryAPI/Services/Interfaces/IBorrowService.cs`
- `LibraryAPI/Repositories/BorrowRepository.cs`
- `LibraryAPI/Repositories/Interfaces/IBorrowRepository.cs`
- `LibraryAPI/DTOs/BorrowRequestDto.cs`
- `LibraryAPI/DTOs/BorrowRecordResponseDto.cs`
- `LibraryAPI/DTOs/ReturnRequestDto.cs`
- `LibraryAPI/LibraryAPI.http` ← **expand this with all test cases**
- `docs/DesignDocument.md` ← **write the design document**

**Your job:** Own the most complex part — concurrency, borrow/return logic, and all the validation around it. You'll present the **Return Flow**, **Concurrency Scenario**, and coordinate the overall demo.

**What to do to get commits:**
1. Add `[ProducesResponseType]` attributes to every controller action
2. Expand `LibraryAPI.http` to have a full test suite (borrow a book, return it, try double-borrow, try return already-returned, etc.)
3. Write the design document (see template below)
4. Add XML doc comments to all controller methods

---

## 📄 Design Document Template (Person 4 writes this)

Create `docs/DesignDocument.md`:

```
# Library Book Borrowing System — Design Document
**Team Name:** [Your Company Name]
**Course:** [Course Number]
**Submission Date:** April 26

## 1. System Overview
A REST API backend built with ASP.NET Core 8 that manages a library's books, members, and borrowing records...

## 2. API Endpoints
| Method | Route | Description | Status Codes |
|--------|-------|-------------|--------------|
| GET | /api/books | Get all books (cached) | 200 |
| POST | /api/books | Create a book | 201, 400 |
| GET | /api/books/{id} | Get book by id | 200, 404 |
| PUT | /api/books/{id} | Update book | 200, 400, 404 |
| DELETE | /api/books/{id} | Delete book | 204, 404 |
| GET | /api/members | Get all members | 200 |
| POST | /api/members | Create member | 201, 400, 409 |
| GET | /api/members/{id} | Get member by id | 200, 404 |
| PUT | /api/members/{id} | Update member | 200, 400, 404, 409 |
| DELETE | /api/members/{id} | Delete member | 204, 404 |
| POST | /api/borrow | Borrow a book | 201, 400, 404, 409 |
| POST | /api/borrow/return | Return a book | 200, 400, 404 |
| GET | /api/borrow | Get all borrow records | 200 |
| GET | /api/borrow/member/{id} | Get member borrow history | 200, 404 |

## 3. Architecture
Explain Controller → Service → Repository layering...

## 4. DTO Design
Explain request vs response DTOs...

## 5. Validation Strategy
Controller-level (data annotations) + Service-level (business rules)...

## 6. Error Handling
ExceptionHandlingMiddleware catches all exceptions and returns { "error": "..." }...

## 7. Concurrency Handling
RowVersion / optimistic concurrency on the Book entity. Two simultaneous borrows → one gets 409...

## 8. Caching Strategy
IMemoryCache caches GET /api/books for 5 minutes. Invalidated on any write. Why: books list is read frequently and changes rarely...

## 9. Team Contributions
| Member | Contributions |
|--------|--------------|
| Person 1 | Models, DbContext, Middleware, Program.cs, README |
| Person 2 | Books Controller/Service/Repository, DTOs, Caching |
| Person 3 | Members Controller/Service/Repository, DTOs, Validation |
| Person 4 | Borrow Controller/Service/Repository, DTOs, Design Doc, Demo |
```

---

## 🎬 Demo Script (April 26)

Run in this order for the video:

1. **Project structure** (Person 1) — show folder layout in VS Code, explain layers
2. **Borrow flow** (Person 2/4):
   - `POST /api/books` — create a book with 2 copies
   - `POST /api/members` — create a member
   - `POST /api/borrow` — borrow the book
   - `GET /api/borrow` — show the record
3. **Return flow** (Person 4):
   - `POST /api/borrow/return` with the record id — show AvailableCopies went back up
4. **Validation example** (Person 3):
   - `POST /api/members` with a bad email → show `{ "error": "..." }`
   - `POST /api/books` with AvailableCopies > TotalCopies → show 400
5. **Error handling** (Person 3):
   - `GET /api/books/99999` → 404 with `{ "error": "Book with id 99999 not found" }`
6. **Concurrency scenario** (Person 4):
   - Explain the RowVersion / DbUpdateConcurrencyException mechanism from `BorrowService.cs`
   - Show the code path — no need to actually race two HTTP requests live
7. **Caching behavior** (Person 2):
   - Show the `_cache.TryGetValue` code in `BookService.cs`
   - Call `GET /api/books` twice — explain first hits DB, second hits cache

---

## 🌿 Git Workflow

**Branch naming:**
```
person1/foundation
person2/books-feature
person3/members-feature
person4/borrow-feature
```

**Steps:**
```bash
git checkout -b person2/books-feature
# make your changes
git add .
git commit -m "Add ProducesResponseType to BooksController"
git push origin person2/books-feature
# open a Pull Request → merge to main
```

Everyone merges to `main` via Pull Requests. This keeps commit history clean and gives each person visible contributions.

---

## ✅ Grading Checklist

| Rubric Item | Who Owns It | Done? |
|---|---|---|
| REST API Design (10pts) | Person 2, 3, 4 | |
| Layered Architecture (15pts) | Person 1 (explain), all | |
| DTO Usage (10pts) | Person 2, 3, 4 | |
| Dependency Injection (10pts) | Person 1 | |
| Validation (10pts) | Person 2, 3 | |
| Error Handling (10pts) | Person 1 | |
| Async Implementation (10pts) | All | |
| Concurrency Handling (10pts) | Person 4 | |
| Caching (10pts) | Person 2 | |
| Code Quality (5pts) | All | |
| Documentation (5pts) | Person 4 | |
| Demo (5pts) | All | |
