using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Spots;
using VrcWebMap.Backend.UseCases.Resources;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.UseCases.Spots;

[KawaUseCase(
    "spots.get",
    Summary = "Get spot",
    Description = "指定されたスポットを返します。",
    Version = "v1",
    Tags = new[] { "Spot Management" })]
[KawaErrorResponse(KawaErrorKind.NotFound, Description = "スポットが見つかりません。")]
/// <summary>
/// 指定されたスポットを取得するユースケースです。
/// </summary>
public sealed class GetSpotUseCase(
    ISpotRepository spots,
    IDiscordUserRepository users,
    ICurrentActorAccessor currentActor)
    : IUseCase<GetSpot.Request, GetSpot.Response>
{
    /// <summary>
    /// 指定された ID のスポットを取得します。
    /// </summary>
    /// <param name="request">取得対象のスポット ID を含むリクエストです。</param>
    /// <param name="cancellationToken">キャンセル通知です。</param>
    /// <returns>取得したスポット、または未検出エラーを返します。</returns>
    public Task<KawaResult<GetSpot.Response>> ExecuteAsync(
        GetSpot.Request request,
        CancellationToken cancellationToken = default)
    {
        if (!spots.TryGet(request.Id, out var spot))
        {
            var error = new KawaError(KawaErrorKind.NotFound, "スポットが見つかりません。");
            return Task.FromResult(KawaResult<GetSpot.Response>.Failure(error));
        }

        var worlds = spots.ListWorlds().Where(world => world.SpotId == spot.Id).ToArray();
        var placeInfos = spots.ListPlaceInfos().Where(placeInfo => placeInfo.SpotId == spot.Id).ToArray();
        var webLinks = spots.ListWebLinks().Where(webLink => webLink.SpotId == spot.Id).ToArray();
        var comments = spots.ListComments().Where(comment => comment.SpotId == spot.Id).ToArray();
        var mapper = new PublicResourceMapper(users.List(), currentActor.GetCurrent());
        var response = new GetSpot.Response(
            mapper.ToSpot(spot, worlds.Length > 0, placeInfos.Length > 0),
            worlds.Select(mapper.ToVRChatWorld).ToArray(),
            placeInfos.Select(mapper.ToPlaceInfo).ToArray(),
            webLinks.Select(mapper.ToWebLink).ToArray(),
            comments.Select(mapper.ToComment).ToArray());
        return Task.FromResult(KawaResult<GetSpot.Response>.Success(response));
    }
}
