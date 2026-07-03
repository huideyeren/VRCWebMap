using System.Text.Json;
using VrcWebMap.Backend.Contracts.Portal;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.Stores;
using VrcWebMap.Backend.Tests.TestDoubles;
using VrcWebMap.Backend.UseCases.Portal;
using VrcWebMap.Backend.UseCases.Users;

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
        var useCase = UseCase(repository);

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
        var useCase = UseCase(repository);

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
        var useCase = UseCase(repository);

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
        var useCase = UseCase(repository);

        var result = await useCase.ExecuteAsync(new GetWorldData.Request());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value.Categorys);
    }

    [Fact]
    public async Task ExecuteAsync_Anonymous_IncludesRegionsAndPublicOnly()
    {
        var spot = TokyoSpot();
        var spots = new FakeSpotRepository(spot);
        spots.AddWorld(CreateWorld(spot.Id, "wrld_region", "地域"));
        var publicCategory = Category("公開", PortalCategoryVisibility.Public, null);
        var personalCategory = Category("個人", PortalCategoryVisibility.Personal, "owner");
        spots.AddWorld(CreatePortalWorld(publicCategory.Id, "wrld_public", "公開"));
        spots.AddWorld(CreatePortalWorld(personalCategory.Id, "wrld_personal", "個人"));

        var result = await UseCase(
                spots,
                Categories(personalCategory, publicCategory))
            .ExecuteAsync(new GetWorldData.Request());

        Assert.Equal(["関東", "公開"], result.Value!.Categorys.Select(item => item.CategoryName));
        Assert.Null(result.Value.Roles);
        Assert.DoesNotContain(
            result.Value.Categorys.SelectMany(item => item.Worlds),
            world => world.ID == "wrld_personal");
    }

    [Fact]
    public async Task ExecuteAsync_GeneralUser_IncludesOwnPersonalAndSingleRole()
    {
        var ownerCategory1 = Category("個人A", PortalCategoryVisibility.Personal, "owner");
        var ownerCategory2 = Category("個人B", PortalCategoryVisibility.Personal, "owner");
        var otherCategory = Category("他人", PortalCategoryVisibility.Personal, "other");
        var spots = new FakeSpotRepository();
        spots.AddWorld(CreatePortalWorld(ownerCategory1.Id, "wrld_a", "A"));

        var result = await UseCase(
                spots,
                Categories(ownerCategory2, otherCategory, ownerCategory1),
                Users(),
                Actor("owner"))
            .ExecuteAsync(new GetWorldData.Request());

        Assert.Equal(["個人A", "個人B"], result.Value!.Categorys.Select(item => item.CategoryName));
        var role = Assert.Single(result.Value.Roles!);
        Assert.Equal("Owner Latest", role.RoleName);
        Assert.Equal(["Owner Latest"], role.DisplayNames);
        Assert.All(
            result.Value.Categorys,
            category => Assert.Equal(["Owner Latest"], category.PermittedRoles!));
        Assert.Empty(result.Value.Categorys.Single(item => item.CategoryName == "個人B").Worlds);
    }

    [Fact]
    public async Task ExecuteAsync_Admin_DoesNotIncludeOtherUsersPersonal()
    {
        var publicCategory = Category("公開", PortalCategoryVisibility.Public, null);
        var otherCategory = Category("他人", PortalCategoryVisibility.Personal, "other");

        var result = await UseCase(
                new FakeSpotRepository(),
                Categories(publicCategory, otherCategory),
                Users(),
                Actor("admin", isAdmin: true))
            .ExecuteAsync(new GetWorldData.Request());

        var category = Assert.Single(result.Value!.Categorys);
        Assert.Equal("公開", category.CategoryName);
        Assert.Null(result.Value.Roles);
    }

    [Fact]
    public async Task ExecuteAsync_SerializesRoleOnlyForPersonalCategory()
    {
        var publicCategory = Category("公開", PortalCategoryVisibility.Public, null);
        var personalCategory = Category("個人", PortalCategoryVisibility.Personal, "owner");
        var result = await UseCase(
                new FakeSpotRepository(),
                Categories(publicCategory, personalCategory),
                Users(),
                Actor("owner"))
            .ExecuteAsync(new GetWorldData.Request());

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(result.Value));
        var root = json.RootElement;
        Assert.True(root.TryGetProperty("Roles", out var roles));
        Assert.Equal("Owner Latest", roles[0].GetProperty("RoleName").GetString());
        var categorys = root.GetProperty("Categorys");
        Assert.False(categorys[0].TryGetProperty("PermittedRoles", out _));
        Assert.Equal(
            "Owner Latest",
            categorys[1].GetProperty("PermittedRoles")[0].GetString());
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

    private static VRChatWorld CreatePortalWorld(
        Guid categoryId,
        string worldId,
        string name) =>
        new(
            Guid.NewGuid(),
            SpotId: null,
            RegisteredByUserId: "owner",
            VRChatWorldId: worldId,
            Name: name,
            RecommendedCapacity: 16,
            Capacity: 32,
            Description: "説明",
            PC: true,
            Android: false,
            IOS: false,
            IsPrivate: false,
            PortalCategoryId: categoryId);

    private static Spot TokyoSpot() =>
        new(
            Guid.NewGuid(), "owner", "東京", 35, 139,
            AreaCodes.Japan.Tokyo, "説明");

    private static PortalCategory Category(
        string name,
        PortalCategoryVisibility visibility,
        string? owner) =>
        new(Guid.NewGuid(), owner ?? "admin", owner, name,
            name.ToUpperInvariant(), visibility);

    private static InMemoryPortalCategoryRepository Categories(
        params PortalCategory[] categories)
    {
        var repository = new InMemoryPortalCategoryRepository();
        foreach (var category in categories)
        {
            repository.Upsert(category);
        }

        return repository;
    }

    private static FakeDiscordUserRepository Users() =>
        new(User("owner", "Owner Latest"), User("other", "Other"), User("admin", "Admin"));

    private static DiscordUser User(string id, string displayName)
    {
        var now = DateTimeOffset.UtcNow;
        return new DiscordUser(
            id, id, null, null, "guild", true, id == "admin",
            now, now, displayName, displayName.ToUpperInvariant());
    }

    private static FakeCurrentActorAccessor Actor(string id, bool isAdmin = false) =>
        new(new CurrentActor(id, isAdmin, HasVRChatDisplayName: true));

    private static GetWorldDataUseCase UseCase(
        FakeSpotRepository spots,
        InMemoryPortalCategoryRepository? categories = null,
        FakeDiscordUserRepository? users = null,
        FakeCurrentActorAccessor? actor = null) =>
        new(
            spots,
            categories ?? new InMemoryPortalCategoryRepository(),
            users ?? new FakeDiscordUserRepository(),
            actor ?? new FakeCurrentActorAccessor(null));
}
