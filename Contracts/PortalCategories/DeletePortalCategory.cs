namespace VrcWebMap.Backend.Contracts.PortalCategories;

/// <summary>
/// 地図外ワールド用カテゴリを削除する契約です。
/// </summary>
public static class DeletePortalCategory
{
    public sealed record Request(Guid Id);

    public sealed record Response(Guid Id);
}
