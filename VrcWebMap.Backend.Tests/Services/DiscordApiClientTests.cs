using Microsoft.Extensions.Options;
using VrcWebMap.Backend.Options;
using VrcWebMap.Backend.Services;

namespace VrcWebMap.Backend.Tests.Services;

public sealed class DiscordApiClientTests
{
    [Fact]
    public void CreateAuthorizationUri_RequestsIdentityAndGuildMembershipWithoutBotScope()
    {
        var client = new DiscordApiClient(
            new HttpClient(),
            Microsoft.Extensions.Options.Options.Create(new DiscordOptions
            {
                ClientId = "client",
                RedirectUri = "https://example.test/auth/discord/callback",
                RequiredGuildId = "guild"
            }));

        var uri = client.CreateAuthorizationUri("state");
        var query = Uri.UnescapeDataString(uri.Query);

        Assert.Contains("identify", query, StringComparison.Ordinal);
        Assert.Contains("guilds.members.read", query, StringComparison.Ordinal);
        Assert.DoesNotContain(" bot", query, StringComparison.Ordinal);
    }
}
