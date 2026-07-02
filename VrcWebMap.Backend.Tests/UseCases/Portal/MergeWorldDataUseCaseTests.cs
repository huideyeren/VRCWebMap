using System.Text.Json.Nodes;
using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Portal;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.Stores;
using VrcWebMap.Backend.Tests.TestDoubles;
using VrcWebMap.Backend.UseCases.Portal;

namespace VrcWebMap.Backend.Tests.UseCases.Portal;

public sealed class MergeWorldDataUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_AllowsAnonymousAndExcludesPersonalData()
    {
        var categories = new InMemoryPortalCategoryRepository();
        categories.Upsert(Category("公開", PortalCategoryVisibility.Public, null));
        categories.Upsert(Category("個人", PortalCategoryVisibility.Personal, "owner"));
        var worldData = new GetWorldDataUseCase(
            new FakeSpotRepository(),
            categories,
            new FakeDiscordUserRepository(),
            new FakeCurrentActorAccessor(null));
        var useCase = new MergeWorldDataUseCase(worldData);

        var result = await useCase.ExecuteAsync(
            new MergeWorldData.Request("""{"Categorys":[]}"""));

        Assert.True(result.IsSuccess);
        var root = JsonNode.Parse(result.Value!.MergedJson)!;
        var category = Assert.Single(root["Categorys"]!.AsArray());
        Assert.Equal("公開", category!["Category"]!.GetValue<string>());
        Assert.Null(root["Roles"]);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsUtf8PayloadOverFiveMiB()
    {
        var worldData = new GetWorldDataUseCase(
            new FakeSpotRepository(),
            new InMemoryPortalCategoryRepository(),
            new FakeDiscordUserRepository(),
            new FakeCurrentActorAccessor(null));
        var useCase = new MergeWorldDataUseCase(worldData);
        var oversized = "{\"Categorys\":[],\"Padding\":\"" +
            new string('a', 5 * 1024 * 1024) +
            "\"}";

        var result = await useCase.ExecuteAsync(new MergeWorldData.Request(oversized));

        Assert.True(result.IsFailure);
        Assert.Equal(KawaErrorKind.Validation, result.Error!.Kind);
    }

    private static PortalCategory Category(
        string name,
        PortalCategoryVisibility visibility,
        string? owner) =>
        new(Guid.NewGuid(), owner ?? "admin", owner, name, name, visibility);
}
