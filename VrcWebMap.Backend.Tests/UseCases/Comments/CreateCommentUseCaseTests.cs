using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Comments;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.Tests.TestDoubles;
using VrcWebMap.Backend.UseCases.Comments;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.Tests.UseCases.Comments;

public sealed class CreateCommentUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ExistingSpot_CreatesComment()
    {
        var spot = new Spot(Guid.NewGuid(), "owner-user", "スポット", 35, 139, AreaCodes.Japan.Tokyo, "説明");
        var repository = new FakeSpotRepository(spot);
        var useCase = CreateUseCase(repository);
        var request = new CreateComment.Request(
            spot.Id,
            " コメント本文 ");

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("VRChat User", result.Value.Comment.RegisteredByDisplayName);
        Assert.True(result.Value.Comment.CanEdit);
        Assert.Equal("コメント本文", result.Value.Comment.Comments);
        var saved = Assert.Single(repository.SavedComments);
        Assert.Equal(spot.Id, saved.SpotId);
        Assert.Equal("discord-user-id", saved.RegisteredByUserId);
    }

    [Fact]
    public async Task ExecuteAsync_EmptySpotId_ReturnsValidation()
    {
        var repository = new FakeSpotRepository();
        var useCase = CreateUseCase(repository);
        var request = new CreateComment.Request(
            Guid.Empty,
            "コメント本文");

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(KawaErrorKind.Validation, result.Error.Kind);
        Assert.Empty(repository.SavedComments);
    }

    [Fact]
    public async Task ExecuteAsync_MissingSpot_ReturnsNotFound()
    {
        var repository = new FakeSpotRepository();
        var useCase = CreateUseCase(repository);
        var request = new CreateComment.Request(
            Guid.NewGuid(),
            "コメント本文");

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(KawaErrorKind.NotFound, result.Error.Kind);
        Assert.Empty(repository.SavedComments);
    }

    private static CreateCommentUseCase CreateUseCase(FakeSpotRepository repository) =>
        new(
            repository,
            FakeDiscordUserRepository.WithVRChatDisplayName("discord-user-id"),
            new FakeCurrentActorAccessor(new CurrentActor(
                "discord-user-id",
                IsAdmin: false,
                HasVRChatDisplayName: true)));
}
