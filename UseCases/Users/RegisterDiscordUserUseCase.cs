using Kawa.Abstractions;
using Microsoft.Extensions.Options;
using VrcWebMap.Backend.Contracts.Users;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.Options;

namespace VrcWebMap.Backend.UseCases.Users;

[KawaUseCase(
    "users.discord.register",
    Summary = "Register Discord user",
    Description = "Discord OAuth callback adapter が確認した Discord ユーザーを登録または更新します。",
    Version = "v1",
    Tags = new[] { "Users" })]
[KawaErrorResponse(KawaErrorKind.Validation, Description = "Discord ユーザー情報の入力値が不正です。")]
[KawaErrorResponse(KawaErrorKind.Forbidden, Description = "対象 Discord サーバーに参加していません。")]
/// <summary>
/// Discord ユーザーをアプリケーションユーザーとして登録または更新するユースケースです。
/// </summary>
public sealed class RegisterDiscordUserUseCase(
    IDiscordUserRepository users,
    IOptions<DiscordOptions> options)
    : IUseCase<RegisterDiscordUser.Request, RegisterDiscordUser.Response>
{
    public Task<KawaResult<RegisterDiscordUser.Response>> ExecuteAsync(
        RegisterDiscordUser.Request request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.DiscordUserId) ||
            string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.RequiredGuildId))
        {
            var error = new KawaError(KawaErrorKind.Validation, "Discord ユーザー ID、ユーザー名、参加必須サーバー ID は必須です。");
            return Task.FromResult(KawaResult<RegisterDiscordUser.Response>.Failure(error));
        }

        if (!request.IsRequiredGuildMember)
        {
            var error = new KawaError(KawaErrorKind.Forbidden, "対象 Discord サーバーに参加しているユーザーのみ利用できます。");
            return Task.FromResult(KawaResult<RegisterDiscordUser.Response>.Failure(error));
        }

        var now = DateTimeOffset.UtcNow;
        var discordUserId = request.DiscordUserId.Trim();
        var username = request.Username.Trim();
        var globalName = string.IsNullOrWhiteSpace(request.GlobalName) ? null : request.GlobalName.Trim();
        var avatarHash = string.IsNullOrWhiteSpace(request.AvatarHash) ? null : request.AvatarHash.Trim();
        var requiredGuildId = request.RequiredGuildId.Trim();

        users.TryGetByDiscordUserId(discordUserId, out var existing);
        var registeredAt = existing?.RegisteredAt ?? now;
        var isInitialAdministrator = options.Value.InitialAdminUserIds
            .Any(id => string.Equals(id?.Trim(), discordUserId, StringComparison.Ordinal));

        var user = new DiscordUser(
            discordUserId,
            username,
            globalName,
            avatarHash,
            requiredGuildId,
            IsGuildMember: true,
            IsAdmin: isInitialAdministrator || existing?.IsAdmin == true,
            registeredAt,
            now,
            existing?.VRChatDisplayName,
            existing?.NormalizedVRChatDisplayName);

        users.Upsert(user);

        var response = new RegisterDiscordUser.Response(user);
        return Task.FromResult(KawaResult<RegisterDiscordUser.Response>.Success(response));
    }
}
