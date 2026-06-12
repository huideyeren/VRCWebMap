using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.Contracts.WebLinks;

/// <summary>
/// スポットに Web サイト情報を追加するユースケースの契約です。
/// </summary>
public static class CreateWebLink
{
    /// <summary>
    /// Web サイト情報の登録入力です。
    /// </summary>
    public sealed record Request(
        Guid SpotId,
        string RegisteredByUserId,
        string SiteName,
        Uri Url);

    /// <summary>
    /// 登録された Web サイト情報を返すレスポンスです。
    /// </summary>
    /// <param name="WebLink">登録された Web サイト情報です。</param>
    public sealed record Response(WebLink WebLink);
}
