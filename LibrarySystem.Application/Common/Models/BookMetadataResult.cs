namespace LibrarySystem.Application.Common.Models;

public sealed class BookMetadataResult
{
    public string? CoverImageUrl
    {
        get;
        init;
    }

    public int? PageCount
    {
        get;
        init;
    }

    public string? Summary
    {
        get;
        init;
    }

    public string? InfoUrl
    {
        get;
        init;
    }
}