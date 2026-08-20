namespace LibrarySystem.Application.DTOs.Borrow;

public class BorrowPenaltyStatusDto
{
    public bool HasOverdueBorrow { get; set; }

    public bool HasActivePenalty { get; set; }

    public DateTime? PenaltyEndDate { get; set; }
}