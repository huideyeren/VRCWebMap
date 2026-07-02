using VrcWebMap.Backend.Contracts.VRChatWorlds;
using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.Contracts.PortalCategories;

/// <summary>
/// 地図外ワールド用カテゴリの公開情報です。
/// </summary>
public sealed record PortalCategoryData(
    Guid Id,
    string Name,
    PortalCategoryVisibility Visibility,
    string RegisteredByDisplayName,
    string? OwnerDisplayName,
    bool CanEdit,
    VRChatWorldData[] Worlds);
