using VRCWebMapBackend.Contracts.Spots;
using VRCWebMapBackend.Models;
using VRCWebMapBackend.Tests.TestDoubles;
using VRCWebMapBackend.UseCases.Spots;

namespace VRCWebMapBackend.Tests.UseCases.Spots;

public sealed class ListSpotsUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsSpotsOrderedByName()
    {
        var spotB = new Spot(Guid.NewGuid(), "B Spot", 35, 139, "B");
        var spotA = new Spot(Guid.NewGuid(), "A Spot", 36, 140, "A");
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
