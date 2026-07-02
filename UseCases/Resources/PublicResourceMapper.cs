using VrcWebMap.Backend.Contracts.Comments;
using VrcWebMap.Backend.Contracts.PlaceInfos;
using VrcWebMap.Backend.Contracts.Spots;
using VrcWebMap.Backend.Contracts.VRChatWorlds;
using VrcWebMap.Backend.Contracts.WebLinks;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.UseCases.Resources;

/// <summary>
/// 内部モデルを、可変な表示名と現在ユーザーの編集可否を含む公開DTOへ変換します。
/// </summary>
public sealed class PublicResourceMapper(
    IEnumerable<DiscordUser> users,
    CurrentActor? actor)
{
    private readonly IReadOnlyDictionary<string, DiscordUser> usersById =
        users.ToDictionary(user => user.DiscordUserId, StringComparer.Ordinal);

    /// <summary>
    /// スポットを公開DTOへ変換します。
    /// </summary>
    public SpotData ToSpot(
        Spot spot,
        bool hasVRChatWorld = false,
        bool hasPlaceInfo = false) =>
        new(
            spot.Id,
            spot.Name,
            spot.Latitude,
            spot.Longitude,
            spot.AreaCode,
            spot.Description,
            ResolveDisplayName(spot.RegisteredByUserId),
            CanEdit(spot.RegisteredByUserId),
            hasVRChatWorld,
            hasPlaceInfo);

    /// <summary>
    /// VRChatワールドを公開DTOへ変換します。
    /// </summary>
    public VRChatWorldData ToVRChatWorld(VRChatWorld world) =>
        new(
            world.Id,
            world.VRChatWorldId,
            world.Name,
            world.RecommendedCapacity,
            world.Capacity,
            world.Description,
            world.PC,
            world.Android,
            world.IOS,
            world.IsPrivate,
            ResolveDisplayName(world.RegisteredByUserId),
            CanEdit(world.RegisteredByUserId));

    /// <summary>
    /// 場所情報を公開DTOへ変換します。
    /// </summary>
    public PlaceInfoData ToPlaceInfo(PlaceInfo placeInfo) =>
        new(
            placeInfo.Id,
            placeInfo.Name,
            placeInfo.Address,
            placeInfo.BusinessInformation,
            ResolveDisplayName(placeInfo.RegisteredByUserId),
            CanEdit(placeInfo.RegisteredByUserId));

    /// <summary>
    /// Webリンクを公開DTOへ変換します。
    /// </summary>
    public WebLinkData ToWebLink(WebLink webLink) =>
        new(
            webLink.Id,
            webLink.SiteName,
            webLink.Url,
            ResolveDisplayName(webLink.RegisteredByUserId),
            CanEdit(webLink.RegisteredByUserId));

    /// <summary>
    /// コメントを公開DTOへ変換します。
    /// </summary>
    public CommentData ToComment(Comment comment) =>
        new(
            comment.Id,
            comment.Comments,
            ResolveDisplayName(comment.RegisteredByUserId),
            CanEdit(comment.RegisteredByUserId));

    /// <summary>
    /// 内部ユーザーIDから現在のVRChat表示名を解決します。
    /// </summary>
    public string ResolveDisplayName(string? userId) =>
        userId is not null &&
        usersById.TryGetValue(userId, out var user) &&
        !string.IsNullOrWhiteSpace(user.VRChatDisplayName)
            ? user.VRChatDisplayName
            : "不明なユーザー";

    private bool CanEdit(string registeredByUserId) =>
        actor?.IsAdmin == true ||
        string.Equals(actor?.DiscordUserId, registeredByUserId, StringComparison.Ordinal);
}
