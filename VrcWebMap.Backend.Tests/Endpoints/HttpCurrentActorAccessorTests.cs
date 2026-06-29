using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using VrcWebMap.Backend.Endpoints.Web;
using VrcWebMap.Backend.Tests.TestDoubles;
using VrcWebMap.Backend.Tests.UseCases.Users;

namespace VrcWebMap.Backend.Tests.Endpoints;

public sealed class HttpCurrentActorAccessorTests
{
    [Fact]
    public void GetCurrent_UsesLatestDatabaseStateInsteadOfCookieRole()
    {
        var databaseUser = UpdateVRChatDisplayNameUseCaseTests
            .CreateUser("discord-id", "discord-user", isAdmin: false) with
        {
            VRChatDisplayName = "Alice",
            NormalizedVRChatDisplayName = "ALICE"
        };
        var repository = new FakeDiscordUserRepository(databaseUser);
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "discord-id"),
            new Claim(ClaimTypes.Role, "Admin")
        ], "test");
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };
        var accessor = new HttpCurrentActorAccessor(
            new HttpContextAccessor { HttpContext = context },
            repository);

        var actor = accessor.GetCurrent();

        Assert.NotNull(actor);
        Assert.Equal("discord-id", actor.DiscordUserId);
        Assert.False(actor.IsAdmin);
        Assert.True(actor.HasVRChatDisplayName);
    }

    [Fact]
    public void GetCurrent_Unauthenticated_ReturnsNull()
    {
        var accessor = new HttpCurrentActorAccessor(
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
            new FakeDiscordUserRepository());

        Assert.Null(accessor.GetCurrent());
    }
}
