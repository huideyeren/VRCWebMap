using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Comments;
using VrcWebMap.Backend.Contracts.PlaceInfos;
using VrcWebMap.Backend.Contracts.VRChatWorlds;
using VrcWebMap.Backend.Contracts.WebLinks;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.Tests.TestDoubles;
using VrcWebMap.Backend.UseCases.Comments;
using VrcWebMap.Backend.UseCases.PlaceInfos;
using VrcWebMap.Backend.UseCases.Users;
using VrcWebMap.Backend.UseCases.VRChatWorlds;
using VrcWebMap.Backend.UseCases.WebLinks;

namespace VrcWebMap.Backend.Tests.UseCases;

public sealed class RelatedDataDeleteAuthorizationTests
{
    [Fact]
    public async Task DeleteVRChatWorld_Owner_ReturnsForbidden()
    {
        var repository = new FakeSpotRepository();
        var world = new VRChatWorld(Guid.NewGuid(), Guid.NewGuid(), "owner-user", "wrld_123", "World", 8, 16, "説明", PC: true, Android: false, IOS: false);
        repository.AddWorld(world);
        var useCase = new DeleteVRChatWorldUseCase(repository, Owner());

        var result = await useCase.ExecuteAsync(new DeleteVRChatWorld.Request(world.Id));

        AssertForbidden(result);
        Assert.True(repository.TryGetWorld(world.Id, out _));
    }

    [Fact]
    public async Task DeletePlaceInfo_Owner_ReturnsForbidden()
    {
        var repository = new FakeSpotRepository();
        var placeInfo = new PlaceInfo(Guid.NewGuid(), Guid.NewGuid(), "owner-user", "店", "住所", "営業情報");
        repository.UpsertPlaceInfo(placeInfo);
        var useCase = new DeletePlaceInfoUseCase(repository, Owner());

        var result = await useCase.ExecuteAsync(new DeletePlaceInfo.Request(placeInfo.Id));

        AssertForbidden(result);
        Assert.True(repository.TryGetPlaceInfo(placeInfo.Id, out _));
    }

    [Fact]
    public async Task DeleteWebLink_Owner_ReturnsForbidden()
    {
        var repository = new FakeSpotRepository();
        var webLink = new WebLink(Guid.NewGuid(), Guid.NewGuid(), "owner-user", "公式", new Uri("https://example.com"));
        repository.UpsertWebLink(webLink);
        var useCase = new DeleteWebLinkUseCase(repository, Owner());

        var result = await useCase.ExecuteAsync(new DeleteWebLink.Request(webLink.Id));

        AssertForbidden(result);
        Assert.True(repository.TryGetWebLink(webLink.Id, out _));
    }

    [Fact]
    public async Task DeleteComment_Owner_ReturnsForbidden()
    {
        var repository = new FakeSpotRepository();
        var comment = new Comment(Guid.NewGuid(), Guid.NewGuid(), "owner-user", "コメント");
        repository.UpsertComment(comment);
        var useCase = new DeleteCommentUseCase(repository, Owner());

        var result = await useCase.ExecuteAsync(new DeleteComment.Request(comment.Id));

        AssertForbidden(result);
        Assert.True(repository.TryGetComment(comment.Id, out _));
    }

    private static void AssertForbidden<TResponse>(KawaResult<TResponse> result)
    {
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(KawaErrorKind.Forbidden, result.Error.Kind);
    }

    private static FakeCurrentActorAccessor Owner() =>
        new(new CurrentActor("owner-user", IsAdmin: false, HasVRChatDisplayName: true));
}
