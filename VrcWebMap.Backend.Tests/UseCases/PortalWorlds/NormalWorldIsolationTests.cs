using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.VRChatWorlds;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.Tests.TestDoubles;
using VrcWebMap.Backend.UseCases.Users;
using VrcWebMap.Backend.UseCases.VRChatWorlds;

namespace VrcWebMap.Backend.Tests.UseCases.PortalWorlds;

public sealed class NormalWorldIsolationTests
{
    [Fact]
    public async Task NormalUpdateAndDelete_TreatPortalWorldAsNotFound()
    {
        var spots = new FakeSpotRepository();
        var world = new VRChatWorld(
            Guid.NewGuid(), null, "owner", "wrld_portal", "Portal",
            16, 32, "説明", true, false, false, false, Guid.NewGuid());
        spots.AddWorld(world);
        var actor = new FakeCurrentActorAccessor(
            new CurrentActor("owner", IsAdmin: true, HasVRChatDisplayName: true));
        var users = new FakeDiscordUserRepository();

        var updated = await new UpdateVRChatWorldUseCase(spots, users, actor)
            .ExecuteAsync(new UpdateVRChatWorld.Request(
                world.Id, "wrld_x", "更新", 1, 2, "説明",
                true, false, false, false));
        var deleted = await new DeleteVRChatWorldUseCase(spots, actor)
            .ExecuteAsync(new DeleteVRChatWorld.Request(world.Id));

        Assert.Equal(KawaErrorKind.NotFound, updated.Error!.Kind);
        Assert.Equal(KawaErrorKind.NotFound, deleted.Error!.Kind);
        Assert.True(spots.TryGetWorld(world.Id, out _));
    }

    [Fact]
    public void PublicDto_DoesNotExposeEitherParentId()
    {
        var names = typeof(VRChatWorldData)
            .GetProperties()
            .Select(property => property.Name)
            .ToArray();

        Assert.DoesNotContain("SpotId", names);
        Assert.DoesNotContain("PortalCategoryId", names);
    }
}
