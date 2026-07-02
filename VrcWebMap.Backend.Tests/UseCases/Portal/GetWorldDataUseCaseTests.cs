using System.Text.Json;
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
        Assert.Empty(typeof(GetWorldData.Request).GetProperties());
        Assert.Collection(
            result.Value.Categorys,
            category =>
            {
                Assert.Equal("関東", category.CategoryName);
                var world = Assert.Single(category.Worlds);
                Assert.Equal("wrld_tokyo", world.ID);
                Assert.Equal("東京ワールド", world.Name);
                Assert.Equal("public", world.ReleaseStatus);
            },
            category =>
            {
                Assert.Equal("関西", category.CategoryName);
                var world = Assert.Single(category.Worlds);
                Assert.Equal("wrld_osaka", world.ID);
            });
    }

    [Fact]
    public async Task ExecuteAsync_AlwaysIncludesPublicAndPrivateReleaseWorlds()
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

        var result = await useCase.ExecuteAsync(new GetWorldData.Request());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.ShowPrivateWorld);
        var category = Assert.Single(result.Value.Categorys);
        Assert.Collection(
            category.Worlds.OrderBy(world => world.ID, StringComparer.Ordinal),
            world =>
            {
                Assert.Equal("wrld_private", world.ID);
                Assert.Equal("private", world.ReleaseStatus);
            },
            world =>
            {
                Assert.Equal("wrld_public", world.ID);
                Assert.Equal("public", world.ReleaseStatus);
            });
    }

    [Fact]
    public async Task ExecuteAsync_SerializesOnlySupportedWpplsProperties()
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
        repository.AddWorld(CreateWorld(spot.Id, "wrld_private", "非公開ワールド", isPrivate: true));
        var useCase = new GetWorldDataUseCase(repository);

        var result = await useCase.ExecuteAsync(new GetWorldData.Request());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(result.Value));
        var root = json.RootElement;
        Assert.True(root.GetProperty("ShowPrivateWorld").GetBoolean());
        Assert.False(root.TryGetProperty("Roles", out _));

        var category = root.GetProperty("Categorys")[0];
        Assert.False(category.TryGetProperty("PermittedRoles", out _));

        var world = category.GetProperty("Worlds")[0];
        Assert.Equal("wrld_private", world.GetProperty("ID").GetString());
        Assert.Equal("private", world.GetProperty("ReleaseStatus").GetString());
        Assert.False(world.TryGetProperty("PermittedRoles", out _));
    }

    [Fact]
    public async Task ExecuteAsync_ExcludesWorldWhoseSpotDoesNotExist()
    {
        var repository = new FakeSpotRepository();
        repository.AddWorld(CreateWorld(Guid.NewGuid(), "wrld_orphan", "孤立ワールド"));
        var useCase = new GetWorldDataUseCase(repository);

        var result = await useCase.ExecuteAsync(new GetWorldData.Request());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value.Categorys);
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
