using VrcWebMap.Backend.Contracts.Spots;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.Options;
using VrcWebMap.Backend.Tests.TestDoubles;
using VrcWebMap.Backend.UseCases.Spots;

namespace VrcWebMap.Backend.Tests.UseCases.Spots;

public sealed class ExportKmlSpotsUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_SelectedSpot_ExportsProvenanceAndDirectLink()
    {
        var spot = new Spot(Guid.NewGuid(), "owner-user", "井の頭公園駅", 35.697484, 139.582739, AreaCodes.Japan.Tokyo, "VRChatの聖地");
        var useCase = new ExportKmlSpotsUseCase(
            new FakeSpotRepository(spot),
            FakeDiscordUserRepository.WithVRChatDisplayName("owner-user", "Karina"),
            new AppOptions { PublicBaseUrl = "https://maps.example.test" });

        var result = await useCase.ExecuteAsync(new ExportKmlSpots.Request([spot.Id]));

        Assert.True(result.IsSuccess);
        Assert.Contains("出典: VRC Web Map", result.Value!.Content);
        Assert.Contains("登録者: Karina", result.Value.Content);
        Assert.Contains($"https://maps.example.test/?spotId={spot.Id}", result.Value.Content);
        Assert.Contains(spot.Id.ToString(), result.Value.Content);
    }

    [Fact]
    public async Task ExecuteAsync_PublicBaseUrlWithPath_PreservesConfiguredPathInDirectLink()
    {
        var spot = new Spot(Guid.NewGuid(), "owner-user", "井の頭公園駅", 35.697484, 139.582739, AreaCodes.Japan.Tokyo, "VRChatの聖地");
        var useCase = new ExportKmlSpotsUseCase(
            new FakeSpotRepository(spot),
            FakeDiscordUserRepository.WithVRChatDisplayName("owner-user", "Karina"),
            new AppOptions { PublicBaseUrl = "https://maps.example.test/vrc-map/" });

        var result = await useCase.ExecuteAsync(new ExportKmlSpots.Request([spot.Id]));

        Assert.True(result.IsSuccess);
        Assert.Contains($"https://maps.example.test/vrc-map/?spotId={spot.Id}", result.Value!.Content);
    }
}
