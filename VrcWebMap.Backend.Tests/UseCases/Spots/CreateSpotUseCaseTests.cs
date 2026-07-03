using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Spots;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.Tests.TestDoubles;
using VrcWebMap.Backend.UseCases.Spots;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.Tests.UseCases.Spots;

public sealed class CreateSpotUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ValidRequest_CreatesSpot()
    {
        var repository = new FakeSpotRepository();
        var useCase = new CreateSpotUseCase(
            repository,
            FakeDiscordUserRepository.WithVRChatDisplayName("owner-user", "Owner"),
            new FakeCurrentActorAccessor(new CurrentActor("owner-user", IsAdmin: false, HasVRChatDisplayName: true)));
        var request = new CreateSpot.Request(
            "  テストスポット  ",
            35.681236,
            139.767125,
            AreaCodes.Japan.Tokyo,
            "  Markdown 説明  ");

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Owner", result.Value.Spot.RegisteredByDisplayName);
        Assert.True(result.Value.Spot.CanEdit);
        Assert.Equal("テストスポット", result.Value.Spot.Name);
        Assert.Equal(35.681236, result.Value.Spot.Latitude);
        Assert.Equal(139.767125, result.Value.Spot.Longitude);
        Assert.Equal(AreaCodes.Japan.Tokyo, result.Value.Spot.AreaCode);
        Assert.Equal("Markdown 説明", result.Value.Spot.Description);
        Assert.NotEqual(Guid.Empty, result.Value.Spot.Id);
        var saved = Assert.Single(repository.SavedSpots);
        Assert.Equal("owner-user", saved.RegisteredByUserId);
        Assert.Equal(result.Value.Spot.Id, saved.Id);
    }

    [Theory]
    [InlineData("", 35.681236, 139.767125, AreaCodes.Japan.Tokyo, "説明", "地図名は必須です。")]
    [InlineData("スポット", -91, 139.767125, AreaCodes.Japan.Tokyo, "説明", "緯度は -90 から 90 の範囲で指定してください。")]
    [InlineData("スポット", 35.681236, 181, AreaCodes.Japan.Tokyo, "説明", "経度は -180 から 180 の範囲で指定してください。")]
    [InlineData("スポット", 35.681236, 139.767125, 999, "説明", "地域コードは定義済みの値を指定してください。")]
    [InlineData("スポット", 35.681236, 139.767125, AreaCodes.Japan.Tokyo, "", "説明は必須です。")]
    public async Task ExecuteAsync_InvalidRequest_ReturnsValidationError(
        string name,
        double latitude,
        double longitude,
        int areaCode,
        string description,
        string expectedMessage)
    {
        var repository = new FakeSpotRepository();
        var useCase = new CreateSpotUseCase(
            repository,
            FakeDiscordUserRepository.WithVRChatDisplayName("owner-user"),
            new FakeCurrentActorAccessor(new CurrentActor("owner-user", IsAdmin: false, HasVRChatDisplayName: true)));
        var request = new CreateSpot.Request(name, latitude, longitude, areaCode, description);

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(KawaErrorKind.Validation, result.Error.Kind);
        Assert.Equal(expectedMessage, result.Error.Message);
        Assert.Empty(repository.SavedSpots);
    }

    [Fact]
    public async Task ExecuteAsync_ActorWithoutVRChatDisplayName_ReturnsForbidden()
    {
        var repository = new FakeSpotRepository();
        var useCase = new CreateSpotUseCase(
            repository,
            FakeDiscordUserRepository.WithVRChatDisplayName("owner-user"),
            new FakeCurrentActorAccessor(new CurrentActor("owner-user", IsAdmin: false, HasVRChatDisplayName: false)));
        var request = new CreateSpot.Request(
            "スポット",
            35.681236,
            139.767125,
            AreaCodes.Japan.Tokyo,
            "説明");

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal(KawaErrorKind.Forbidden, result.Error!.Kind);
        Assert.Empty(repository.SavedSpots);
    }
}
