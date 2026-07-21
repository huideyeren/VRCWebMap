using VrcWebMap.Backend.Contracts.Spots;
using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.UseCases.Spots;

/// <summary>
/// KML import 候補と既存 Spot の近接を、地表面距離で判定します。
/// </summary>
public static class KmlSpotDuplicateMatcher
{
    /// <summary>
    /// 重複候補として確認を求める距離です。
    /// </summary>
    public const double NearDuplicateDistanceMeters = 50.0;

    private const double EarthRadiusMeters = 6_371_008.8;

    /// <summary>
    /// 指定候補から 50m 以内にある既存 Spot を近い順に取得します。
    /// </summary>
    public static PreviewKmlImport.NearbySpot[] FindNearDuplicates(
        PreviewKmlImport.KmlImportSpotCandidate candidate,
        IEnumerable<Spot> existingSpots) =>
        existingSpots
            .Select(spot => new PreviewKmlImport.NearbySpot(
                spot.Id,
                spot.Name,
                spot.Latitude,
                spot.Longitude,
                CalculateDistanceMeters(
                    candidate.Latitude,
                    candidate.Longitude,
                    spot.Latitude,
                    spot.Longitude)))
            .Where(spot => spot.DistanceMeters <= NearDuplicateDistanceMeters)
            .OrderBy(spot => spot.DistanceMeters)
            .ThenBy(spot => spot.Id)
            .ToArray();

    /// <summary>
    /// WGS84 座標間の地表面距離を Haversine formula で計算します。
    /// </summary>
    public static double CalculateDistanceMeters(
        double latitudeA,
        double longitudeA,
        double latitudeB,
        double longitudeB)
    {
        var latitudeDelta = DegreesToRadians(latitudeB - latitudeA);
        var longitudeDelta = DegreesToRadians(longitudeB - longitudeA);
        var a = Math.Pow(Math.Sin(latitudeDelta / 2), 2) +
            Math.Cos(DegreesToRadians(latitudeA)) * Math.Cos(DegreesToRadians(latitudeB)) *
            Math.Pow(Math.Sin(longitudeDelta / 2), 2);
        return 2 * EarthRadiusMeters * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;
}
