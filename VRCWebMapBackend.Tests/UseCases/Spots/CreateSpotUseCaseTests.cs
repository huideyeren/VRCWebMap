using Kawa.Abstractions;
using VRCWebMapBackend.Contracts.Spots;
using VRCWebMapBackend.Tests.TestDoubles;
using VRCWebMapBackend.UseCases.Spots;

namespace VRCWebMapBackend.Tests.UseCases.Spots;

public sealed class CreateSpotUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ValidRequest_CreatesSpot()
    {
        var repository = new FakeSpotRepository();
        var useCase = new CreateSpotUseCase(repository);
        var request = new CreateSpot.Request(
            "  テストスポット  ",
            35.681236,
            139.767125,
            "  Markdown 説明  ");

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("テストスポット", result.Value.Spot.Name);
        Assert.Equal(35.681236, result.Value.Spot.Latitude);
        Assert.Equal(139.767125, result.Value.Spot.Longitude);
        Assert.Equal("Markdown 説明", result.Value.Spot.Description);
        Assert.NotEqual(Guid.Empty, result.Value.Spot.Id);
        Assert.Single(repository.SavedSpots);
        Assert.Equal(result.Value.Spot, repository.SavedSpots[0]);
    }

    [Theory]
    [InlineData("", 35.681236, 139.767125, "説明", "地図名は必須です。")]
    [InlineData("スポット", -91, 139.767125, "説明", "緯度は -90 から 90 の範囲で指定してください。")]
    [InlineData("スポット", 35.681236, 181, "説明", "経度は -180 から 180 の範囲で指定してください。")]
    [InlineData("スポット", 35.681236, 139.767125, "", "説明は必須です。")]
    public async Task ExecuteAsync_InvalidRequest_ReturnsValidationError(
        string name,
        double latitude,
        double longitude,
        string description,
        string expectedMessage)
    {
        var repository = new FakeSpotRepository();
        var useCase = new CreateSpotUseCase(repository);
        var request = new CreateSpot.Request(name, latitude, longitude, description);

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(KawaErrorKind.Validation, result.Error.Kind);
        Assert.Equal(expectedMessage, result.Error.Message);
        Assert.Empty(repository.SavedSpots);
    }
}
