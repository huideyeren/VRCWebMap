namespace VrcWebMap.Backend.Contracts.WebLinks;

/// <summary>
/// 外部へ公開するWebサイト情報です。
/// </summary>
public sealed record WebLinkData(
    Guid Id,
    string SiteName,
    Uri Url,
    string RegisteredByDisplayName,
    bool CanEdit);
