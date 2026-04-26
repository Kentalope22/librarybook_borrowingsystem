# Library Book Borrowing System

A backend REST API for managing a library's books, members, and borrowing records. Built with **ASP.NET Core 8** and **Entity Framework Core**.

**Team:** [Your Company Name] | **Course:** [Course Number] | **CSUF Spring 2025**

---

## Running the Project

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [VS Code](https://code.visualstudio.com/) with the C# Dev Kit extension

### Steps
```bash
git clone https://github.com/YOUR_ORG/LibraryBookBorrowingSystem.git
cd LibraryBookBorrowingSystem/LibraryAPI
dotnet run
```

The API will start at `http://localhost:5000`. Sample books and members are seeded automatically.

### Test the API
Open `LibraryAPI/LibraryAPI.http` in VS Code with the REST Client extension and click **Send Request** on any block.

---

## Project Structure

```
LibraryAPI/
├── Controllers/        # HTTP layer — routes, status codes, no business logic
├── Services/           # Business logic layer
│   └── Interfaces/
├── Repositories/       # Data access layer (EF Core)
│   └── Interfaces/
├── Models/             # EF Core entities
├── DTOs/               # Request and response objects
├── Data/               # DbContext
├── Middleware/         # Global exception handler
└── Program.cs          # DI registration, middleware pipeline, seed data
```

---

## 🔗 API Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | /api/books | Get all books |
| POST | /api/books | Create a book |
| GET | /api/books/{id} | Get book by id |
| PUT | /api/books/{id} | Update a book |
| DELETE | /api/books/{id} | Delete a book |
| GET | /api/members | Get all members |
| POST | /api/members | Create a member |
| GET | /api/members/{id} | Get member by id |
| PUT | /api/members/{id} | Update a member |
| DELETE | /api/members/{id} | Delete a member |
| POST | /api/borrow | Borrow a book |
| POST | /api/borrow/return | Return a book |
| GET | /api/borrow | Get all borrow records |
| GET | /api/borrow/member/{id} | Get member borrow history |

All errors return `{ "error": "message" }`.
