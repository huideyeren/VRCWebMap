namespace VrcWebMap.Backend.Models;

/// <summary>
/// スポットに紐づく現実側の場所情報です。
/// </summary>
/// <param name="Id">このレコードの主キーです。</param>
/// <param name="SpotId">関連するスポットの ID です。</param>
/// <param name="RegisteredByUserId">この場所情報を登録したユーザーの ID です。</param>
/// <param name="Name">場所名です。</param>
/// <param name="Address">所在地です。</param>
/// <param name="BusinessInformation">営業時間、昼夜営業、定休日などを記述する Markdown 対応の営業情報です。</param>
public sealed record PlaceInfo(
    Guid Id,
    Guid SpotId,
    string RegisteredByUserId,
    string Name,
    string Address,
    string BusinessInformation);
