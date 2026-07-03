using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.PortalWorlds;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.Stores;
using VrcWebMap.Backend.Tests.TestDoubles;
using VrcWebMap.Backend.UseCases.PortalWorlds;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.Tests.UseCases.PortalWorlds;

public sealed class PortalWorldUseCaseTests
{
    [Fact]
    public async Task Create_PersonalOwnerStoresPortalParentAndRegistrant()
    {
        var categories = Categories(Personal("owner"));
        var spots = new FakeSpotRepository();
        var useCase = new CreatePortalWorldUseCase(
            categories, spots, Users(), Actor("owner"));

        var result = await useCase.ExecuteAsync(CreateRequest(categories.List()[0].Id));

        Assert.True(result.IsSuccess);
        var world = Assert.Single(spots.SavedWorlds);
        Assert.Null(world.SpotId);
        Assert.Equal(categories.List()[0].Id, world.PortalCategoryId);
        Assert.Equal("owner", world.RegisteredByUserId);
        Assert.True(result.Value!.World.CanEdit);
        Assert.Equal("Owner VR", result.Value.World.RegisteredByDisplayName);
    }

    [Theory]
    [InlineData("other", false)]
    [InlineData("owner", true)]
    public async Task Create_RequiresPermissionForParent(string actorId, bool publicCategory)
    {
        var category = publicCategory ? Public() : Personal("owner");
        var useCase = new CreatePortalWorldUseCase(
            Categories(category),
            new FakeSpotRepository(),
            Users(),
            Actor(actorId));

        var result = await useCase.ExecuteAsync(CreateRequest(category.Id));

        Assert.True(result.IsFailure);
        Assert.Equal(KawaErrorKind.Forbidden, result.Error!.Kind);
    }

    [Fact]
    public async Task Update_PreservesParentAndRegistrantButUsesCategoryPermission()
    {
        var category = Personal("owner");
        var categories = Categories(category);
        var spots = new FakeSpotRepository();
        var existing = World(category.Id, "other");
        spots.AddWorld(existing);
        var useCase = new UpdatePortalWorldUseCase(
            categories, spots, Users(), Actor("owner"));

        var result = await useCase.ExecuteAsync(new UpdatePortalWorld.Request(
            existing.Id, "wrld_updated", "更新後", 20, 40, "説明更新",
            true, true, false, false));

        Assert.True(result.IsSuccess);
        var saved = Assert.Single(spots.SavedWorlds);
        Assert.Equal(category.Id, saved.PortalCategoryId);
        Assert.Equal("other", saved.RegisteredByUserId);
        Assert.Equal("Other VR", result.Value!.World.RegisteredByDisplayName);
        Assert.True(result.Value.World.CanEdit);
    }

    [Fact]
    public async Task Move_GeneralUserCanMoveOnlyBetweenOwnPersonalCategories()
    {
        var source = Personal("owner");
        var destination = Personal("owner");
        var forbiddenDestination = Personal("other");
        var categories = Categories(source, destination, forbiddenDestination);
        var spots = new FakeSpotRepository();
        var world = World(source.Id, "owner");
        spots.AddWorld(world);
        var useCase = new MovePortalWorldUseCase(
            categories, spots, Users(), Actor("owner"));

        var moved = await useCase.ExecuteAsync(
            new MovePortalWorld.Request(world.Id, destination.Id));
        var forbidden = await useCase.ExecuteAsync(
            new MovePortalWorld.Request(world.Id, forbiddenDestination.Id));

        Assert.True(moved.IsSuccess);
        Assert.Equal(destination.Id, spots.SavedWorlds[0].PortalCategoryId);
        Assert.True(forbidden.IsFailure);
        Assert.Equal(KawaErrorKind.Forbidden, forbidden.Error!.Kind);
    }

    [Fact]
    public async Task Delete_RejectsOtherUserAndAllowsOwner()
    {
        var category = Personal("owner");
        var categories = Categories(category);
        var spots = new FakeSpotRepository();
        var world = World(category.Id, "other");
        spots.AddWorld(world);

        var forbidden = await new DeletePortalWorldUseCase(
                categories, spots, Actor("other"))
            .ExecuteAsync(new DeletePortalWorld.Request(world.Id));
        var deleted = await new DeletePortalWorldUseCase(
                categories, spots, Actor("owner"))
            .ExecuteAsync(new DeletePortalWorld.Request(world.Id));

        Assert.Equal(KawaErrorKind.Forbidden, forbidden.Error!.Kind);
        Assert.True(deleted.IsSuccess);
        Assert.False(spots.TryGetWorld(world.Id, out _));
    }

    private static CreatePortalWorld.Request CreateRequest(Guid categoryId) =>
        new(categoryId, "wrld_portal", "Portal World", 16, 32, "説明",
            true, false, false, false);

    private static PortalCategory Personal(string owner) =>
        new(Guid.NewGuid(), owner, owner, $"個人-{Guid.NewGuid():N}",
            Guid.NewGuid().ToString("N"), PortalCategoryVisibility.Personal);

    private static PortalCategory Public() =>
        new(Guid.NewGuid(), "admin", null, $"公開-{Guid.NewGuid():N}",
            Guid.NewGuid().ToString("N"), PortalCategoryVisibility.Public);

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

    private static VRChatWorld World(Guid categoryId, string registrant) =>
        new(Guid.NewGuid(), null, registrant, "wrld_portal", "Portal World",
            16, 32, "説明", true, false, false, false, categoryId);

    private static FakeCurrentActorAccessor Actor(string id, bool isAdmin = false) =>
        new(new CurrentActor(id, isAdmin, HasVRChatDisplayName: true));

    private static FakeDiscordUserRepository Users() =>
        new(User("owner", "Owner VR"), User("other", "Other VR"), User("admin", "Admin VR"));

    private static DiscordUser User(string id, string displayName)
    {
        var now = DateTimeOffset.UtcNow;
        return new DiscordUser(
            id, id, null, null, "guild", true, id == "admin",
            now, now, displayName, displayName.ToUpperInvariant());
    }
}
