namespace VrcWebMap.Backend.Contracts.PlaceInfos;

/// <summary>
/// 外部へ公開する現実側の場所情報です。
/// </summary>
public sealed record PlaceInfoData(
    Guid Id,
    string Name,
    string Address,
    string BusinessInformation,
    string RegisteredByDisplayName,
    bool CanEdit);
