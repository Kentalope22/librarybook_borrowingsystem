using LibraryAPI.DTOs;

namespace LibraryAPI.Services.Interfaces;

/// <summary>
/// Contract for borrow/return business operations.
/// Layer: Service interface
/// </summary>
public interface IBorrowService
{
    /// <summary>Returns all borrow records with book and member info.</summary>
    Task<IEnumerable<BorrowRecordResponseDto>> GetAllAsync();

    /// <summary>
    /// Returns all borrow records for a specific member.
    /// Throws NotFoundException if the member doesn't exist.
    /// </summary>
    Task<IEnumerable<BorrowRecordResponseDto>> GetByMemberIdAsync(int memberId);

    /// <summary>
    /// Records a new borrow event and decrements AvailableCopies.
    /// Throws NotFoundException if book or member not found.
    /// Throws ConflictException if no copies available or member already has book borrowed.
    /// Throws ConflictException (wrapping DbUpdateConcurrencyException) on concurrent borrow race.
    /// Returns the created borrow record DTO.
    /// </summary>
    Task<BorrowRecordResponseDto> BorrowBookAsync(BorrowRequestDto dto);

    /// <summary>
    /// Marks a borrow record as Returned, sets ReturnDate, and increments AvailableCopies.
    /// Throws NotFoundException if record not found.
    /// Throws BadRequestException if record is already returned.
    /// Returns the updated borrow record DTO.
    /// </summary>
    Task<BorrowRecordResponseDto> ReturnBookAsync(ReturnRequestDto dto);
}
