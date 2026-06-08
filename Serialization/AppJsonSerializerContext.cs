using System.Text.Json.Serialization;
using VrcWebMap.Backend.Contracts.Spots;
using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.Serialization;

[JsonSerializable(typeof(Spot))]
[JsonSerializable(typeof(Spot[]))]
[JsonSerializable(typeof(VRChatWorld))]
[JsonSerializable(typeof(VRChatWorld[]))]
[JsonSerializable(typeof(SpotArea))]
[JsonSerializable(typeof(SpotArea[]))]
[JsonSerializable(typeof(AreaDefinition))]
[JsonSerializable(typeof(AreaDefinition[]))]
[JsonSerializable(typeof(Restaurant))]
[JsonSerializable(typeof(Restaurant[]))]
[JsonSerializable(typeof(Comment))]
[JsonSerializable(typeof(Comment[]))]
[JsonSerializable(typeof(ListSpots.Request), TypeInfoPropertyName = "ListSpotsRequest")]
[JsonSerializable(typeof(ListSpots.Response), TypeInfoPropertyName = "ListSpotsResponse")]
[JsonSerializable(typeof(GetSpot.Request), TypeInfoPropertyName = "GetSpotRequest")]
[JsonSerializable(typeof(GetSpot.Response), TypeInfoPropertyName = "GetSpotResponse")]
[JsonSerializable(typeof(CreateSpot.Request), TypeInfoPropertyName = "CreateSpotRequest")]
[JsonSerializable(typeof(CreateSpot.Response), TypeInfoPropertyName = "CreateSpotResponse")]
[JsonSerializable(typeof(UpdateSpot.Request), TypeInfoPropertyName = "UpdateSpotRequest")]
[JsonSerializable(typeof(UpdateSpot.Response), TypeInfoPropertyName = "UpdateSpotResponse")]
[JsonSerializable(typeof(DeleteSpot.Request), TypeInfoPropertyName = "DeleteSpotRequest")]
[JsonSerializable(typeof(DeleteSpot.Response), TypeInfoPropertyName = "DeleteSpotResponse")]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}
