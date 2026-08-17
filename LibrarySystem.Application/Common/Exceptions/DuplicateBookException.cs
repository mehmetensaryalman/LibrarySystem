namespace LibrarySystem.Application.Common.Exceptions;

public class DuplicateBookException : Exception
{
    public DuplicateBookException(
        string message)
        : base(message)
    {
    }

    public DuplicateBookException(
        string message,
        Exception innerException)
        : base(
            message,
            innerException)
    {
    }
}