namespace VrcWebMap.Backend.Contracts.PortalCategories;

/// <summary>
/// 地図外ワールド用カテゴリ名を更新する契約です。
/// </summary>
public static class UpdatePortalCategory
{
    public sealed record Request(Guid Id, string Name);

    public sealed record Response(PortalCategoryData Category);
}
