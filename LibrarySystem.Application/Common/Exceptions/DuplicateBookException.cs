namespace LibrarySystem.Application.Common.Exceptions;

public class DuplicateBookException : Exception
{
    public DuplicateBookException(
        string message)
        : base(message)
    {
    }
}