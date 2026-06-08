namespace VrcWebMap.Backend.Models;

/// <summary>
/// エリアコードから表示名と分類を引くための定義一覧です。
/// </summary>
public static class AreaDefinitions
{
    /// <summary>
    /// 現在サポートする全エリア定義です。
    /// </summary>
    public static readonly AreaDefinition[] All =
    [
        new(AreaCodes.Japan.Hokkaido, "北海道", AreaCategory.Hokkaido),
        new(AreaCodes.Japan.Aomori, "青森県", AreaCategory.Tohoku),
        new(AreaCodes.Japan.Iwate, "岩手県", AreaCategory.Tohoku),
        new(AreaCodes.Japan.Miyagi, "宮城県", AreaCategory.Tohoku),
        new(AreaCodes.Japan.Akita, "秋田県", AreaCategory.Tohoku),
        new(AreaCodes.Japan.Yamagata, "山形県", AreaCategory.Tohoku),
        new(AreaCodes.Japan.Fukushima, "福島県", AreaCategory.Tohoku),
        new(AreaCodes.Japan.Ibaraki, "茨城県", AreaCategory.Kanto),
        new(AreaCodes.Japan.Tochigi, "栃木県", AreaCategory.Kanto),
        new(AreaCodes.Japan.Gunma, "群馬県", AreaCategory.Kanto),
        new(AreaCodes.Japan.Saitama, "埼玉県", AreaCategory.Kanto),
        new(AreaCodes.Japan.Chiba, "千葉県", AreaCategory.Kanto),
        new(AreaCodes.Japan.Tokyo, "東京都", AreaCategory.Kanto),
        new(AreaCodes.Japan.Kanagawa, "神奈川県", AreaCategory.Kanto),
        new(AreaCodes.Japan.Niigata, "新潟県", AreaCategory.Chubu),
        new(AreaCodes.Japan.Toyama, "富山県", AreaCategory.Chubu),
        new(AreaCodes.Japan.Ishikawa, "石川県", AreaCategory.Chubu),
        new(AreaCodes.Japan.Fukui, "福井県", AreaCategory.Chubu),
        new(AreaCodes.Japan.Yamanashi, "山梨県", AreaCategory.Chubu),
        new(AreaCodes.Japan.Nagano, "長野県", AreaCategory.Chubu),
        new(AreaCodes.Japan.Gifu, "岐阜県", AreaCategory.Chubu),
        new(AreaCodes.Japan.Shizuoka, "静岡県", AreaCategory.Chubu),
        new(AreaCodes.Japan.Aichi, "愛知県", AreaCategory.Chubu),
        new(AreaCodes.Japan.Mie, "三重県", AreaCategory.Kinki),
        new(AreaCodes.Japan.Shiga, "滋賀県", AreaCategory.Kinki),
        new(AreaCodes.Japan.Kyoto, "京都府", AreaCategory.Kinki),
        new(AreaCodes.Japan.Osaka, "大阪府", AreaCategory.Kinki),
        new(AreaCodes.Japan.Hyogo, "兵庫県", AreaCategory.Kinki),
        new(AreaCodes.Japan.Nara, "奈良県", AreaCategory.Kinki),
        new(AreaCodes.Japan.Wakayama, "和歌山県", AreaCategory.Kinki),
        new(AreaCodes.Japan.Tottori, "鳥取県", AreaCategory.Chugoku),
        new(AreaCodes.Japan.Shimane, "島根県", AreaCategory.Chugoku),
        new(AreaCodes.Japan.Okayama, "岡山県", AreaCategory.Chugoku),
        new(AreaCodes.Japan.Hiroshima, "広島県", AreaCategory.Chugoku),
        new(AreaCodes.Japan.Yamaguchi, "山口県", AreaCategory.Chugoku),
        new(AreaCodes.Japan.Tokushima, "徳島県", AreaCategory.Shikoku),
        new(AreaCodes.Japan.Kagawa, "香川県", AreaCategory.Shikoku),
        new(AreaCodes.Japan.Ehime, "愛媛県", AreaCategory.Shikoku),
        new(AreaCodes.Japan.Kochi, "高知県", AreaCategory.Shikoku),
        new(AreaCodes.Japan.Fukuoka, "福岡県", AreaCategory.KyushuOkinawa),
        new(AreaCodes.Japan.Saga, "佐賀県", AreaCategory.KyushuOkinawa),
        new(AreaCodes.Japan.Nagasaki, "長崎県", AreaCategory.KyushuOkinawa),
        new(AreaCodes.Japan.Kumamoto, "熊本県", AreaCategory.KyushuOkinawa),
        new(AreaCodes.Japan.Oita, "大分県", AreaCategory.KyushuOkinawa),
        new(AreaCodes.Japan.Miyazaki, "宮崎県", AreaCategory.KyushuOkinawa),
        new(AreaCodes.Japan.Kagoshima, "鹿児島県", AreaCategory.KyushuOkinawa),
        new(AreaCodes.Japan.Okinawa, "沖縄県", AreaCategory.KyushuOkinawa),
        new(AreaCodes.Overseas.Asia, "アジア", AreaCategory.Asia),
        new(AreaCodes.Overseas.Europe, "ヨーロッパ", AreaCategory.Europe),
        new(AreaCodes.Overseas.Africa, "アフリカ", AreaCategory.Africa),
        new(AreaCodes.Overseas.Oceania, "オセアニア", AreaCategory.Oceania),
        new(AreaCodes.Overseas.NorthAmerica, "北アメリカ", AreaCategory.NorthAmerica),
        new(AreaCodes.Overseas.SouthAmerica, "南アメリカ", AreaCategory.SouthAmerica),
        new(AreaCodes.Overseas.Antarctica, "南極", AreaCategory.Antarctica)
    ];
}
