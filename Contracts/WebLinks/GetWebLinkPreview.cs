namespace VrcWebMap.Backend.Contracts.WebLinks;

/// <summary>
/// WebLink の OGP preview を取得するユースケースの契約です。
/// </summary>
public static class GetWebLinkPreview
{
    /// <summary>
    /// OGP preview を取得する対象 URL です。
    /// </summary>
    /// <param name="Url">preview 対象の public な http/https URL です。</param>
    public sealed record Request(Uri Url);

    /// <summary>
    /// 取得した OGP preview 情報です。
    /// </summary>
    /// <param name="Preview">一時表示用の preview 情報です。保存データには含めません。</param>
    public sealed record Response(Preview Preview);

    /// <summary>
    /// Web ページから抽出した OGP preview です。
    /// </summary>
    /// <param name="Url">正規化された対象 URL です。</param>
    /// <param name="Title">ページタイトルまたは OGP title です。</param>
    /// <param name="Description">ページ説明または OGP description です。</param>
    /// <param name="ImageUrl">OGP image の URL です。</param>
    /// <param name="SiteName">サイト名です。</param>
    public sealed record Preview(
        Uri Url,
        string? Title,
        string? Description,
        Uri? ImageUrl,
        string? SiteName);
}
