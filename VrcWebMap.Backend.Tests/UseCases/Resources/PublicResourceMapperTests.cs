using VrcWebMap.Backend.Contracts.Comments;
using VrcWebMap.Backend.Contracts.PlaceInfos;
using VrcWebMap.Backend.Contracts.Spots;
using VrcWebMap.Backend.Contracts.VRChatWorlds;
using VrcWebMap.Backend.Contracts.WebLinks;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.UseCases.Resources;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.Tests.UseCases.Resources;

public sealed class PublicResourceMapperTests
{
    [Fact]
    public void ToSpot_UsesLatestDisplayNameAndOwnerCanEdit()
    {
        var mapper = new PublicResourceMapper(
            [CreateUser("owner-user", "現在の表示名")],
            new CurrentActor("owner-user", IsAdmin: false, HasVRChatDisplayName: true));
        var spot = new Spot(
            Guid.NewGuid(),
            "owner-user",
            "東京スポット",
            35.681236,
            139.767125,
            AreaCodes.Japan.Tokyo,
            "説明");

        var result = mapper.ToSpot(spot);

        Assert.Equal("現在の表示名", result.RegisteredByDisplayName);
        Assert.True(result.CanEdit);
        Assert.DoesNotContain(
            typeof(SpotData).GetProperties(),
            property => property.Name == "RegisteredByUserId");
    }

    [Fact]
    public void ToSpot_UsesUnknownUserAndThirdPartyCannotEdit()
    {
        var mapper = new PublicResourceMapper(
            [],
            new CurrentActor("other-user", IsAdmin: false, HasVRChatDisplayName: true));

        var result = mapper.ToSpot(new Spot(
            Guid.NewGuid(),
            "missing-user",
            "東京スポット",
            35.681236,
            139.767125,
            AreaCodes.Japan.Tokyo,
            "説明"));

        Assert.Equal("不明なユーザー", result.RegisteredByDisplayName);
        Assert.False(result.CanEdit);
    }

    [Fact]
    public void RelatedResources_UsePublicDtosWithoutInternalUserIds()
    {
        var spotId = Guid.NewGuid();
        var mapper = new PublicResourceMapper(
            [CreateUser("owner-user", "所有者")],
            new CurrentActor("admin-user", IsAdmin: true, HasVRChatDisplayName: true));

        var world = mapper.ToVRChatWorld(new VRChatWorld(
            Guid.NewGuid(),
            spotId,
            "owner-user",
            "wrld_00000000-0000-0000-0000-000000000000",
            "ワールド",
            16,
            32,
            "説明",
            PC: true,
            Android: false,
            IOS: false));
        var place = mapper.ToPlaceInfo(new PlaceInfo(
            Guid.NewGuid(),
            spotId,
            "owner-user",
            "場所",
            "住所",
            "営業情報"));
        var link = mapper.ToWebLink(new WebLink(
            Guid.NewGuid(),
            spotId,
            "owner-user",
            "サイト",
            new Uri("https://example.com")));
        var comment = mapper.ToComment(new Comment(
            Guid.NewGuid(),
            spotId,
            "owner-user",
            "コメント"));

        Assert.All(
            new[] { world.RegisteredByDisplayName, place.RegisteredByDisplayName, link.RegisteredByDisplayName, comment.RegisteredByDisplayName },
            displayName => Assert.Equal("所有者", displayName));
        Assert.True(world.CanEdit);
        Assert.True(place.CanEdit);
        Assert.True(link.CanEdit);
        Assert.True(comment.CanEdit);
        Assert.DoesNotContain(typeof(VRChatWorldData).GetProperties(), property => property.Name == "RegisteredByUserId");
        Assert.DoesNotContain(typeof(PlaceInfoData).GetProperties(), property => property.Name == "RegisteredByUserId");
        Assert.DoesNotContain(typeof(WebLinkData).GetProperties(), property => property.Name == "RegisteredByUserId");
        Assert.DoesNotContain(typeof(CommentData).GetProperties(), property => property.Name == "RegisteredByUserId");
    }

    private static DiscordUser CreateUser(string id, string? displayName) =>
        new(
            id,
            $"discord-{id}",
            null,
            null,
            "guild",
            IsGuildMember: true,
            IsAdmin: false,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch,
            displayName,
            displayName?.ToUpperInvariant());
}
