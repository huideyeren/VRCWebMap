using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.UseCases.Spots;

namespace VrcWebMap.Backend.Stores;

/// <summary>
/// Development 環境で使うサンプルデータを投入します。
/// </summary>
public static class DevelopmentSampleDataSeeder
{
    private const string RegisteredByUserId = "local-user";

    private static readonly Guid YoshimuraStudySpotId = Guid.Parse("339814d0-6331-4c6e-b86e-64de4b450040");
    private static readonly Guid InokashiraKoenStationSpotId = Guid.Parse("87f2b4d1-7eb8-4dbb-8bdc-ab76cc2e42dd");

    /// <summary>
    /// 開発用サンプルデータを追加または更新します。
    /// </summary>
    /// <param name="spots">投入先のスポットリポジトリです。</param>
    public static void Seed(ISpotRepository spots)
    {
        foreach (var spot in Spots)
        {
            spots.Upsert(spot);
        }

        foreach (var world in Worlds)
        {
            spots.UpsertWorld(world);
        }

        foreach (var placeInfo in PlaceInfos)
        {
            spots.UpsertPlaceInfo(placeInfo);
        }

        foreach (var webLink in WebLinks)
        {
            spots.UpsertWebLink(webLink);
        }
    }

    private static readonly Spot[] Spots =
    [
        new(
            YoshimuraStudySpotId,
            RegisteredByUserId,
            "三鷹市吉村昭書斎",
            35.696234,
            139.584051,
            AreaCodes.Japan.Tokyo,
            "# 三鷹市吉村昭書斎\n\n記録者・吉村昭の書斎"),
        new(
            InokashiraKoenStationSpotId,
            RegisteredByUserId,
            "井の頭公園駅",
            35.697484,
            139.582739,
            AreaCodes.Japan.Tokyo,
            "# 井の頭公園駅\n\nVRChatの聖地")
    ];

    private static readonly VRChatWorld[] Worlds =
    [
        new(
            Guid.Parse("91bbf952-12b5-4f38-bbfe-214fe4f365eb"),
            InokashiraKoenStationSpotId,
            RegisteredByUserId,
            "wrld_6e5a0cf2-9ea4-4ccd-a165-c4b53d2945d1",
            "Inokashira-kōen Station（井の頭公園駅）",
            40,
            80,
            "Recreation of a railway station located in the city of Mitaka‚ Tokyo‚ Japan․ 東京都三鷹市井の頭三丁目にある、京王電鉄井の頭線の井の頭公園駅の再現ワールドです。 Port to sdk3 and android version by 3Dimka․",
            PC: true,
            Android: true,
            IOS: false),
        new(
            Guid.Parse("6f0400d0-1ab8-4113-bbe0-d0a9ed12ed32"),
            YoshimuraStudySpotId,
            RegisteredByUserId,
            "wrld_18d4c589-e896-4acf-9fa7-f696f5f22e79",
            "三鷹市吉村昭書斎",
            16,
            32,
            "東京都三鷹市井の頭にある、作家・吉村昭氏の書斎です。あの井の頭公園駅ワールドの端っこにあります。 Assets˸ VRChat Gaussian Splatting by Mykhailo Moroz‚ QvPen by ureishi",
            PC: true,
            Android: false,
            IOS: false)
    ];

    private static readonly PlaceInfo[] PlaceInfos =
    [
        new(
            Guid.Parse("da419116-a5f1-4e33-94ab-28fb1c04774a"),
            YoshimuraStudySpotId,
            RegisteredByUserId,
            "三鷹市吉村昭書斎",
            "三鷹市井の頭3-3-17",
            "三鷹市吉村昭書斎\n〒181-0001 三鷹市井の頭3-3-17\n京王井の頭線井の頭公園駅から徒歩3分\n電話 0422-26-7500　ファクス 0422-26-7548\n開館時間 10:00-17:30\n入館料 交流棟無料、書斎棟100円、年間パスポート300円\n＊年間パスポートの有効期限は交付日から1年間（同施設の窓口にて販売）\n＊中学生以下、障害者手帳を持参の方とその介助者、校外学習の高校生以下と引率教諭は無料\n＊「東京・ミュージアムぐるっとパス新しいウィンドウで外部サイトを開きます」利用可\n休館日 月曜日、年末年始（12月29日～1月4日）\n＊月曜日が休日の場合は開館し、休日を除く翌日と翌々日が休館"),
        new(
            Guid.Parse("e9ec86f7-c9d2-4647-ad61-5a6f3ae8c9c2"),
            InokashiraKoenStationSpotId,
            RegisteredByUserId,
            "井の頭公園駅",
            "東京都三鷹市井の頭三丁目35-12",
            "井の頭公園駅（いのかしらこうえんえき）は、東京都三鷹市井の頭三丁目にある、京王電鉄井の頭線の駅である。井の頭北管区所属。駅番号はIN16。")
    ];

    private static readonly WebLink[] WebLinks =
    [
        new(
            Guid.Parse("fc132153-5d01-4acd-ae68-3e40217759d2"),
            InokashiraKoenStationSpotId,
            RegisteredByUserId,
            "Wikipedia",
            new Uri("https://ja.wikipedia.org/wiki/井の頭公園駅")),
        new(
            Guid.Parse("16624b5a-c291-44d6-b3ad-4459fd19bf6c"),
            InokashiraKoenStationSpotId,
            RegisteredByUserId,
            "京王電鉄公式",
            new Uri("https://www.keio.co.jp/train/station/in16_inokashira-koen/")),
        new(
            Guid.Parse("533388f0-c9cf-4682-ab20-92a24f94ec6a"),
            YoshimuraStudySpotId,
            RegisteredByUserId,
            "公式サイト",
            new Uri("https://mitaka-sportsandculture.or.jp/yoshimura/info/"))
    ];
}
