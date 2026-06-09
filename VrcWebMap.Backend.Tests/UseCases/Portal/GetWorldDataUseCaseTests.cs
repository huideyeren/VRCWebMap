using VrcWebMap.Backend.Contracts.Portal;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.Tests.TestDoubles;
using VrcWebMap.Backend.UseCases.Portal;

namespace VrcWebMap.Backend.Tests.UseCases.Portal;

public sealed class GetWorldDataUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_GroupsWorldsByJapaneseAreaCategoryName()
    {
        var tokyoSpot = new Spot(
            Guid.NewGuid(),
            "owner-user",
            "東京スポット",
            35.681236,
            139.767125,
            AreaCodes.Japan.Tokyo,
            "説明");
        var osakaSpot = new Spot(
            Guid.NewGuid(),
            "owner-user",
            "大阪スポット",
            34.702485,
            135.495951,
            AreaCodes.Japan.Osaka,
            "説明");
        var repository = new FakeSpotRepository(tokyoSpot, osakaSpot);
        repository.AddWorld(CreateWorld(tokyoSpot.Id, "wrld_tokyo", "東京ワールド"));
        repository.AddWorld(CreateWorld(osakaSpot.Id, "wrld_osaka", "大阪ワールド"));
        var useCase = new GetWorldDataUseCase(repository);

        var result = await useCase.ExecuteAsync(new GetWorldData.Request());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.False(result.Value.ReverseCategorys);
        Assert.True(result.Value.ShowPrivateWorld);
        Assert.Empty(result.Value.Roles);
        Assert.Collection(
            result.Value.Categorys,
            category =>
            {
                Assert.Equal("関東", category.CategoryName);
                var world = Assert.Single(category.Worlds);
                Assert.Equal("wrld_tokyo", world.ID);
                Assert.Equal("東京ワールド", world.Name);
                Assert.Equal("public", world.ReleaseStatus);
                Assert.Equal(new Uri("https://vrchat.com/home/world/wrld_tokyo/info"), world.WorldPageUrl);
            },
            category =>
            {
                Assert.Equal("関西", category.CategoryName);
                var world = Assert.Single(category.Worlds);
                Assert.Equal("wrld_osaka", world.ID);
            });
    }

    [Fact]
    public async Task ExecuteAsync_ShowPrivateWorldFalse_ExcludesPrivateWorlds()
    {
        var spot = new Spot(
            Guid.NewGuid(),
            "owner-user",
            "東京スポット",
            35.681236,
            139.767125,
            AreaCodes.Japan.Tokyo,
            "説明");
        var repository = new FakeSpotRepository(spot);
        repository.AddWorld(CreateWorld(spot.Id, "wrld_public", "公開ワールド"));
        repository.AddWorld(CreateWorld(spot.Id, "wrld_private", "非公開ワールド", isPrivate: true));
        var useCase = new GetWorldDataUseCase(repository);

        var result = await useCase.ExecuteAsync(new GetWorldData.Request(ShowPrivateWorld: false));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.False(result.Value.ShowPrivateWorld);
        var category = Assert.Single(result.Value.Categorys);
        var world = Assert.Single(category.Worlds);
        Assert.Equal("wrld_public", world.ID);
    }

    private static VRChatWorld CreateWorld(
        Guid spotId,
        string worldId,
        string name,
        bool isPrivate = false) =>
        new(
            Guid.NewGuid(),
            spotId,
            "discord-user-id",
            worldId,
            name,
            16,
            32,
            "説明",
            PC: true,
            Android: false,
            IOS: false,
            IsPrivate: isPrivate);
}
