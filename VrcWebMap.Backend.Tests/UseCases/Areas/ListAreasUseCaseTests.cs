using VrcWebMap.Backend.Contracts.Areas;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.UseCases.Areas;

namespace VrcWebMap.Backend.Tests.UseCases.Areas;

public sealed class ListAreasUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsAreaDefinitions()
    {
        var useCase = new ListAreasUseCase();

        var result = await useCase.ExecuteAsync(new ListAreas.Request());

        Assert.True(result.IsSuccess);
        Assert.Equal(AreaDefinitions.All, result.Value!.Areas);
    }
}
