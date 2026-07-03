using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Comments;
using VrcWebMap.Backend.Contracts.PlaceInfos;
using VrcWebMap.Backend.Contracts.Spots;
using VrcWebMap.Backend.Contracts.VRChatWorlds;
using VrcWebMap.Backend.Contracts.WebLinks;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.Tests.TestDoubles;
using VrcWebMap.Backend.UseCases.Spots;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.Tests.UseCases.Spots;

public sealed class GetSpotUseCaseTests
{
    [Fact]
    public void Response_UsesPublicResourceDtos()
    {
        Assert.Equal(typeof(SpotData), typeof(GetSpot.Response).GetProperty(nameof(GetSpot.Response.Spot))!.PropertyType);
        Assert.Equal(
            typeof(VRChatWorldData[]),
            typeof(GetSpot.Response).GetProperty(nameof(GetSpot.Response.VRChatWorlds))!.PropertyType);
        Assert.Equal(
            typeof(PlaceInfoData[]),
            typeof(GetSpot.Response).GetProperty(nameof(GetSpot.Response.PlaceInfos))!.PropertyType);
        Assert.Equal(
            typeof(WebLinkData[]),
            typeof(GetSpot.Response).GetProperty(nameof(GetSpot.Response.WebLinks))!.PropertyType);
        Assert.Equal(
            typeof(CommentData[]),
            typeof(GetSpot.Response).GetProperty(nameof(GetSpot.Response.Comments))!.PropertyType);
    }

    [Fact]
    public async Task ExecuteAsync_ExistingSpot_ReturnsSpot()
    {
        var spot = new Spot(Guid.NewGuid(), "owner-user", "スポット", 35.681236, 139.767125, AreaCodes.Japan.Tokyo, "説明");
        var repository = new FakeSpotRepository(spot);
        var useCase = CreateUseCase(repository);

        var result = await useCase.ExecuteAsync(new GetSpot.Request(spot.Id));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(spot.Id, result.Value.Spot.Id);
        Assert.Equal("所有者", result.Value.Spot.RegisteredByDisplayName);
        Assert.True(result.Value.Spot.CanEdit);
        Assert.Empty(result.Value.VRChatWorlds);
        Assert.Empty(result.Value.PlaceInfos);
        Assert.Empty(result.Value.WebLinks);
        Assert.Empty(result.Value.Comments);
    }

    [Fact]
    public async Task ExecuteAsync_ExistingSpot_ReturnsRelatedData()
    {
        var spot = new Spot(Guid.NewGuid(), "owner-user", "スポット", 35.681236, 139.767125, AreaCodes.Japan.Tokyo, "説明");
        var otherSpot = new Spot(Guid.NewGuid(), "owner-user", "別スポット", 36, 140, AreaCodes.Japan.Osaka, "説明");
        var repository = new FakeSpotRepository(spot, otherSpot);
        var world = new VRChatWorld(
            Guid.NewGuid(),
            spot.Id,
            "owner-user",
            "wrld_00000000-0000-0000-0000-000000000000",
            "ワールド",
            16,
            32,
            "説明",
            PC: true,
            Android: false,
            IOS: false);
        var placeInfo = new PlaceInfo(
            Guid.NewGuid(),
            spot.Id,
            "owner-user",
            "飲食店",
            "住所",
            BusinessInformation: "- 昼: 11:00-14:00\n- 夜: 17:00-22:00\n- 定休日: 不定休");
        var webLink = new WebLink(
            Guid.NewGuid(),
            spot.Id,
            "owner-user",
            "公式サイト",
            new Uri("https://example.com"));
        var comment = new Comment(Guid.NewGuid(), spot.Id, "owner-user", "コメント");
        repository.UpsertWorld(world);
        repository.UpsertPlaceInfo(placeInfo);
        repository.UpsertWebLink(webLink);
        repository.UpsertComment(comment);
        repository.UpsertWorld(world with { Id = Guid.NewGuid(), SpotId = otherSpot.Id });
        repository.UpsertWebLink(webLink with { Id = Guid.NewGuid(), SpotId = otherSpot.Id });
        var useCase = CreateUseCase(repository);

        var result = await useCase.ExecuteAsync(new GetSpot.Request(spot.Id));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(spot.Id, result.Value.Spot.Id);
        Assert.Equal(world.Id, Assert.Single(result.Value.VRChatWorlds).Id);
        Assert.Equal(placeInfo.Id, Assert.Single(result.Value.PlaceInfos).Id);
        Assert.Equal(webLink.Id, Assert.Single(result.Value.WebLinks).Id);
        Assert.Equal(comment.Id, Assert.Single(result.Value.Comments).Id);
    }

    [Fact]
    public async Task ExecuteAsync_MissingSpot_ReturnsNotFound()
    {
        var repository = new FakeSpotRepository();
        var useCase = CreateUseCase(repository);

        var result = await useCase.ExecuteAsync(new GetSpot.Request(Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(KawaErrorKind.NotFound, result.Error.Kind);
        Assert.Equal("スポットが見つかりません。", result.Error.Message);
    }

    private static GetSpotUseCase CreateUseCase(FakeSpotRepository repository) =>
        new(
            repository,
            new FakeDiscordUserRepository(
                new DiscordUser(
                    "owner-user",
                    "owner",
                    null,
                    null,
                    "guild",
                    IsGuildMember: true,
                    IsAdmin: false,
                    DateTimeOffset.UnixEpoch,
                    DateTimeOffset.UnixEpoch,
                    "所有者",
                    "所有者")),
            new FakeCurrentActorAccessor(
                new CurrentActor("owner-user", IsAdmin: false, HasVRChatDisplayName: true)));
}
