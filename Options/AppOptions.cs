namespace VrcWebMap.Backend.Options;

/// <summary>外部へ出力する直リンクの正規URL設定です。</summary>
public sealed class AppOptions
{
    public string PublicBaseUrl { get; init; } = string.Empty;
}
