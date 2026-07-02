namespace VrcWebMap.Backend.Contracts.Spots;

/// <summary>
/// 地図上へ公開するスポット情報です。
/// </summary>
public sealed record SpotData(
    Guid Id,
    string Name,
    double Latitude,
    double Longitude,
    int AreaCode,
    string Description,
    string RegisteredByDisplayName,
    bool CanEdit,
    bool HasVRChatWorld,
    bool HasPlaceInfo);
