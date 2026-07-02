namespace VrcWebMap.Backend.Models;

/// <summary>
/// 地図外ワールド用カテゴリの公開範囲です。
/// </summary>
public enum PortalCategoryVisibility
{
    /// <summary>所有者のWPPLSロールだけに出力します。</summary>
    Personal,

    /// <summary>ログイン状態にかかわらず全体へ出力します。</summary>
    Public
}

/// <summary>
/// 座標を持たない、地図外VRChatワールド用カテゴリです。
/// </summary>
public sealed record PortalCategory(
    Guid Id,
    string RegisteredByUserId,
    string? OwnerUserId,
    string Name,
    string NormalizedName,
    PortalCategoryVisibility Visibility);
