using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.PlaceInfos;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.Tests.TestDoubles;
using VrcWebMap.Backend.UseCases.PlaceInfos;

namespace VrcWebMap.Backend.Tests.UseCases.PlaceInfos;

public sealed class CreatePlaceInfoUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ExistingSpot_CreatesPlaceInfo()
    {
        var spot = new Spot(Guid.NewGuid(), "owner-user", "スポット", 35, 139, AreaCodes.Japan.Tokyo, "説明");
        var repository = new FakeSpotRepository(spot);
        var useCase = new CreatePlaceInfoUseCase(repository);
        var request = new CreatePlaceInfo.Request(
            spot.Id,
            " discord-user-id ",
            " サンプル飲食店 ",
            " 東京都千代田区 ",
            BusinessInformation: " - 昼: 11:00-14:00\n- 夜: 17:00-22:00\n- 定休日: 不定休 ");

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(spot.Id, result.Value.PlaceInfo.SpotId);
        Assert.Equal("discord-user-id", result.Value.PlaceInfo.RegisteredByUserId);
        Assert.Equal("サンプル飲食店", result.Value.PlaceInfo.Name);
        Assert.Single(repository.SavedPlaceInfos);
    }

    [Fact]
    public async Task ExecuteAsync_EmptySpotId_ReturnsValidation()
    {
        var repository = new FakeSpotRepository();
        var useCase = new CreatePlaceInfoUseCase(repository);
        var request = new CreatePlaceInfo.Request(
            Guid.Empty,
            "discord-user-id",
            "サンプル飲食店",
            "東京都千代田区",
            BusinessInformation: "- 昼: 11:00-14:00\n- 夜: 17:00-22:00\n- 定休日: 不定休");

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(KawaErrorKind.Validation, result.Error.Kind);
        Assert.Empty(repository.SavedPlaceInfos);
    }

    [Fact]
    public async Task ExecuteAsync_MissingSpot_ReturnsNotFound()
    {
        var repository = new FakeSpotRepository();
        var useCase = new CreatePlaceInfoUseCase(repository);
        var request = new CreatePlaceInfo.Request(
            Guid.NewGuid(),
            "discord-user-id",
            "サンプル飲食店",
            "東京都千代田区",
            BusinessInformation: "- 昼: 11:00-14:00\n- 夜: 17:00-22:00\n- 定休日: 不定休");

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(KawaErrorKind.NotFound, result.Error.Kind);
        Assert.Empty(repository.SavedPlaceInfos);
    }
}
