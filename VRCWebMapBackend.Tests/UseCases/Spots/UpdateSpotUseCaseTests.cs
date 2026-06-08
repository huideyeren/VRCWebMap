using Kawa.Abstractions;
using VRCWebMapBackend.Contracts.Spots;
using VRCWebMapBackend.Models;
using VRCWebMapBackend.Tests.TestDoubles;
using VRCWebMapBackend.UseCases.Spots;

namespace VRCWebMapBackend.Tests.UseCases.Spots;

public sealed class UpdateSpotUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ExistingSpot_UpdatesSpot()
    {
        var existing = new Spot(Guid.NewGuid(), "古い名前", 35, 139, "古い説明");
        var repository = new FakeSpotRepository(existing);
        var useCase = new UpdateSpotUseCase(repository);
        var request = new UpdateSpot.Request(
            existing.Id,
            "  新しい名前  ",
            35.681236,
            139.767125,
            "  新しい説明  ");

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(existing.Id, result.Value.Spot.Id);
        Assert.Equal("新しい名前", result.Value.Spot.Name);
        Assert.Equal(35.681236, result.Value.Spot.Latitude);
        Assert.Equal(139.767125, result.Value.Spot.Longitude);
        Assert.Equal("新しい説明", result.Value.Spot.Description);
        Assert.Single(repository.SavedSpots);
        Assert.Equal(result.Value.Spot, repository.SavedSpots[0]);
    }

    [Fact]
    public async Task ExecuteAsync_MissingSpot_ReturnsNotFound()
    {
        var repository = new FakeSpotRepository();
        var useCase = new UpdateSpotUseCase(repository);
        var request = new UpdateSpot.Request(Guid.NewGuid(), "スポット", 35, 139, "説明");

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
        var existing = new Spot(Guid.NewGuid(), "スポット", 35, 139, "説明");
        var repository = new FakeSpotRepository(existing);
        var useCase = new UpdateSpotUseCase(repository);
        var request = new UpdateSpot.Request(existing.Id, "", 35, 139, "説明");

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(KawaErrorKind.Validation, result.Error.Kind);
        Assert.Equal("地図名は必須です。", result.Error.Message);
        Assert.Empty(repository.SavedSpots);
    }
}
