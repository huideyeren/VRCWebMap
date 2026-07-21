namespace VrcWebMap.Backend.Contracts.Spots;

public static class ExportKmlSpots
{
    public sealed record Request(Guid[] SpotIds);
    public sealed record Response(string FileName, string Content, Guid[] MissingSpotIds);
}
