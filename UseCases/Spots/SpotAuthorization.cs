namespace VrcWebMap.Backend.UseCases.Spots;

/// <summary>
/// スポット操作の権限判定です。
/// </summary>
internal static class SpotAuthorization
{
    /// <summary>
    /// 管理者または登録者本人であれば変更できます。
    /// </summary>
    /// <param name="registeredByUserId">対象データの登録者 ID です。</param>
    /// <param name="actorUserId">操作ユーザーの ID です。</param>
    /// <param name="actorIsAdmin">操作ユーザーが管理者かどうかです。</param>
    /// <returns>変更可能な場合は <c>true</c> です。</returns>
    public static bool CanMutate(string registeredByUserId, string actorUserId, bool actorIsAdmin) =>
        actorIsAdmin ||
        string.Equals(registeredByUserId, actorUserId?.Trim(), StringComparison.Ordinal);
}
