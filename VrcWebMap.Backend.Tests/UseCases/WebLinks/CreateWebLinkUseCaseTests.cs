using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.WebLinks;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.Tests.TestDoubles;
using VrcWebMap.Backend.UseCases.Users;
using VrcWebMap.Backend.UseCases.WebLinks;

namespace VrcWebMap.Backend.Tests.UseCases.WebLinks;

public sealed class CreateWebLinkUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ExistingSpot_CreatesWebLink()
    {
        var spot = new Spot(Guid.NewGuid(), "owner-user", "スポット", 35, 139, AreaCodes.Japan.Tokyo, "説明");
        var repository = new FakeSpotRepository(spot);
        var useCase = CreateUseCase(repository);
        var request = new CreateWebLink.Request(
            spot.Id,
            " 公式サイト ",
            new Uri("https://example.com"));

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(spot.Id, result.Value.WebLink.SpotId);
        Assert.Equal("discord-user-id", result.Value.WebLink.RegisteredByUserId);
        Assert.Equal("公式サイト", result.Value.WebLink.SiteName);
        Assert.Single(repository.SavedWebLinks);
    }

    [Fact]
    public async Task ExecuteAsync_EmptySpotId_ReturnsValidation()
    {
        var repository = new FakeSpotRepository();
        var useCase = CreateUseCase(repository);
        var request = new CreateWebLink.Request(
            Guid.Empty,
            "公式サイト",
            new Uri("https://example.com"));

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(KawaErrorKind.Validation, result.Error.Kind);
        Assert.Empty(repository.SavedWebLinks);
    }

    [Fact]
    public async Task ExecuteAsync_MissingSpot_ReturnsNotFound()
    {
        var repository = new FakeSpotRepository();
        var useCase = CreateUseCase(repository);
        var request = new CreateWebLink.Request(
            Guid.NewGuid(),
            "公式サイト",
            new Uri("https://example.com"));

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(KawaErrorKind.NotFound, result.Error.Kind);
        Assert.Empty(repository.SavedWebLinks);
    }

    private static CreateWebLinkUseCase CreateUseCase(FakeSpotRepository repository) =>
        new(
            repository,
            new FakeCurrentActorAccessor(new CurrentActor(
                "discord-user-id",
                IsAdmin: false,
                HasVRChatDisplayName: true)));
}
