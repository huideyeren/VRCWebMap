using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Restaurants;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.Tests.TestDoubles;
using VrcWebMap.Backend.UseCases.Restaurants;

namespace VrcWebMap.Backend.Tests.UseCases.Restaurants;

public sealed class CreateRestaurantUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ExistingSpot_CreatesRestaurant()
    {
        var spot = new Spot(Guid.NewGuid(), "owner-user", "スポット", 35, 139, AreaCodes.Japan.Tokyo, "説明");
        var repository = new FakeSpotRepository(spot);
        var useCase = new CreateRestaurantUseCase(repository);
        var request = new CreateRestaurant.Request(
            spot.Id,
            " discord-user-id ",
            " サンプル飲食店 ",
            " 東京都千代田区 ",
            Url: null,
            GurunaviUrl: null,
            TabelogUrl: null,
            RettyUrl: null,
            XUrl: null,
            InstagramUrl: null,
            OpenTime: new TimeOnly(11, 0),
            CloseTime: new TimeOnly(22, 0),
            ClosedOn: " 不定休 ");

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(spot.Id, result.Value.Restaurant.SpotId);
        Assert.Equal("discord-user-id", result.Value.Restaurant.RegisteredByUserId);
        Assert.Equal("サンプル飲食店", result.Value.Restaurant.Name);
        Assert.Single(repository.SavedRestaurants);
    }

    [Fact]
    public async Task ExecuteAsync_EmptySpotId_ReturnsValidation()
    {
        var repository = new FakeSpotRepository();
        var useCase = new CreateRestaurantUseCase(repository);
        var request = new CreateRestaurant.Request(
            Guid.Empty,
            "discord-user-id",
            "サンプル飲食店",
            "東京都千代田区",
            Url: null,
            GurunaviUrl: null,
            TabelogUrl: null,
            RettyUrl: null,
            XUrl: null,
            InstagramUrl: null,
            OpenTime: new TimeOnly(11, 0),
            CloseTime: new TimeOnly(22, 0),
            ClosedOn: "不定休");

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(KawaErrorKind.Validation, result.Error.Kind);
        Assert.Empty(repository.SavedRestaurants);
    }

    [Fact]
    public async Task ExecuteAsync_MissingSpot_ReturnsNotFound()
    {
        var repository = new FakeSpotRepository();
        var useCase = new CreateRestaurantUseCase(repository);
        var request = new CreateRestaurant.Request(
            Guid.NewGuid(),
            "discord-user-id",
            "サンプル飲食店",
            "東京都千代田区",
            Url: null,
            GurunaviUrl: null,
            TabelogUrl: null,
            RettyUrl: null,
            XUrl: null,
            InstagramUrl: null,
            OpenTime: new TimeOnly(11, 0),
            CloseTime: new TimeOnly(22, 0),
            ClosedOn: "不定休");

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(KawaErrorKind.NotFound, result.Error.Kind);
        Assert.Empty(repository.SavedRestaurants);
    }
}
