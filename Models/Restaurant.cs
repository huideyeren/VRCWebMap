namespace VRCWebMapBackend.Models;

/// <summary>
/// スポットに紐づく飲食店情報です。
/// </summary>
/// <param name="Id">このレコードの主キーです。</param>
/// <param name="SpotsId">関連するスポットの ID です。</param>
/// <param name="Name">飲食店名です。</param>
/// <param name="Address">所在地です。</param>
/// <param name="Url">公式サイト URL です。</param>
/// <param name="GurunaviUrl">ぐるなび URL です。</param>
/// <param name="TabelogUrl">食べログ URL です。</param>
/// <param name="RettyUrl">Retty URL です。</param>
/// <param name="XUrl">X の URL です。</param>
/// <param name="InstagramUrl">Instagram の URL です。</param>
/// <param name="OpenTime">開店時刻です。</param>
/// <param name="CloseTime">閉店時刻です。</param>
/// <param name="ClosedOn">定休日の説明です。</param>
public sealed record Restaurant(
    Guid Id,
    Guid SpotsId,
    string Name,
    string Address,
    Uri? Url,
    Uri? GurunaviUrl,
    Uri? TabelogUrl,
    Uri? RettyUrl,
    Uri? XUrl,
    Uri? InstagramUrl,
    TimeOnly OpenTime,
    TimeOnly CloseTime,
    string ClosedOn);
