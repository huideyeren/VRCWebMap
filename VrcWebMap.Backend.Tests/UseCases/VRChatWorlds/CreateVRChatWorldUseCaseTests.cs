using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.VRChatWorlds;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.Tests.TestDoubles;
using VrcWebMap.Backend.UseCases.VRChatWorlds;

namespace VrcWebMap.Backend.Tests.UseCases.VRChatWorlds;

public sealed class CreateVRChatWorldUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ExistingSpot_CreatesWorld()
    {
        var spot = new Spot(Guid.NewGuid(), "owner-user", "スポット", 35, 139, AreaCodes.Japan.Tokyo, "説明");
        var repository = new FakeSpotRepository(spot);
        var useCase = new CreateVRChatWorldUseCase(repository);
        var request = new CreateVRChatWorld.Request(
            spot.Id,
            " discord-user-id ",
            " wrld_00000000-0000-0000-0000-000000000000 ",
            " テストワールド ",
            16,
            32,
            " 説明 ",
            PC: true,
            Android: false,
            IOS: false);

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(spot.Id, result.Value.World.SpotId);
        Assert.Equal("discord-user-id", result.Value.World.RegisteredByUserId);
        Assert.Equal("wrld_00000000-0000-0000-0000-000000000000", result.Value.World.VRChatWorldId);
        Assert.Equal("public", result.Value.World.ReleaseStatus);
        Assert.Single(repository.SavedWorlds);
    }

    [Fact]
    public async Task ExecuteAsync_EmptySpotId_ReturnsValidation()
    {
        var repository = new FakeSpotRepository();
        var useCase = new CreateVRChatWorldUseCase(repository);
        var request = new CreateVRChatWorld.Request(
            Guid.Empty,
            "discord-user-id",
            "wrld_00000000-0000-0000-0000-000000000000",
            "テストワールド",
            16,
            32,
            "説明",
            PC: true,
            Android: false,
            IOS: false);

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(KawaErrorKind.Validation, result.Error.Kind);
        Assert.Empty(repository.SavedWorlds);
    }

    [Fact]
    public async Task ExecuteAsync_MissingSpot_ReturnsNotFound()
    {
        var repository = new FakeSpotRepository();
        var useCase = new CreateVRChatWorldUseCase(repository);
        var request = new CreateVRChatWorld.Request(
            Guid.NewGuid(),
            "discord-user-id",
            "wrld_00000000-0000-0000-0000-000000000000",
            "テストワールド",
            16,
            32,
            "説明",
            PC: true,
            Android: false,
            IOS: false);

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(KawaErrorKind.NotFound, result.Error.Kind);
        Assert.Empty(repository.SavedWorlds);
    }
}
