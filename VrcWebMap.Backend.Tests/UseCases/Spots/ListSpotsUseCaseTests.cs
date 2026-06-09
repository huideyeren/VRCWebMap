using VrcWebMap.Backend.Contracts.Spots;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.Tests.TestDoubles;
using VrcWebMap.Backend.UseCases.Spots;

namespace VrcWebMap.Backend.Tests.UseCases.Spots;

public sealed class ListSpotsUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsSpotsOrderedByName()
    {
        var spotB = new Spot(Guid.NewGuid(), "owner-user", "B Spot", 35, 139, AreaCodes.Japan.Tokyo, "B");
        var spotA = new Spot(Guid.NewGuid(), "owner-user", "A Spot", 36, 140, AreaCodes.Japan.Osaka, "A");
        var repository = new FakeSpotRepository(spotB, spotA);
        var useCase = new ListSpotsUseCase(repository);

        var result = await useCase.ExecuteAsync(new ListSpots.Request());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Collection(
            result.Value.Spots,
            spot => Assert.Equal(spotA, spot),
            spot => Assert.Equal(spotB, spot));
    }
}
