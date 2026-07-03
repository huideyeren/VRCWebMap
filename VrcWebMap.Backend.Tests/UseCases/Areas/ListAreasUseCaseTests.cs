using VrcWebMap.Backend.Contracts.Areas;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.UseCases.Areas;

namespace VrcWebMap.Backend.Tests.UseCases.Areas;

public sealed class ListAreasUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsAreaDefinitionsWithCategoryMetadata()
    {
        var useCase = new ListAreasUseCase();

        var result = await useCase.ExecuteAsync(new ListAreas.Request());

        Assert.True(result.IsSuccess);
        Assert.Equal(AreaDefinitions.All.Length, result.Value!.Areas.Length);
        var tokyo = Assert.Single(
            result.Value.Areas,
            area => area.AreaCode == AreaCodes.Japan.Tokyo);
        Assert.Equal("東京都", tokyo.AreaName);
        Assert.Equal(AreaCategory.Kanto, tokyo.Category);
        Assert.Equal("関東", tokyo.CategoryName);
        Assert.Equal(2, tokyo.CategoryOrder);

        var mie = Assert.Single(
            result.Value.Areas,
            area => area.AreaCode == AreaCodes.Japan.Mie);
        Assert.Equal(AreaCategory.Chubu, mie.Category);
        Assert.Equal("中部", mie.CategoryName);
    }
}
