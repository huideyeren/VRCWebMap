using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.Portal;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.UseCases.Spots;

namespace VrcWebMap.Backend.UseCases.Portal;

[KawaUseCase(
    "portal.world-data",
    Summary = "Get portal world data",
    Description = "VRChat ワールドポータル向けの WorldData.json 形式データを返します。",
    Version = "v1",
    Tags = new[] { "Portal" })]
/// <summary>
/// VRChat ワールドポータル向け JSON を作成するユースケースです。
/// </summary>
public sealed class GetWorldDataUseCase(ISpotRepository spots)
    : IUseCase<GetWorldData.Request, GetWorldData.Response>
{
    private static readonly AreaCategory[] CategoryOrder =
    [
        AreaCategory.Hokkaido,
        AreaCategory.Tohoku,
        AreaCategory.Kanto,
        AreaCategory.Chubu,
        AreaCategory.Kansai,
        AreaCategory.Chugoku,
        AreaCategory.Shikoku,
        AreaCategory.KyushuOkinawa,
        AreaCategory.Asia,
        AreaCategory.Europe,
        AreaCategory.Africa,
        AreaCategory.Oceania,
        AreaCategory.NorthAmerica,
        AreaCategory.SouthAmerica,
        AreaCategory.Antarctica
    ];

    /// <summary>
    /// Spot の地域カテゴリごとに VRChat ワールドをまとめます。
    /// </summary>
    /// <param name="request">出力オプションです。</param>
    /// <param name="cancellationToken">キャンセル通知です。</param>
    /// <returns>WorldData.json 形式のデータを返します。</returns>
    public Task<KawaResult<GetWorldData.Response>> ExecuteAsync(
        GetWorldData.Request request,
        CancellationToken cancellationToken = default)
    {
        var spotById = spots.List().ToDictionary(spot => spot.Id);
        var areaByCode = AreaDefinitions.All.ToDictionary(area => area.AreaCode);
        var worlds = spots.ListWorlds()
            .Where(world => request.ShowPrivateWorld || !world.IsPrivate)
            .Where(world => spotById.ContainsKey(world.SpotId))
            .ToArray();

        var categorys = worlds
            .GroupBy(world => areaByCode[spotById[world.SpotId].AreaCode].Category)
            .OrderBy(group => Array.IndexOf(CategoryOrder, group.Key))
            .Select(group => new GetWorldData.Category(
                CategoryDisplayName(group.Key),
                group
                    .OrderBy(world => world.Name, StringComparer.Ordinal)
                    .Select(ToWorld)
                    .ToArray()))
            .ToArray();

        var response = new GetWorldData.Response(
            ReverseCategorys: false,
            ShowPrivateWorld: request.ShowPrivateWorld,
            Categorys: categorys,
            Roles: []);

        return Task.FromResult(KawaResult<GetWorldData.Response>.Success(response));
    }

    private static GetWorldData.World ToWorld(VRChatWorld world) =>
        new(
            world.VRChatWorldId,
            world.Name,
            world.RecommendedCapacity,
            world.Capacity,
            world.Description,
            new GetWorldData.Platform(world.PC, world.Android, world.IOS),
            world.ReleaseStatus,
            world.WorldPageUrl);

    private static string CategoryDisplayName(AreaCategory category) =>
        category switch
        {
            AreaCategory.Hokkaido => "北海道",
            AreaCategory.Tohoku => "東北",
            AreaCategory.Kanto => "関東",
            AreaCategory.Chubu => "中部",
            AreaCategory.Kansai => "関西",
            AreaCategory.Chugoku => "中国",
            AreaCategory.Shikoku => "四国",
            AreaCategory.KyushuOkinawa => "九州・沖縄",
            AreaCategory.Asia => "アジア",
            AreaCategory.Europe => "ヨーロッパ",
            AreaCategory.Africa => "アフリカ",
            AreaCategory.Oceania => "オセアニア",
            AreaCategory.NorthAmerica => "北アメリカ",
            AreaCategory.SouthAmerica => "南アメリカ",
            AreaCategory.Antarctica => "南極",
            _ => category.ToString()
        };
}
