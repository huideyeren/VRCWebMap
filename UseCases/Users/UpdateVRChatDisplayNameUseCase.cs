using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Users;

namespace VrcWebMap.Backend.UseCases.Users;

[KawaUseCase(
    "users.vrchat-display-name.update",
    Summary = "Update VRChat display name",
    Description = "現在ユーザーのVRChat表示名を登録または変更します。",
    Version = "v1",
    Tags = new[] { "Users" })]
[KawaErrorResponse(KawaErrorKind.Validation, Description = "VRChat表示名が不正です。")]
[KawaErrorResponse(KawaErrorKind.Conflict, Description = "VRChat表示名が他ユーザーに登録されています。")]
[KawaErrorResponse(KawaErrorKind.Forbidden, Description = "Discordログインが必要です。")]
public sealed class UpdateVRChatDisplayNameUseCase(
    IDiscordUserRepository users,
    ICurrentActorAccessor currentActor)
    : IUseCase<UpdateVRChatDisplayName.Request, UpdateVRChatDisplayName.Response>
{
    public Task<KawaResult<UpdateVRChatDisplayName.Response>> ExecuteAsync(
        UpdateVRChatDisplayName.Request request,
        CancellationToken cancellationToken = default)
    {
        var actor = currentActor.GetCurrent();
        if (actor is null)
        {
            return Failure(KawaErrorKind.Forbidden, "Discordログインが必要です。");
        }

        if (!users.TryGetByDiscordUserId(actor.DiscordUserId, out var user))
        {
            return Failure(KawaErrorKind.NotFound, "ログインユーザーが登録されていません。");
        }

        var validationError = VRChatDisplayNameNormalizer.Validate(request.VRChatDisplayName);
        if (validationError is not null)
        {
            return Task.FromResult(KawaResult<UpdateVRChatDisplayName.Response>.Failure(validationError));
        }

        var displayName = request.VRChatDisplayName.Trim();
        var normalizedDisplayName = VRChatDisplayNameNormalizer.Normalize(displayName);
        if (users.TryGetByNormalizedVRChatDisplayName(normalizedDisplayName, out var owner) &&
            !string.Equals(owner.DiscordUserId, actor.DiscordUserId, StringComparison.Ordinal))
        {
            return Failure(KawaErrorKind.Conflict, "そのVRChat表示名はすでに登録されています。");
        }

        var updated = user with
        {
            VRChatDisplayName = displayName,
            NormalizedVRChatDisplayName = normalizedDisplayName
        };
        users.Upsert(updated);
        return Task.FromResult(KawaResult<UpdateVRChatDisplayName.Response>.Success(new(updated)));
    }

    private static Task<KawaResult<UpdateVRChatDisplayName.Response>> Failure(
        KawaErrorKind kind,
        string message) =>
        Task.FromResult(
            KawaResult<UpdateVRChatDisplayName.Response>.Failure(new KawaError(kind, message)));
}
