namespace VrcWebMap.Backend.Contracts.VRChatWorlds;

/// <summary>
/// 外部へ公開するVRChatワールド情報です。
/// </summary>
public sealed record VRChatWorldData(
    Guid Id,
    string VRChatWorldId,
    string Name,
    int RecommendedCapacity,
    int Capacity,
    string Description,
    bool PC,
    bool Android,
    bool IOS,
    bool IsPrivate,
    string RegisteredByDisplayName,
    bool CanEdit);
