using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Spots;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.UseCases.Resources;
using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.UseCases.Spots;

[KawaUseCase(
    "spots.list",
    Summary = "List spots",
    Description = "管理対象のスポット一覧を返します。",
    Version = "v1",
    Tags = new[] { "Spot Management" })]
/// <summary>
/// スポット一覧を取得するユースケースです。
/// </summary>
public sealed class ListSpotsUseCase(
    ISpotRepository spots,
    IDiscordUserRepository users,
    ICurrentActorAccessor currentActor)
    : IUseCase<ListSpots.Request, ListSpots.Response>
{
    /// <summary>
    /// 管理対象のスポット一覧を取得します。
    /// </summary>
    /// <param name="request">一覧取得リクエストです。</param>
    /// <param name="cancellationToken">キャンセル通知です。</param>
    /// <returns>スポット一覧を返します。</returns>
    public Task<KawaResult<ListSpots.Response>> ExecuteAsync(
        ListSpots.Request request,
        CancellationToken cancellationToken = default)
    {
        var allSpots = spots.List();
        var terms = SearchTerms(request.Query);
        var listedSpots = terms.Length == 0
            ? allSpots
            : allSpots.Where(spot => MatchesAllTerms(spot, terms)).ToArray();
        var worldSpotIds = spots.ListWorlds()
            .Where(world => world.SpotId.HasValue)
            .Select(world => world.SpotId!.Value)
            .ToHashSet();
        var placeInfoSpotIds = spots.ListPlaceInfos()
            .Select(placeInfo => placeInfo.SpotId)
            .ToHashSet();
        var mapper = new PublicResourceMapper(users.List(), currentActor.GetCurrent());
        var items = listedSpots
            .Select(spot => mapper.ToSpot(
                spot,
                worldSpotIds.Contains(spot.Id),
                placeInfoSpotIds.Contains(spot.Id)))
            .ToArray();

        var response = new ListSpots.Response(items);
        return Task.FromResult(KawaResult<ListSpots.Response>.Success(response));
    }

    private static string[] SearchTerms(string? query) =>
        string.IsNullOrWhiteSpace(query)
            ? []
            : query.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static bool MatchesAllTerms(Spot spot, string[] terms)
    {
        // Repository-specific full-text indexes can be introduced later. Keeping
        // this rule in the UseCase makes the search behavior identical for the
        // in-memory prototype and PostgreSQL while the data set is still small.
        return terms.All(term =>
            Contains(spot.Name, term) ||
            Contains(spot.Description, term));
    }

    private static bool Contains(string value, string term) =>
        value.Contains(term, StringComparison.OrdinalIgnoreCase);
}
