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
    /// <param name="SpotId">Web サイト情報を追加するスポットの ID です。</param>
    /// <param name="SiteName">表示用のサイト名です。</param>
    /// <param name="Url">外部サイトの URL です。</param>
    public sealed record Request(
        Guid SpotId,
        string SiteName,
        Uri Url);

    /// <summary>
    /// 登録された Web サイト情報を返すレスポンスです。
    /// </summary>
    /// <param name="WebLink">登録された Web サイト情報です。</param>
    public sealed record Response(WebLink WebLink);
}
