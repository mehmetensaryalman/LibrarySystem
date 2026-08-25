using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using LibrarySystem.Application.Common.Models;
using LibrarySystem.Application.Interfaces.Books;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LibrarySystem.Infrastructure.Services.Books;

public sealed class GoogleBooksMetadataProvider :
    IBookMetadataProvider
{
    private static readonly TimeSpan
        CacheDuration =
            TimeSpan.FromHours(12);

    private readonly HttpClient
        _httpClient;

    private readonly IMemoryCache
        _memoryCache;

    private readonly
        ILogger<GoogleBooksMetadataProvider>
            _logger;

    private readonly string
        _apiKey;

    public GoogleBooksMetadataProvider(
        HttpClient httpClient,
        IMemoryCache memoryCache,
        IOptions<GoogleBooksOptions>
            options,
        ILogger<GoogleBooksMetadataProvider>
            logger)
    {
        _httpClient =
            httpClient;

        _memoryCache =
            memoryCache;

        _logger =
            logger;

        _apiKey =
            options.Value.ApiKey.Trim();
    }

    public async Task<BookMetadataResult?>
        GetMetadataAsync(
            string bookName,
            string author,
            CancellationToken
                cancellationToken = default)
    {
        var normalizedBookName =
            Normalize(bookName);

        var normalizedAuthor =
            Normalize(author);

        var cacheKey =
            $"google-books:{normalizedBookName}:{normalizedAuthor}";

        if (
            _memoryCache.TryGetValue(
                cacheKey,
                out BookMetadataResult?
                    cachedResult) &&
            cachedResult is not null)
        {
            return cachedResult;
        }

        if (
            string.IsNullOrWhiteSpace(
                _apiKey))
        {
            _logger.LogWarning(
                "Google Books API anahtarı yapılandırılmamış.");

            return null;
        }

        var query =
            $"intitle:\"{bookName.Trim()}\" inauthor:\"{author.Trim()}\"";

        var requestUri =
            "volumes" +
            $"?q={Uri.EscapeDataString(query)}" +
            "&maxResults=10" +
            "&printType=books" +
            "&orderBy=relevance" +
            "&projection=full";

        try
        {
            var response =
                await GetFromGoogleBooksAsync<
                    GoogleBooksSearchResponse>(
                    requestUri,
                    cancellationToken);

            var candidates =
                CreateCandidates(
                    response?.Items,
                    bookName,
                    author);

            if (candidates.Count == 0)
            {
                return null;
            }

            /*
             * Özet ve sayfa sayısı için en
             * dolu metadata kaydını seçiyoruz.
             */
            var metadataCandidate =
                candidates
                    .OrderByDescending(candidate =>
                        candidate.MetadataScore)
                    .First();

            /*
             * Kapak seçimini metadata seçiminden
             * ayırıyoruz. Yalnızca ISBN'li
             * baskıların görselini kullanıyoruz.
             */
            var coverCandidate =
                candidates
                    .Where(candidate =>
                        HasReliableCover(
                            candidate.Volume))
                    .OrderByDescending(candidate =>
                        candidate.CoverScore)
                    .FirstOrDefault();

            var metadata =
                CreateMetadata(
                    metadataCandidate,
                    coverCandidate,
                    candidates);

            if (metadata is null)
            {
                return null;
            }

            _memoryCache.Set(
                cacheKey,
                metadata,
                CacheDuration);

            return metadata;
        }
        catch (
            OperationCanceledException)
            when (
                !cancellationToken
                    .IsCancellationRequested)
        {
            _logger.LogWarning(
                "Google Books isteği zaman aşımına uğradı. Kitap: {BookName}",
                bookName);

            return null;
        }
        catch (
            HttpRequestException exception)
        {
            _logger.LogWarning(
                exception,
                "Google Books isteği başarısız oldu. Kitap: {BookName}",
                bookName);

            return null;
        }
        catch (
            JsonException exception)
        {
            _logger.LogWarning(
                exception,
                "Google Books yanıtı okunamadı. Kitap: {BookName}",
                bookName);

            return null;
        }
    }

    private async Task<T?>
        GetFromGoogleBooksAsync<T>(
            string requestUri,
            CancellationToken
                cancellationToken)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                requestUri);

        request.Headers
            .TryAddWithoutValidation(
                "X-Goog-Api-Key",
                _apiKey);

        using var response =
            await _httpClient.SendAsync(
                request,
                HttpCompletionOption
                    .ResponseHeadersRead,
                cancellationToken);

        response
            .EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<T>(
                cancellationToken:
                    cancellationToken);
    }

    private static List<VolumeCandidate>
        CreateCandidates(
            List<GoogleBooksVolume>?
                volumes,
            string requestedBookName,
            string requestedAuthor)
    {
        if (
            volumes is null ||
            volumes.Count == 0)
        {
            return [];
        }

        var candidates =
            new List<VolumeCandidate>();

        foreach (var volume in volumes)
        {
            var matchScore =
                CalculateMatchScore(
                    volume.VolumeInfo,
                    requestedBookName,
                    requestedAuthor);

            if (matchScore < 0)
            {
                continue;
            }

            candidates.Add(
                new VolumeCandidate
                {
                    Volume =
                        volume,

                    MetadataScore =
                        matchScore +
                        CalculateMetadataScore(
                            volume.VolumeInfo),

                    CoverScore =
                        matchScore +
                        CalculateCoverScore(
                            volume)
                });
        }

        return candidates;
    }

    private static int CalculateMatchScore(
        GoogleBooksVolumeInfo volumeInfo,
        string requestedBookName,
        string requestedAuthor)
    {
        var requestedTitle =
            Normalize(
                requestedBookName);

        var candidateTitle =
            Normalize(
                volumeInfo.Title);

        if (
            string.IsNullOrWhiteSpace(
                candidateTitle))
        {
            return -1;
        }

        var exactTitleMatch =
            candidateTitle ==
            requestedTitle;

        var partialTitleMatch =
            candidateTitle.Contains(
                requestedTitle,
                StringComparison.Ordinal) ||
            requestedTitle.Contains(
                candidateTitle,
                StringComparison.Ordinal);

        if (
            !exactTitleMatch &&
            !partialTitleMatch)
        {
            return -1;
        }

        var requestedAuthorName =
            Normalize(
                requestedAuthor);

        var exactAuthorMatch =
            volumeInfo.Authors?
                .Any(candidateAuthor =>
                    Normalize(candidateAuthor) ==
                    requestedAuthorName) ==
            true;

        var partialAuthorMatch =
            volumeInfo.Authors?
                .Any(candidateAuthor =>
                {
                    var normalizedCandidate =
                        Normalize(
                            candidateAuthor);

                    return
                        normalizedCandidate.Contains(
                            requestedAuthorName,
                            StringComparison.Ordinal) ||
                        requestedAuthorName.Contains(
                            normalizedCandidate,
                            StringComparison.Ordinal);
                }) ==
            true;

        /*
         * Başlık kısmen eşleşiyorsa
         * yazar eşleşmesini zorunlu tutuyoruz.
         */
        if (
            !exactTitleMatch &&
            !exactAuthorMatch &&
            !partialAuthorMatch)
        {
            return -1;
        }

        var score =
            exactTitleMatch
                ? 100
                : 60;

        if (exactAuthorMatch)
        {
            score += 50;
        }
        else if (partialAuthorMatch)
        {
            score += 25;
        }

        return score;
    }

    private static int
        CalculateMetadataScore(
            GoogleBooksVolumeInfo
                volumeInfo)
    {
        var score = 0;

        if (
            volumeInfo.PageCount >
            0)
        {
            score += 25;
        }

        if (
            !string.IsNullOrWhiteSpace(
                volumeInfo.Description))
        {
            score += 35;
        }

        if (
            !string.IsNullOrWhiteSpace(
                volumeInfo.InfoLink))
        {
            score += 5;
        }

        if (
            !string.IsNullOrWhiteSpace(
                volumeInfo.Publisher))
        {
            score += 5;
        }

        if (
            string.Equals(
                volumeInfo.Language,
                "tr",
                StringComparison
                    .OrdinalIgnoreCase))
        {
            score += 10;
        }

        return score;
    }

    private static int
        CalculateCoverScore(
            GoogleBooksVolume volume)
    {
        var score =
            GetCoverQualityScore(
                volume.VolumeInfo
                    .ImageLinks);

        if (
            HasIdentifier(
                volume.VolumeInfo,
                "ISBN_13"))
        {
            score += 80;
        }
        else if (
            HasIdentifier(
                volume.VolumeInfo,
                "ISBN_10"))
        {
            score += 60;
        }

        if (
            string.Equals(
                volume.VolumeInfo.Language,
                "tr",
                StringComparison
                    .OrdinalIgnoreCase))
        {
            score += 15;
        }

        if (
            !string.IsNullOrWhiteSpace(
                volume.VolumeInfo.Publisher))
        {
            score += 10;
        }

        if (
            volume.AccessInfo?
                .PublicDomain ==
            true)
        {
            score -= 30;
        }

        return score;
    }

    private static bool HasReliableCover(
        GoogleBooksVolume volume)
    {
        if (
            !HasAnyCover(
                volume.VolumeInfo
                    .ImageLinks))
        {
            return false;
        }

        return
            HasIdentifier(
                volume.VolumeInfo,
                "ISBN_13") ||
            HasIdentifier(
                volume.VolumeInfo,
                "ISBN_10");
    }

    private static bool HasIdentifier(
        GoogleBooksVolumeInfo volumeInfo,
        string identifierType)
    {
        return volumeInfo
            .IndustryIdentifiers?
            .Any(identifier =>
                string.Equals(
                    identifier.Type,
                    identifierType,
                    StringComparison
                        .OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(
                    identifier.Identifier)) ==
            true;
    }

    private static bool HasAnyCover(
        GoogleBooksImageLinks?
            imageLinks)
    {
        if (imageLinks is null)
        {
            return false;
        }

        return
            !string.IsNullOrWhiteSpace(
                imageLinks.ExtraLarge) ||
            !string.IsNullOrWhiteSpace(
                imageLinks.Large) ||
            !string.IsNullOrWhiteSpace(
                imageLinks.Medium) ||
            !string.IsNullOrWhiteSpace(
                imageLinks.Small) ||
            !string.IsNullOrWhiteSpace(
                imageLinks.Thumbnail) ||
            !string.IsNullOrWhiteSpace(
                imageLinks.SmallThumbnail);
    }

    private static int
        GetCoverQualityScore(
            GoogleBooksImageLinks?
                imageLinks)
    {
        if (imageLinks is null)
        {
            return 0;
        }

        if (
            !string.IsNullOrWhiteSpace(
                imageLinks.ExtraLarge))
        {
            return 30;
        }

        if (
            !string.IsNullOrWhiteSpace(
                imageLinks.Large))
        {
            return 25;
        }

        if (
            !string.IsNullOrWhiteSpace(
                imageLinks.Medium))
        {
            return 20;
        }

        if (
            !string.IsNullOrWhiteSpace(
                imageLinks.Small))
        {
            return 15;
        }

        if (
            !string.IsNullOrWhiteSpace(
                imageLinks.Thumbnail))
        {
            return 8;
        }

        if (
            !string.IsNullOrWhiteSpace(
                imageLinks.SmallThumbnail))
        {
            return 3;
        }

        return 0;
    }

    private static BookMetadataResult?
        CreateMetadata(
            VolumeCandidate metadataCandidate,
            VolumeCandidate? coverCandidate,
            List<VolumeCandidate>
                candidates)
    {
        var metadataInfo =
            metadataCandidate
                .Volume
                .VolumeInfo;

        var coverImageUrl =
            coverCandidate is null
                ? null
                : GetBestCoverUrl(
                    coverCandidate
                        .Volume
                        .VolumeInfo
                        .ImageLinks);

        /*
         * Seçilen metadata kaydında sayfa
         * sayısı yoksa diğer eşleşen
         * baskılardan tamamlıyoruz.
         */
        var pageCount =
            metadataInfo.PageCount > 0
                ? metadataInfo.PageCount
                : candidates
                    .OrderByDescending(candidate =>
                        candidate.MetadataScore)
                    .Select(candidate =>
                        candidate
                            .Volume
                            .VolumeInfo
                            .PageCount)
                    .FirstOrDefault(value =>
                        value > 0);

        var description =
            !string.IsNullOrWhiteSpace(
                metadataInfo.Description)
                ? metadataInfo.Description
                : candidates
                    .OrderByDescending(candidate =>
                        candidate.MetadataScore)
                    .Select(candidate =>
                        candidate
                            .Volume
                            .VolumeInfo
                            .Description)
                    .FirstOrDefault(value =>
                        !string.IsNullOrWhiteSpace(
                            value));

        var summary =
            CleanSummary(
                description);

        var infoLink =
            !string.IsNullOrWhiteSpace(
                metadataInfo.InfoLink)
                ? metadataInfo.InfoLink
                : coverCandidate?
                    .Volume
                    .VolumeInfo
                    .InfoLink;

        var infoUrl =
            NormalizeHttpsUrl(
                infoLink);

        if (
            coverImageUrl is null &&
            !pageCount.HasValue &&
            summary is null)
        {
            return null;
        }

        return new BookMetadataResult
        {
            CoverImageUrl =
                coverImageUrl,

            PageCount =
                pageCount,

            Summary =
                summary,

            InfoUrl =
                infoUrl
        };
    }

    private static string? GetBestCoverUrl(
        GoogleBooksImageLinks?
            imageLinks)
    {
        if (imageLinks is null)
        {
            return null;
        }

        return NormalizeHttpsUrl(
            imageLinks.ExtraLarge ??
            imageLinks.Large ??
            imageLinks.Medium ??
            imageLinks.Small ??
            imageLinks.Thumbnail ??
            imageLinks.SmallThumbnail);
    }

    private static string? CleanSummary(
        string? description)
    {
        if (
            string.IsNullOrWhiteSpace(
                description))
        {
            return null;
        }

        var withoutHtml =
            Regex.Replace(
                description,
                "<[^>]+>",
                " ");

        var decoded =
            WebUtility.HtmlDecode(
                withoutHtml);

        var normalizedWhitespace =
            Regex.Replace(
                    decoded,
                    @"\s+",
                    " ")
                .Trim();

        const int maximumLength =
            900;

        if (
            normalizedWhitespace.Length <=
            maximumLength)
        {
            return normalizedWhitespace;
        }

        return
            normalizedWhitespace[
                ..(maximumLength - 3)] +
            "...";
    }

    private static string? NormalizeHttpsUrl(
        string? value)
    {
        if (
            string.IsNullOrWhiteSpace(
                value))
        {
            return null;
        }

        return value.Trim()
            .Replace(
                "http://",
                "https://",
                StringComparison
                    .OrdinalIgnoreCase);
    }

    private static string Normalize(
        string? value)
    {
        if (
            string.IsNullOrWhiteSpace(
                value))
        {
            return string.Empty;
        }

        return Regex.Replace(
                value.Trim()
                    .ToUpperInvariant(),
                @"\s+",
                " ")
            .Trim();
    }

    private sealed class VolumeCandidate
    {
        public GoogleBooksVolume Volume
        {
            get;
            init;
        } = new();

        public int MetadataScore
        {
            get;
            init;
        }

        public int CoverScore
        {
            get;
            init;
        }
    }

    private sealed class
        GoogleBooksSearchResponse
    {
        [JsonPropertyName("items")]
        public List<GoogleBooksVolume>?
            Items
        {
            get;
            init;
        }
    }

    private sealed class
        GoogleBooksVolume
    {
        [JsonPropertyName("volumeInfo")]
        public GoogleBooksVolumeInfo
            VolumeInfo
        {
            get;
            init;
        } = new();

        [JsonPropertyName("accessInfo")]
        public GoogleBooksAccessInfo?
            AccessInfo
        {
            get;
            init;
        }
    }

    private sealed class
        GoogleBooksVolumeInfo
    {
        [JsonPropertyName("title")]
        public string? Title
        {
            get;
            init;
        }

        [JsonPropertyName("authors")]
        public List<string>? Authors
        {
            get;
            init;
        }

        [JsonPropertyName("publisher")]
        public string? Publisher
        {
            get;
            init;
        }

        [JsonPropertyName("language")]
        public string? Language
        {
            get;
            init;
        }

        [JsonPropertyName(
            "industryIdentifiers")]
        public List<
            GoogleBooksIndustryIdentifier>?
            IndustryIdentifiers
        {
            get;
            init;
        }

        [JsonPropertyName("description")]
        public string? Description
        {
            get;
            init;
        }

        [JsonPropertyName("pageCount")]
        public int? PageCount
        {
            get;
            init;
        }

        [JsonPropertyName("imageLinks")]
        public GoogleBooksImageLinks?
            ImageLinks
        {
            get;
            init;
        }

        [JsonPropertyName("infoLink")]
        public string? InfoLink
        {
            get;
            init;
        }
    }

    private sealed class
        GoogleBooksIndustryIdentifier
    {
        [JsonPropertyName("type")]
        public string? Type
        {
            get;
            init;
        }

        [JsonPropertyName("identifier")]
        public string? Identifier
        {
            get;
            init;
        }
    }

    private sealed class
        GoogleBooksAccessInfo
    {
        [JsonPropertyName("publicDomain")]
        public bool PublicDomain
        {
            get;
            init;
        }
    }

    private sealed class
        GoogleBooksImageLinks
    {
        [JsonPropertyName(
            "smallThumbnail")]
        public string? SmallThumbnail
        {
            get;
            init;
        }

        [JsonPropertyName(
            "thumbnail")]
        public string? Thumbnail
        {
            get;
            init;
        }

        [JsonPropertyName(
            "small")]
        public string? Small
        {
            get;
            init;
        }

        [JsonPropertyName(
            "medium")]
        public string? Medium
        {
            get;
            init;
        }

        [JsonPropertyName(
            "large")]
        public string? Large
        {
            get;
            init;
        }

        [JsonPropertyName(
            "extraLarge")]
        public string? ExtraLarge
        {
            get;
            init;
        }
    }
}