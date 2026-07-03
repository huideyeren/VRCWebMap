namespace VrcWebMap.Backend.Contracts.WebLinks;

/// <summary>
/// スポットに紐づく Web サイト情報を削除するユースケースの契約です。
/// </summary>
public static class DeleteWebLink
{
    /// <summary>
    /// 削除対象の Web サイト情報と操作ユーザーを指定する入力です。
    /// </summary>
    /// <param name="Id">削除する Web サイト情報の ID です。</param>
    public sealed record Request(Guid Id);

    /// <summary>
    /// 削除された Web サイト情報 ID を返すレスポンスです。
    /// </summary>
    /// <param name="Id">削除された Web サイト情報の ID です。</param>
    public sealed record Response(Guid Id);
}
