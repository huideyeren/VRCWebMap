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
    /// スポットに紐づく VRChat ワールド一覧を取得します。
    /// </summary>
    /// <returns>VRChat ワールド一覧です。</returns>
    VRChatWorld[] ListWorlds();

    bool TryGetWorld(Guid id, [NotNullWhen(true)] out VRChatWorld? world);

    /// <summary>
    /// スポットに紐づく飲食店一覧を取得します。
    /// </summary>
    /// <returns>飲食店一覧です。</returns>
    Restaurant[] ListRestaurants();

    bool TryGetRestaurant(Guid id, [NotNullWhen(true)] out Restaurant? restaurant);

    /// <summary>
    /// スポットに紐づくコメント一覧を取得します。
    /// </summary>
    /// <returns>コメント一覧です。</returns>
    Comment[] ListComments();

    bool TryGetComment(Guid id, [NotNullWhen(true)] out Comment? comment);

    /// <summary>
    /// VRChat ワールド情報を追加または更新します。
    /// </summary>
    /// <param name="world">保存する VRChat ワールド情報です。</param>
    void UpsertWorld(VRChatWorld world);

    bool DeleteWorld(Guid id);

    /// <summary>
    /// 飲食店情報を追加または更新します。
    /// </summary>
    /// <param name="restaurant">保存する飲食店情報です。</param>
    void UpsertRestaurant(Restaurant restaurant);

    bool DeleteRestaurant(Guid id);

    /// <summary>
    /// コメントを追加または更新します。
    /// </summary>
    /// <param name="comment">保存するコメントです。</param>
    void UpsertComment(Comment comment);

    bool DeleteComment(Guid id);

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

    /// <summary>
    /// 指定されたスポットに紐づく従属データを削除します。
    /// </summary>
    /// <param name="spotId">従属データを削除するスポット ID です。</param>
    void DeleteRelatedData(Guid spotId);
}
