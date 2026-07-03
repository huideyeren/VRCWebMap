namespace VrcWebMap.Backend.Contracts.PortalCategories;

/// <summary>
/// 現在ユーザーが閲覧できる地図外ワールド用カテゴリを一覧する契約です。
/// </summary>
public static class ListPortalCategories
{
    public sealed record Request;

    public sealed record Response(PortalCategoryData[] Categories);
}
