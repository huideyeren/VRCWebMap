using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Spots;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.Tests.TestDoubles;
using VrcWebMap.Backend.UseCases.Spots;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.Tests.UseCases.Spots;

public sealed class UpdateSpotUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ExistingSpot_UpdatesSpot()
    {
        var existing = new Spot(Guid.NewGuid(), "owner-user", "古い名前", 35, 139, AreaCodes.Japan.Tokyo, "古い説明");
        var repository = new FakeSpotRepository(existing);
        var useCase = CreateUseCase(repository, "owner-user");
        var request = new UpdateSpot.Request(
            existing.Id,
            "  新しい名前  ",
            35.681236,
            139.767125,
            AreaCodes.Japan.Osaka,
            "  新しい説明  ");

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(existing.Id, result.Value.Spot.Id);
        Assert.Equal("owner-user", result.Value.Spot.RegisteredByUserId);
        Assert.Equal("新しい名前", result.Value.Spot.Name);
        Assert.Equal(35.681236, result.Value.Spot.Latitude);
        Assert.Equal(139.767125, result.Value.Spot.Longitude);
        Assert.Equal(AreaCodes.Japan.Osaka, result.Value.Spot.AreaCode);
        Assert.Equal("新しい説明", result.Value.Spot.Description);
        Assert.Single(repository.SavedSpots);
        Assert.Equal(result.Value.Spot, repository.SavedSpots[0]);
    }

    [Fact]
    public async Task ExecuteAsync_MissingSpot_ReturnsNotFound()
    {
        var repository = new FakeSpotRepository();
        var useCase = CreateUseCase(repository, "owner-user");
        var request = new UpdateSpot.Request(Guid.NewGuid(), "スポット", 35, 139, AreaCodes.Japan.Tokyo, "説明");

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(KawaErrorKind.NotFound, result.Error.Kind);
        Assert.Equal("スポットが見つかりません。", result.Error.Message);
        Assert.Empty(repository.SavedSpots);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidExistingSpot_ReturnsValidationError()
    {
        var existing = new Spot(Guid.NewGuid(), "owner-user", "スポット", 35, 139, AreaCodes.Japan.Tokyo, "説明");
        var repository = new FakeSpotRepository(existing);
        var useCase = CreateUseCase(repository, "owner-user");
        var request = new UpdateSpot.Request(existing.Id, "", 35, 139, AreaCodes.Japan.Tokyo, "説明");

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(KawaErrorKind.Validation, result.Error.Kind);
        Assert.Equal("地図名は必須です。", result.Error.Message);
        Assert.Empty(repository.SavedSpots);
    }

    [Fact]
    public async Task ExecuteAsync_OtherUser_ReturnsForbidden()
    {
        var existing = new Spot(Guid.NewGuid(), "owner-user", "スポット", 35, 139, AreaCodes.Japan.Tokyo, "説明");
        var repository = new FakeSpotRepository(existing);
        var useCase = CreateUseCase(repository, "other-user");
        var request = new UpdateSpot.Request(existing.Id, "更新", 35, 139, AreaCodes.Japan.Tokyo, "説明");

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(KawaErrorKind.Forbidden, result.Error.Kind);
        Assert.Empty(repository.SavedSpots);
    }

    [Fact]
    public async Task ExecuteAsync_AdminCanUpdateOtherUsersSpot()
    {
        var existing = new Spot(Guid.NewGuid(), "owner-user", "スポット", 35, 139, AreaCodes.Japan.Tokyo, "説明");
        var repository = new FakeSpotRepository(existing);
        var useCase = CreateUseCase(repository, "admin-user", isAdmin: true);
        var request = new UpdateSpot.Request(existing.Id, "更新", 35, 139, AreaCodes.Japan.Tokyo, "説明");

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Single(repository.SavedSpots);
    }

    private static UpdateSpotUseCase CreateUseCase(
        FakeSpotRepository repository,
        string userId,
        bool isAdmin = false) =>
        new(
            repository,
            new FakeCurrentActorAccessor(new CurrentActor(userId, isAdmin, HasVRChatDisplayName: true)));
}
