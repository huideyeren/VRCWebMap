using VrcWebMap.Backend.Contracts.Spots;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.Tests.TestDoubles;
using VrcWebMap.Backend.UseCases.Spots;

namespace VrcWebMap.Backend.Tests.UseCases.Spots;

public sealed class ListSpotsUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsSpotsOrderedByName()
    {
        var spotB = new Spot(Guid.NewGuid(), "owner-user", "B Spot", 35, 139, AreaCodes.Japan.Tokyo, "B");
        var spotA = new Spot(Guid.NewGuid(), "owner-user", "A Spot", 36, 140, AreaCodes.Japan.Osaka, "A");
        var repository = new FakeSpotRepository(spotB, spotA);
        var useCase = new ListSpotsUseCase(repository);

        var result = await useCase.ExecuteAsync(new ListSpots.Request());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Collection(
            result.Value.Spots,
            spot => Assert.Equal(spotA, spot),
            spot => Assert.Equal(spotB, spot));
    }

    [Fact]
    public async Task ExecuteAsync_QueryMatchesSpotName()
    {
        var akihabara = new Spot(Guid.NewGuid(), "owner-user", "Akihabara Station", 35, 139, AreaCodes.Japan.Tokyo, "電気街の集合場所です。");
        var namba = new Spot(Guid.NewGuid(), "owner-user", "Namba", 34, 135, AreaCodes.Japan.Osaka, "大阪の集合場所です。");
        var repository = new FakeSpotRepository(namba, akihabara);
        var useCase = new ListSpotsUseCase(repository);

        var result = await useCase.ExecuteAsync(new ListSpots.Request("akihabara"));

        Assert.True(result.IsSuccess);
        var spot = Assert.Single(result.Value!.Spots);
        Assert.Equal(akihabara, spot);
    }

    [Fact]
    public async Task ExecuteAsync_QueryMatchesSpotDescription()
    {
        var portal = new Spot(Guid.NewGuid(), "owner-user", "Portal Hub", 35, 139, AreaCodes.Japan.Tokyo, "VRChat ワールドの入口です。");
        var cafe = new Spot(Guid.NewGuid(), "owner-user", "Cafe", 34, 135, AreaCodes.Japan.Osaka, "休憩場所です。");
        var repository = new FakeSpotRepository(cafe, portal);
        var useCase = new ListSpotsUseCase(repository);

        var result = await useCase.ExecuteAsync(new ListSpots.Request("ワールド"));

        Assert.True(result.IsSuccess);
        var spot = Assert.Single(result.Value!.Spots);
        Assert.Equal(portal, spot);
    }

    [Fact]
    public async Task ExecuteAsync_QueryRequiresAllTermsAcrossNameAndDescription()
    {
        var matched = new Spot(Guid.NewGuid(), "owner-user", "Tokyo Portal", 35, 139, AreaCodes.Japan.Tokyo, "VRChat ワールドの入口です。");
        var nameOnly = new Spot(Guid.NewGuid(), "owner-user", "Tokyo Cafe", 35, 139, AreaCodes.Japan.Tokyo, "休憩場所です。");
        var descriptionOnly = new Spot(Guid.NewGuid(), "owner-user", "Portal", 35, 139, AreaCodes.Japan.Tokyo, "VRChat ワールドの入口です。");
        var repository = new FakeSpotRepository(nameOnly, matched, descriptionOnly);
        var useCase = new ListSpotsUseCase(repository);

        var result = await useCase.ExecuteAsync(new ListSpots.Request("Tokyo ワールド"));

        Assert.True(result.IsSuccess);
        var spot = Assert.Single(result.Value!.Spots);
        Assert.Equal(matched, spot);
    }
}
