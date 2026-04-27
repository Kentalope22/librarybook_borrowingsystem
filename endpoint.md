# API Endpoint List

## Books
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/books | Get all books |
| GET | /api/books/{id} | Get book by ID |
| GET | /api/books/search?title=&author=&isbn= | Search books |
| POST | /api/books | Create a book |
| PUT | /api/books/{id} | Update a book |
| DELETE | /api/books/{id} | Delete a book |

## Members
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/members | Get all members |
| GET | /api/members/{id} | Get member by ID |
| POST | /api/members | Create a member |
| PUT | /api/members/{id} | Update a member |
| DELETE | /api/members/{id} | Delete a member |

## Borrow
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/borrow | Get all borrow records |
| GET | /api/borrow/member/{memberId} | Get borrow history for a member |
| POST | /api/borrow | Borrow a book |
| POST | /api/borrow/return | Return a book |
