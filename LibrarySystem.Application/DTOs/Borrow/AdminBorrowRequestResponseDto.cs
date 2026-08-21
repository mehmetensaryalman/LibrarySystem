namespace LibrarySystem.Application.DTOs.Borrow;

public class AdminBorrowRequestResponseDto
{
    public int BorrowRequestId
    {
        get;
        set;
    }

    public string UserId
    {
        get;
        set;
    } = string.Empty;

    public string UserEmail
    {
        get;
        set;
    } = string.Empty;

    public int BookId
    {
        get;
        set;
    }

    public string BookName
    {
        get;
        set;
    } = string.Empty;

    public string Author
    {
        get;
        set;
    } = string.Empty;

    public DateTime RequestedAt
    {
        get;
        set;
    }
}