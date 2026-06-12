namespace VrcWebMap.Backend.Contracts.WebLinks;

/// <summary>
/// WebLink の OGP preview を取得するユースケースの契約です。
/// </summary>
public static class GetWebLinkPreview
{
    public sealed record Request(Uri Url);

    public sealed record Response(Preview Preview);

    public sealed record Preview(
        Uri Url,
        string? Title,
        string? Description,
        Uri? ImageUrl,
        string? SiteName);
}
