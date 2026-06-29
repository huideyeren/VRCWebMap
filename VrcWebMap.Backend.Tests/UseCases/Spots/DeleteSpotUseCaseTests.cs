using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Spots;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.Tests.TestDoubles;
using VrcWebMap.Backend.UseCases.Spots;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.Tests.UseCases.Spots;

public sealed class DeleteSpotUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Admin_DeletesSpot()
    {
        var spot = new Spot(Guid.NewGuid(), "owner-user", "スポット", 35, 139, AreaCodes.Japan.Tokyo, "説明");
        var repository = new FakeSpotRepository(spot);
        var useCase = CreateUseCase(repository, "admin-user", isAdmin: true);

        var result = await useCase.ExecuteAsync(new DeleteSpot.Request(spot.Id));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(spot.Id, result.Value.Id);
        Assert.Contains(spot.Id, repository.DeletedSpotIds);
        Assert.False(repository.Exists(spot.Id));
    }

    [Fact]
    public async Task ExecuteAsync_Owner_ReturnsForbidden()
    {
        var spot = new Spot(Guid.NewGuid(), "owner-user", "スポット", 35, 139, AreaCodes.Japan.Tokyo, "説明");
        var repository = new FakeSpotRepository(spot);
        var useCase = CreateUseCase(repository, "owner-user");

        var result = await useCase.ExecuteAsync(new DeleteSpot.Request(spot.Id));

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(KawaErrorKind.Forbidden, result.Error.Kind);
        Assert.True(repository.Exists(spot.Id));
        Assert.DoesNotContain(spot.Id, repository.DeletedSpotIds);
    }

    [Fact]
    public async Task ExecuteAsync_MissingSpot_ReturnsNotFound()
    {
        var missingId = Guid.NewGuid();
        var repository = new FakeSpotRepository();
        var useCase = CreateUseCase(repository, "owner-user");

        var result = await useCase.ExecuteAsync(new DeleteSpot.Request(missingId));

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(KawaErrorKind.NotFound, result.Error.Kind);
        Assert.Equal("スポットが見つかりません。", result.Error.Message);
        Assert.DoesNotContain(missingId, repository.DeletedSpotIds);
    }

    [Fact]
    public async Task ExecuteAsync_OtherUser_ReturnsForbidden()
    {
        var spot = new Spot(Guid.NewGuid(), "owner-user", "スポット", 35, 139, AreaCodes.Japan.Tokyo, "説明");
        var repository = new FakeSpotRepository(spot);
        var useCase = CreateUseCase(repository, "other-user");

        var result = await useCase.ExecuteAsync(new DeleteSpot.Request(spot.Id));

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(KawaErrorKind.Forbidden, result.Error.Kind);
        Assert.True(repository.Exists(spot.Id));
    }

    [Fact]
    public async Task ExecuteAsync_RelatedDataExists_ReturnsConflict()
    {
        var spot = new Spot(Guid.NewGuid(), "owner-user", "スポット", 35, 139, AreaCodes.Japan.Tokyo, "説明");
        var repository = new FakeSpotRepository(spot);
        repository.UpsertWebLink(new WebLink(
            Guid.NewGuid(),
            spot.Id,
            "owner-user",
            "公式サイト",
            new Uri("https://example.com")));
        var useCase = CreateUseCase(repository, "admin-user", isAdmin: true);

        var result = await useCase.ExecuteAsync(new DeleteSpot.Request(spot.Id));

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(KawaErrorKind.Conflict, result.Error.Kind);
        Assert.True(repository.Exists(spot.Id));
        Assert.DoesNotContain(spot.Id, repository.DeletedSpotIds);
    }

    private static DeleteSpotUseCase CreateUseCase(
        FakeSpotRepository repository,
        string userId,
        bool isAdmin = false) =>
        new(
            repository,
            new FakeCurrentActorAccessor(new CurrentActor(userId, isAdmin, HasVRChatDisplayName: true)));
}
