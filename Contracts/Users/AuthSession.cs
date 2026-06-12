namespace VrcWebMap.Backend.Contracts.Users;

public static class AuthSession
{
    public sealed record CurrentUserResponse(
        string DiscordUserId,
        string Username,
        string? DisplayName,
        bool IsAdmin);

    public sealed record DevelopmentUserResponse(
        string UserId,
        string Username,
        string DisplayName,
        bool IsAdmin,
        string LoginUrl);

    public sealed record LogoutResponse(bool LoggedOut);
}
