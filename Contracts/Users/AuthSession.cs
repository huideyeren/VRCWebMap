namespace VrcWebMap.Backend.Contracts.Users;

/// <summary>
/// ブラウザセッションと開発用ログイン画面で使う認証レスポンスです。
/// </summary>
public static class AuthSession
{
    /// <summary>
    /// 現在ログインしているユーザーを返すレスポンスです。
    /// </summary>
    /// <param name="DiscordUserId">Discord ユーザー ID です。</param>
    /// <param name="Username">Discord のユーザー名です。</param>
    /// <param name="DisplayName">Discord の表示名です。</param>
    /// <param name="IsAdmin">アプリケーション管理者として扱う場合は <c>true</c> です。</param>
    public sealed record CurrentUserResponse(
        string DiscordUserId,
        string Username,
        string? DisplayName,
        bool IsAdmin);

    /// <summary>
    /// Development 環境で選択できるサンプルユーザーです。
    /// </summary>
    /// <param name="UserId">開発用サンプルユーザー ID です。</param>
    /// <param name="Username">開発用ユーザー名です。</param>
    /// <param name="DisplayName">画面表示用の名前です。</param>
    /// <param name="IsAdmin">管理者サンプルユーザーの場合は <c>true</c> です。</param>
    /// <param name="LoginUrl">このユーザーとしてログインする URL です。</param>
    public sealed record DevelopmentUserResponse(
        string UserId,
        string Username,
        string DisplayName,
        bool IsAdmin,
        string LoginUrl);

    /// <summary>
    /// Development 環境の補助リンクです。
    /// </summary>
    /// <param name="IsDevelopment">Development 環境で実行中の場合は <c>true</c> です。</param>
    /// <param name="SwaggerUrl">Swagger UI の URL です。</param>
    /// <param name="ReDocUrl">ReDoc UI の URL です。</param>
    /// <param name="OpenApiUrl">OpenAPI JSON の URL です。</param>
    public sealed record DevelopmentAppResponse(
        bool IsDevelopment,
        string SwaggerUrl,
        string ReDocUrl,
        string OpenApiUrl);

    /// <summary>
    /// ログアウト結果です。
    /// </summary>
    /// <param name="LoggedOut">ログアウト処理を実行した場合は <c>true</c> です。</param>
    public sealed record LogoutResponse(bool LoggedOut);
}
