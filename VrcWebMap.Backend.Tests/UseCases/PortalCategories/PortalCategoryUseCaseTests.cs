using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.PortalCategories;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.Stores;
using VrcWebMap.Backend.Tests.TestDoubles;
using VrcWebMap.Backend.UseCases.PortalCategories;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.Tests.UseCases.PortalCategories;

public sealed class PortalCategoryUseCaseTests
{
    [Fact]
    public async Task Create_GeneralUserCreatesPersonalOwnedBySelf()
    {
        var categories = new InMemoryPortalCategoryRepository();
        var useCase = CreateUseCase(categories, "owner");

        var result = await useCase.ExecuteAsync(
            new CreatePortalCategory.Request(" 私のカテゴリ ", PortalCategoryVisibility.Personal));

        Assert.True(result.IsSuccess);
        Assert.Equal("私のカテゴリ", result.Value!.Category.Name);
        Assert.Equal("Owner VR", result.Value.Category.OwnerDisplayName);
        Assert.True(result.Value.Category.CanEdit);
        var saved = Assert.Single(categories.List());
        Assert.Equal("owner", saved.OwnerUserId);
        Assert.Equal(PortalCategoryVisibility.Personal, saved.Visibility);
    }

    [Fact]
    public async Task Create_AdminCreatesPublicWithNoOwner()
    {
        var categories = new InMemoryPortalCategoryRepository();
        var useCase = CreateUseCase(categories, "admin", isAdmin: true);

        var result = await useCase.ExecuteAsync(
            new CreatePortalCategory.Request("全体公開", PortalCategoryVisibility.Public));

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.Category.OwnerDisplayName);
        Assert.Null(Assert.Single(categories.List()).OwnerUserId);
    }

    [Fact]
    public async Task Create_GeneralUserCannotCreatePublic()
    {
        var useCase = CreateUseCase(new InMemoryPortalCategoryRepository(), "owner");

        var result = await useCase.ExecuteAsync(
            new CreatePortalCategory.Request("全体公開", PortalCategoryVisibility.Public));

        Assert.True(result.IsFailure);
        Assert.Equal(KawaErrorKind.Forbidden, result.Error!.Kind);
    }

    [Fact]
    public async Task Create_DuplicateNormalizedNameReturnsConflict()
    {
        var categories = new InMemoryPortalCategoryRepository();
        categories.Upsert(Category("one", "owner", "My Category"));
        var useCase = CreateUseCase(categories, "owner");

        var result = await useCase.ExecuteAsync(
            new CreatePortalCategory.Request(" my category ", PortalCategoryVisibility.Personal));

        Assert.True(result.IsFailure);
        Assert.Equal(KawaErrorKind.Conflict, result.Error!.Kind);
    }

    [Fact]
    public async Task Update_ChangesOnlyName()
    {
        var categories = new InMemoryPortalCategoryRepository();
        var existing = Category("one", "owner", "変更前");
        categories.Upsert(existing);
        var useCase = new UpdatePortalCategoryUseCase(
            categories,
            new FakeSpotRepository(),
            Users(),
            Actor("owner"));

        var result = await useCase.ExecuteAsync(
            new UpdatePortalCategory.Request(existing.Id, "変更後"));

        Assert.True(result.IsSuccess);
        var saved = Assert.Single(categories.List());
        Assert.Equal("変更後", saved.Name);
        Assert.Equal(existing.OwnerUserId, saved.OwnerUserId);
        Assert.Equal(existing.Visibility, saved.Visibility);
        Assert.Equal(existing.RegisteredByUserId, saved.RegisteredByUserId);
    }

    [Fact]
    public async Task Delete_WithPortalWorldReturnsConflict()
    {
        var categories = new InMemoryPortalCategoryRepository();
        var existing = Category("one", "owner", "個人用");
        categories.Upsert(existing);
        var spots = new FakeSpotRepository();
        spots.AddWorld(World(existing.Id));
        var useCase = new DeletePortalCategoryUseCase(categories, spots, Actor("owner"));

        var result = await useCase.ExecuteAsync(new DeletePortalCategory.Request(existing.Id));

        Assert.True(result.IsFailure);
        Assert.Equal(KawaErrorKind.Conflict, result.Error!.Kind);
    }

    [Fact]
    public async Task List_AppliesAnonymousGeneralAndAdminScopes()
    {
        var categories = new InMemoryPortalCategoryRepository();
        categories.Upsert(Category("public", null, "全体", PortalCategoryVisibility.Public));
        categories.Upsert(Category("owner", "owner", "本人"));
        categories.Upsert(Category("other", "other", "他人"));
        var spots = new FakeSpotRepository();
        var users = Users();

        var anonymous = await new ListPortalCategoriesUseCase(
            categories, spots, users, new FakeCurrentActorAccessor(null))
            .ExecuteAsync(new ListPortalCategories.Request());
        var general = await new ListPortalCategoriesUseCase(
            categories, spots, users, Actor("owner"))
            .ExecuteAsync(new ListPortalCategories.Request());
        var admin = await new ListPortalCategoriesUseCase(
            categories, spots, users, Actor("admin", isAdmin: true))
            .ExecuteAsync(new ListPortalCategories.Request());

        Assert.Equal(["全体"], anonymous.Value!.Categories.Select(item => item.Name));
        Assert.Equal(["全体", "本人"], general.Value!.Categories.Select(item => item.Name));
        Assert.Equal(3, admin.Value!.Categories.Length);
    }

    private static CreatePortalCategoryUseCase CreateUseCase(
        InMemoryPortalCategoryRepository categories,
        string actorId,
        bool isAdmin = false) =>
        new(categories, new FakeSpotRepository(), Users(), Actor(actorId, isAdmin));

    private static FakeCurrentActorAccessor Actor(string id, bool isAdmin = false) =>
        new(new CurrentActor(id, isAdmin, HasVRChatDisplayName: true));

    private static FakeDiscordUserRepository Users() =>
        new(
            User("owner", "Owner VR"),
            User("other", "Other VR"),
            User("admin", "Admin VR"));

    private static DiscordUser User(string id, string displayName)
    {
        var now = DateTimeOffset.UtcNow;
        return new DiscordUser(
            id,
            id,
            null,
            null,
            "guild",
            IsGuildMember: true,
            IsAdmin: id == "admin",
            now,
            now,
            displayName,
            displayName.ToUpperInvariant());
    }

    private static PortalCategory Category(
        string seed,
        string? owner,
        string name,
        PortalCategoryVisibility visibility = PortalCategoryVisibility.Personal) =>
        new(
            Guid.NewGuid(),
            owner ?? "admin",
            owner,
            name,
            name.Trim().ToUpperInvariant(),
            visibility);

    private static VRChatWorld World(Guid categoryId) =>
        new(
            Guid.NewGuid(),
            SpotId: null,
            RegisteredByUserId: "owner",
            VRChatWorldId: "wrld_portal",
            Name: "Portal World",
            RecommendedCapacity: 16,
            Capacity: 32,
            Description: "説明",
            PC: true,
            Android: false,
            IOS: false,
            IsPrivate: false,
            PortalCategoryId: categoryId);
}
