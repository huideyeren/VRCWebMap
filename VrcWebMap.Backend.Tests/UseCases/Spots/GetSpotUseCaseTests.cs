using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Spots;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.Tests.TestDoubles;
using VrcWebMap.Backend.UseCases.Spots;

namespace VrcWebMap.Backend.Tests.UseCases.Spots;

public sealed class GetSpotUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ExistingSpot_ReturnsSpot()
    {
        var spot = new Spot(Guid.NewGuid(), "owner-user", "スポット", 35.681236, 139.767125, AreaCodes.Japan.Tokyo, "説明");
        var repository = new FakeSpotRepository(spot);
        var useCase = new GetSpotUseCase(repository);

        var result = await useCase.ExecuteAsync(new GetSpot.Request(spot.Id));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(spot, result.Value.Spot);
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
        var useCase = new GetSpotUseCase(repository);

        var result = await useCase.ExecuteAsync(new GetSpot.Request(spot.Id));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(spot, result.Value.Spot);
        Assert.Equal([world], result.Value.VRChatWorlds);
        Assert.Equal([placeInfo], result.Value.PlaceInfos);
        Assert.Equal([webLink], result.Value.WebLinks);
        Assert.Equal([comment], result.Value.Comments);
    }

    [Fact]
    public async Task ExecuteAsync_MissingSpot_ReturnsNotFound()
    {
        var repository = new FakeSpotRepository();
        var useCase = new GetSpotUseCase(repository);

        var result = await useCase.ExecuteAsync(new GetSpot.Request(Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(KawaErrorKind.NotFound, result.Error.Kind);
        Assert.Equal("スポットが見つかりません。", result.Error.Message);
    }
}
