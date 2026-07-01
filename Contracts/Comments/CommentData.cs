namespace VrcWebMap.Backend.Contracts.Comments;

/// <summary>
/// 外部へ公開するコメント情報です。
/// </summary>
public sealed record CommentData(
    Guid Id,
    string Comments,
    string RegisteredByDisplayName,
    bool CanEdit);
