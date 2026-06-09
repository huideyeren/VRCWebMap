using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.Tests.Models;

public sealed class VRChatWorldTests
{
    [Fact]
    public void WorldPageUrl_UsesVRChatWorldId()
    {
        var world = new VRChatWorld(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "discord-user-id",
            "wrld_00000000-0000-0000-0000-000000000000",
            "テストワールド",
            16,
            32,
            "説明",
            PC: true,
            Android: true,
            IOS: false);

        Assert.Equal(
            new Uri("https://vrchat.com/home/world/wrld_00000000-0000-0000-0000-000000000000/info"),
            world.WorldPageUrl);
    }

    [Fact]
    public void ReleaseStatus_DefaultsToPublic()
    {
        var world = new VRChatWorld(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "discord-user-id",
            "wrld_00000000-0000-0000-0000-000000000000",
            "テストワールド",
            16,
            32,
            "説明",
            PC: true,
            Android: true,
            IOS: false);

        Assert.False(world.IsPrivate);
        Assert.Equal("public", world.ReleaseStatus);
    }

    [Fact]
    public void ReleaseStatus_ReturnsPrivateWhenIsPrivate()
    {
        var world = new VRChatWorld(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "discord-user-id",
            "wrld_00000000-0000-0000-0000-000000000000",
            "テストワールド",
            16,
            32,
            "説明",
            PC: true,
            Android: false,
            IOS: false,
            IsPrivate: true);

        Assert.Equal("private", world.ReleaseStatus);
    }
}
