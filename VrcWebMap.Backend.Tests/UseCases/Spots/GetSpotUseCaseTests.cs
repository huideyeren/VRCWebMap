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
        Assert.Empty(result.Value.Restaurants);
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
        var restaurant = new Restaurant(
            Guid.NewGuid(),
            spot.Id,
            "owner-user",
            "飲食店",
            "住所",
            Url: null,
            GurunaviUrl: null,
            TabelogUrl: null,
            RettyUrl: null,
            XUrl: null,
            InstagramUrl: null,
            OpenTime: new TimeOnly(11, 0),
            CloseTime: new TimeOnly(22, 0),
            ClosedOn: "不定休");
        var comment = new Comment(Guid.NewGuid(), spot.Id, "owner-user", "コメント");
        repository.UpsertWorld(world);
        repository.UpsertRestaurant(restaurant);
        repository.UpsertComment(comment);
        repository.UpsertWorld(world with { Id = Guid.NewGuid(), SpotId = otherSpot.Id });
        var useCase = new GetSpotUseCase(repository);

        var result = await useCase.ExecuteAsync(new GetSpot.Request(spot.Id));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(spot, result.Value.Spot);
        Assert.Equal([world], result.Value.VRChatWorlds);
        Assert.Equal([restaurant], result.Value.Restaurants);
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
