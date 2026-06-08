using Kawa.Abstractions;
using VRCWebMapBackend.Contracts.Spots;
using VRCWebMapBackend.Models;
using VRCWebMapBackend.Tests.TestDoubles;
using VRCWebMapBackend.UseCases.Spots;

namespace VRCWebMapBackend.Tests.UseCases.Spots;

public sealed class DeleteSpotUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ExistingSpot_DeletesSpot()
    {
        var spot = new Spot(Guid.NewGuid(), "スポット", 35, 139, "説明");
        var repository = new FakeSpotRepository(spot);
        var useCase = new DeleteSpotUseCase(repository);

        var result = await useCase.ExecuteAsync(new DeleteSpot.Request(spot.Id));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(spot.Id, result.Value.Id);
        Assert.Contains(spot.Id, repository.DeletedSpotIds);
        Assert.False(repository.Exists(spot.Id));
    }

    [Fact]
    public async Task ExecuteAsync_MissingSpot_ReturnsNotFound()
    {
        var missingId = Guid.NewGuid();
        var repository = new FakeSpotRepository();
        var useCase = new DeleteSpotUseCase(repository);

        var result = await useCase.ExecuteAsync(new DeleteSpot.Request(missingId));

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(KawaErrorKind.NotFound, result.Error.Kind);
        Assert.Equal("スポットが見つかりません。", result.Error.Message);
        Assert.Contains(missingId, repository.DeletedSpotIds);
    }
}
