using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Spots;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.Tests.TestDoubles;
using VrcWebMap.Backend.UseCases.Spots;

namespace VrcWebMap.Backend.Tests.UseCases.Spots;

public sealed class GetSpotUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ExistingSpot_ReturnsSpot()
    {
        var spot = new Spot(Guid.NewGuid(), "スポット", 35.681236, 139.767125, "説明");
        var repository = new FakeSpotRepository(spot);
        var useCase = new GetSpotUseCase(repository);

        var result = await useCase.ExecuteAsync(new GetSpot.Request(spot.Id));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(spot, result.Value.Spot);
    }

    [Fact]
    public async Task ExecuteAsync_MissingSpot_ReturnsNotFound()
    {
        var repository = new FakeSpotRepository();
        var useCase = new GetSpotUseCase(repository);

        var result = await useCase.ExecuteAsync(new GetSpot.Request(Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(KawaErrorKind.NotFound, result.Error.Kind);
        Assert.Equal("スポットが見つかりません。", result.Error.Message);
    }
}
