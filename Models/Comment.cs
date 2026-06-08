namespace VrcWebMap.Backend.Models;

/// <summary>
/// スポットに紐づく自由コメントです。
/// </summary>
/// <param name="Id">このレコードの主キーです。</param>
/// <param name="SpotsId">関連するスポットの ID です。</param>
/// <param name="Comments">Markdown を想定したコメント本文です。</param>
public sealed record Comment(
    Guid Id,
    Guid SpotsId,
    string Comments);
