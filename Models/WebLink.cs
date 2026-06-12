namespace VrcWebMap.Backend.Models;

/// <summary>
/// スポットに紐づく Web サイト情報です。
/// </summary>
/// <param name="Id">このレコードの主キーです。</param>
/// <param name="SpotId">関連するスポットの ID です。</param>
/// <param name="RegisteredByUserId">この Web サイト情報を登録したユーザーの ID です。</param>
/// <param name="SiteName">サイト名です。</param>
/// <param name="Url">サイト URL です。</param>
public sealed record WebLink(
    Guid Id,
    Guid SpotId,
    string RegisteredByUserId,
    string SiteName,
    Uri Url);
