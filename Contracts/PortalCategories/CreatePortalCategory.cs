using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.Contracts.PortalCategories;

/// <summary>
/// 地図外ワールド用カテゴリを作成する契約です。
/// </summary>
public static class CreatePortalCategory
{
    public sealed record Request(
        string Name,
        PortalCategoryVisibility Visibility,
        string? OwnerUserId = null);

    public sealed record Response(PortalCategoryData Category);
}
