namespace LibrarySystem.Application.Common.Models;

public class ReturnBookWriteResult
{
    public ReturnWriteStatus Status
    {
        get;
        set;
    }

    public int PenaltyDays
    {
        get;
        set;
    }

    public DateTime? PenaltyStartDate
    {
        get;
        set;
    }

    public DateTime? PenaltyEndDate
    {
        get;
        set;
    }
}