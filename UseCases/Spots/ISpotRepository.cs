using System.Diagnostics.CodeAnalysis;
using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.UseCases.Spots;

/// <summary>
/// スポット永続化の境界です。
/// </summary>
public interface ISpotRepository
{
    /// <summary>
    /// スポット一覧を取得します。
    /// </summary>
    /// <returns>スポット一覧です。</returns>
    Spot[] List();

    /// <summary>
    /// 指定された ID のスポットを取得します。
    /// </summary>
    /// <param name="id">取得対象のスポット ID です。</param>
    /// <param name="spot">見つかったスポットです。</param>
    /// <returns>スポットが見つかった場合は <c>true</c> です。</returns>
    bool TryGet(Guid id, [NotNullWhen(true)] out Spot? spot);

    /// <summary>
    /// 指定された ID のスポットが存在するか確認します。
    /// </summary>
    /// <param name="id">確認対象のスポット ID です。</param>
    /// <returns>存在する場合は <c>true</c> です。</returns>
    bool Exists(Guid id);

    /// <summary>
    /// スポットを追加または更新します。
    /// </summary>
    /// <param name="spot">保存するスポットです。</param>
    void Upsert(Spot spot);

    /// <summary>
    /// 指定された ID のスポットを削除します。
    /// </summary>
    /// <param name="id">削除対象のスポット ID です。</param>
    /// <returns>削除できた場合は <c>true</c> です。</returns>
    bool Delete(Guid id);
}
