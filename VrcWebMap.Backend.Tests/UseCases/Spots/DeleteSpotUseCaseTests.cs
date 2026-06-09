using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Spots;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.Tests.TestDoubles;
using VrcWebMap.Backend.UseCases.Spots;

namespace VrcWebMap.Backend.Tests.UseCases.Spots;

public sealed class DeleteSpotUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ExistingSpot_DeletesSpot()
    {
        var spot = new Spot(Guid.NewGuid(), "owner-user", "スポット", 35, 139, AreaCodes.Japan.Tokyo, "説明");
        var repository = new FakeSpotRepository(spot);
        var useCase = new DeleteSpotUseCase(repository);

        var result = await useCase.ExecuteAsync(new DeleteSpot.Request(spot.Id, "owner-user", ActorIsAdmin: false));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(spot.Id, result.Value.Id);
        Assert.Contains(spot.Id, repository.DeletedSpotIds);
        Assert.False(repository.Exists(spot.Id));
    }

    [Fact]
    public async Task ExecuteAsync_MissingSpot_ReturnsNotFound()
    {
        var missingId = Guid.NewGuid();
        var repository = new FakeSpotRepository();
        var useCase = new DeleteSpotUseCase(repository);

        var result = await useCase.ExecuteAsync(new DeleteSpot.Request(missingId, "owner-user", ActorIsAdmin: false));

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(KawaErrorKind.NotFound, result.Error.Kind);
        Assert.Equal("スポットが見つかりません。", result.Error.Message);
        Assert.DoesNotContain(missingId, repository.DeletedSpotIds);
    }

    [Fact]
    public async Task ExecuteAsync_OtherUser_ReturnsForbidden()
    {
        var spot = new Spot(Guid.NewGuid(), "owner-user", "スポット", 35, 139, AreaCodes.Japan.Tokyo, "説明");
        var repository = new FakeSpotRepository(spot);
        var useCase = new DeleteSpotUseCase(repository);

        var result = await useCase.ExecuteAsync(new DeleteSpot.Request(spot.Id, "other-user", ActorIsAdmin: false));

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(KawaErrorKind.Forbidden, result.Error.Kind);
        Assert.True(repository.Exists(spot.Id));
    }
}
