namespace VrcWebMap.Backend.Contracts.Portal;

/// <summary>
/// 既存のWPPLS WorldData.jsonへシステム管理データを追加する契約です。
/// </summary>
public static class MergeWorldData
{
    public sealed record Request(string ExistingJson);

    public sealed record Response(string MergedJson);
}
