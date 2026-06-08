namespace VrcWebMap.Backend.Models;

/// <summary>
/// スポットのエリア分類で使うコード定数です。
/// </summary>
public static class AreaCodes
{
    /// <summary>
    /// 日本の都道府県コードです。
    /// 国土数値情報の都道府県コードに準じます。
    /// </summary>
    public static class Japan
    {
        public const int Hokkaido = 1;
        public const int Aomori = 2;
        public const int Iwate = 3;
        public const int Miyagi = 4;
        public const int Akita = 5;
        public const int Yamagata = 6;
        public const int Fukushima = 7;
        public const int Ibaraki = 8;
        public const int Tochigi = 9;
        public const int Gunma = 10;
        public const int Saitama = 11;
        public const int Chiba = 12;
        public const int Tokyo = 13;
        public const int Kanagawa = 14;
        public const int Niigata = 15;
        public const int Toyama = 16;
        public const int Ishikawa = 17;
        public const int Fukui = 18;
        public const int Yamanashi = 19;
        public const int Nagano = 20;
        public const int Gifu = 21;
        public const int Shizuoka = 22;
        public const int Aichi = 23;
        public const int Mie = 24;
        public const int Shiga = 25;
        public const int Kyoto = 26;
        public const int Osaka = 27;
        public const int Hyogo = 28;
        public const int Nara = 29;
        public const int Wakayama = 30;
        public const int Tottori = 31;
        public const int Shimane = 32;
        public const int Okayama = 33;
        public const int Hiroshima = 34;
        public const int Yamaguchi = 35;
        public const int Tokushima = 36;
        public const int Kagawa = 37;
        public const int Ehime = 38;
        public const int Kochi = 39;
        public const int Fukuoka = 40;
        public const int Saga = 41;
        public const int Nagasaki = 42;
        public const int Kumamoto = 43;
        public const int Oita = 44;
        public const int Miyazaki = 45;
        public const int Kagoshima = 46;
        public const int Okinawa = 47;
    }

    /// <summary>
    /// 海外エリア用の 100 番台コードです。
    /// </summary>
    public static class Overseas
    {
        public const int Asia = 101;
        public const int Europe = 102;
        public const int Africa = 103;
        public const int Oceania = 104;
        public const int NorthAmerica = 105;
        public const int SouthAmerica = 106;
        public const int Antarctica = 107;
    }
}
