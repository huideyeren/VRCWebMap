namespace VrcWebMap.Backend.Contracts.WebLinks;

/// <summary>
/// スポットに紐づく Web サイト情報を更新するユースケースの契約です。
/// </summary>
public static class UpdateWebLink
{
    /// <summary>
    /// Web サイト情報更新に必要な入力です。
    /// </summary>
    /// <param name="Id">更新する Web サイト情報の ID です。</param>
    /// <param name="SiteName">更新後のサイト名です。</param>
    /// <param name="Url">更新後の URL です。</param>
    public sealed record Request(
        Guid Id,
        string SiteName,
        Uri Url);

    /// <summary>
    /// 更新された Web サイト情報を返すレスポンスです。
    /// </summary>
    /// <param name="WebLink">更新された Web サイト情報です。</param>
    public sealed record Response(WebLinkData WebLink);
}
