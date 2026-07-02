using VrcWebMap.Backend.Contracts.Spots;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.Tests.TestDoubles;
using VrcWebMap.Backend.UseCases.Spots;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.Tests.UseCases.Spots;

public sealed class ListSpotsUseCaseTests
{
    [Fact]
    public void ResponseItem_ExposesDisplayNameAndCanEditWithoutInternalUserId()
    {
        var itemType = typeof(ListSpots.Response)
            .GetProperty(nameof(ListSpots.Response.Spots))!
            .PropertyType
            .GetElementType()!;
        var propertyNames = itemType.GetProperties().Select(property => property.Name).ToArray();

        Assert.Contains("RegisteredByDisplayName", propertyNames);
        Assert.Contains("CanEdit", propertyNames);
        Assert.DoesNotContain("RegisteredByUserId", propertyNames);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsSpotsOrderedByName()
    {
        var spotB = new Spot(Guid.NewGuid(), "owner-user", "B Spot", 35, 139, AreaCodes.Japan.Tokyo, "B");
        var spotA = new Spot(Guid.NewGuid(), "owner-user", "A Spot", 36, 140, AreaCodes.Japan.Osaka, "A");
        var repository = new FakeSpotRepository(spotB, spotA);
        var useCase = CreateUseCase(repository);

        var result = await useCase.ExecuteAsync(new ListSpots.Request());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Collection(
            result.Value.Spots,
            spot => Assert.Equal(spotA.Id, spot.Id),
            spot => Assert.Equal(spotB.Id, spot.Id));
    }

    [Fact]
    public async Task ExecuteAsync_QueryMatchesSpotName()
    {
        var akihabara = new Spot(Guid.NewGuid(), "owner-user", "Akihabara Station", 35, 139, AreaCodes.Japan.Tokyo, "電気街の集合場所です。");
        var namba = new Spot(Guid.NewGuid(), "owner-user", "Namba", 34, 135, AreaCodes.Japan.Osaka, "大阪の集合場所です。");
        var repository = new FakeSpotRepository(namba, akihabara);
        var useCase = CreateUseCase(repository);

        var result = await useCase.ExecuteAsync(new ListSpots.Request("akihabara"));

        Assert.True(result.IsSuccess);
        var spot = Assert.Single(result.Value!.Spots);
        Assert.Equal(akihabara.Id, spot.Id);
    }

    [Fact]
    public async Task ExecuteAsync_QueryMatchesSpotDescription()
    {
        var portal = new Spot(Guid.NewGuid(), "owner-user", "Portal Hub", 35, 139, AreaCodes.Japan.Tokyo, "VRChat ワールドの入口です。");
        var cafe = new Spot(Guid.NewGuid(), "owner-user", "Cafe", 34, 135, AreaCodes.Japan.Osaka, "休憩場所です。");
        var repository = new FakeSpotRepository(cafe, portal);
        var useCase = CreateUseCase(repository);

        var result = await useCase.ExecuteAsync(new ListSpots.Request("ワールド"));

        Assert.True(result.IsSuccess);
        var spot = Assert.Single(result.Value!.Spots);
        Assert.Equal(portal.Id, spot.Id);
    }

    [Fact]
    public async Task ExecuteAsync_QueryRequiresAllTermsAcrossNameAndDescription()
    {
        var matched = new Spot(Guid.NewGuid(), "owner-user", "Tokyo Portal", 35, 139, AreaCodes.Japan.Tokyo, "VRChat ワールドの入口です。");
        var nameOnly = new Spot(Guid.NewGuid(), "owner-user", "Tokyo Cafe", 35, 139, AreaCodes.Japan.Tokyo, "休憩場所です。");
        var descriptionOnly = new Spot(Guid.NewGuid(), "owner-user", "Portal", 35, 139, AreaCodes.Japan.Tokyo, "VRChat ワールドの入口です。");
        var repository = new FakeSpotRepository(nameOnly, matched, descriptionOnly);
        var useCase = CreateUseCase(repository);

        var result = await useCase.ExecuteAsync(new ListSpots.Request("Tokyo ワールド"));

        Assert.True(result.IsSuccess);
        var spot = Assert.Single(result.Value!.Spots);
        Assert.Equal(matched.Id, spot.Id);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsLinkedContentFlagsForEachSpot()
    {
        var normal = new Spot(Guid.NewGuid(), "owner-user", "Normal", 35, 139, AreaCodes.Japan.Tokyo, "説明");
        var place = new Spot(Guid.NewGuid(), "owner-user", "Place", 35, 139, AreaCodes.Japan.Tokyo, "説明");
        var world = new Spot(Guid.NewGuid(), "owner-user", "World", 35, 139, AreaCodes.Japan.Tokyo, "説明");
        var both = new Spot(Guid.NewGuid(), "owner-user", "Both", 35, 139, AreaCodes.Japan.Tokyo, "説明");
        var repository = new FakeSpotRepository(normal, place, world, both);
        repository.UpsertPlaceInfo(new PlaceInfo(Guid.NewGuid(), place.Id, "owner-user", "施設", "住所", "営業情報"));
        repository.UpsertPlaceInfo(new PlaceInfo(Guid.NewGuid(), both.Id, "owner-user", "施設", "住所", "営業情報"));
        repository.AddWorld(new VRChatWorld(Guid.NewGuid(), world.Id, "owner-user", "wrld_world", "World", 8, 16, "説明", true, false, false));
        repository.AddWorld(new VRChatWorld(Guid.NewGuid(), both.Id, "owner-user", "wrld_both", "Both", 8, 16, "説明", true, false, false));
        var useCase = CreateUseCase(repository);

        var result = await useCase.ExecuteAsync(new ListSpots.Request());
        var items = result.Value!.Spots.ToDictionary(spot => spot.Id);

        Assert.False(items[normal.Id].HasVRChatWorld);
        Assert.False(items[normal.Id].HasPlaceInfo);
        Assert.True(items[place.Id].HasPlaceInfo);
        Assert.False(items[place.Id].HasVRChatWorld);
        Assert.True(items[world.Id].HasVRChatWorld);
        Assert.False(items[world.Id].HasPlaceInfo);
        Assert.True(items[both.Id].HasVRChatWorld);
        Assert.True(items[both.Id].HasPlaceInfo);
    }

    private static ListSpotsUseCase CreateUseCase(FakeSpotRepository repository) =>
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
