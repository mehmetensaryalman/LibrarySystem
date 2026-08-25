namespace LibrarySystem.Application.DTOs.Books;

public sealed class BookPreviewResponseDto
{
    public int Id
    {
        get;
        init;
    }

    public string Name
    {
        get;
        init;
    } = string.Empty;

    public string Author
    {
        get;
        init;
    } = string.Empty;

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

    public string? Source
    {
        get;
        init;
    }

    public bool MetadataFound
    {
        get;
        init;
    }
}