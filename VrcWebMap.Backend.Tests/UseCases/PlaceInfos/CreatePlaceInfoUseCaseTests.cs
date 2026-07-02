using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.PlaceInfos;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.Tests.TestDoubles;
using VrcWebMap.Backend.UseCases.PlaceInfos;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.Tests.UseCases.PlaceInfos;

public sealed class CreatePlaceInfoUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ExistingSpot_CreatesPlaceInfo()
    {
        var spot = new Spot(Guid.NewGuid(), "owner-user", "スポット", 35, 139, AreaCodes.Japan.Tokyo, "説明");
        var repository = new FakeSpotRepository(spot);
        var useCase = CreateUseCase(repository);
        var request = new CreatePlaceInfo.Request(
            spot.Id,
            " サンプル飲食店 ",
            " 東京都千代田区 ",
            BusinessInformation: " - 昼: 11:00-14:00\n- 夜: 17:00-22:00\n- 定休日: 不定休 ");

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("VRChat User", result.Value.PlaceInfo.RegisteredByDisplayName);
        Assert.True(result.Value.PlaceInfo.CanEdit);
        Assert.Equal("サンプル飲食店", result.Value.PlaceInfo.Name);
        var saved = Assert.Single(repository.SavedPlaceInfos);
        Assert.Equal(spot.Id, saved.SpotId);
        Assert.Equal("discord-user-id", saved.RegisteredByUserId);
    }

    [Fact]
    public async Task ExecuteAsync_EmptySpotId_ReturnsValidation()
    {
        var repository = new FakeSpotRepository();
        var useCase = CreateUseCase(repository);
        var request = new CreatePlaceInfo.Request(
            Guid.Empty,
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
        var useCase = CreateUseCase(repository);
        var request = new CreatePlaceInfo.Request(
            Guid.NewGuid(),
            "サンプル飲食店",
            "東京都千代田区",
            BusinessInformation: "- 昼: 11:00-14:00\n- 夜: 17:00-22:00\n- 定休日: 不定休");

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(KawaErrorKind.NotFound, result.Error.Kind);
        Assert.Empty(repository.SavedPlaceInfos);
    }

    private static CreatePlaceInfoUseCase CreateUseCase(FakeSpotRepository repository) =>
        new(
            repository,
            FakeDiscordUserRepository.WithVRChatDisplayName("discord-user-id"),
            new FakeCurrentActorAccessor(new CurrentActor(
                "discord-user-id",
                IsAdmin: false,
                HasVRChatDisplayName: true)));
}
