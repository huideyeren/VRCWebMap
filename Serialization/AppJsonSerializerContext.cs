using System.Text.Json.Serialization;
using VrcWebMap.Backend.Contracts.Comments;
using VrcWebMap.Backend.Contracts.Portal;
using VrcWebMap.Backend.Contracts.Restaurants;
using VrcWebMap.Backend.Contracts.Spots;
using VrcWebMap.Backend.Contracts.Users;
using VrcWebMap.Backend.Contracts.VRChatWorlds;
using VrcWebMap.Backend.Models;

namespace VrcWebMap.Backend.Serialization;

[JsonSerializable(typeof(Spot))]
[JsonSerializable(typeof(Spot[]))]
[JsonSerializable(typeof(VRChatWorld))]
[JsonSerializable(typeof(VRChatWorld[]))]
[JsonSerializable(typeof(AreaDefinition))]
[JsonSerializable(typeof(AreaDefinition[]))]
[JsonSerializable(typeof(Restaurant))]
[JsonSerializable(typeof(Restaurant[]))]
[JsonSerializable(typeof(Comment))]
[JsonSerializable(typeof(Comment[]))]
[JsonSerializable(typeof(DiscordUser))]
[JsonSerializable(typeof(DiscordUser[]))]
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
[JsonSerializable(typeof(GetWorldData.Request), TypeInfoPropertyName = "GetWorldDataRequest")]
[JsonSerializable(typeof(GetWorldData.Response), TypeInfoPropertyName = "GetWorldDataResponse")]
[JsonSerializable(typeof(GetWorldData.Category), TypeInfoPropertyName = "GetWorldDataCategory")]
[JsonSerializable(typeof(GetWorldData.Category[]), TypeInfoPropertyName = "GetWorldDataCategoryArray")]
[JsonSerializable(typeof(GetWorldData.World), TypeInfoPropertyName = "GetWorldDataWorld")]
[JsonSerializable(typeof(GetWorldData.World[]), TypeInfoPropertyName = "GetWorldDataWorldArray")]
[JsonSerializable(typeof(GetWorldData.Platform), TypeInfoPropertyName = "GetWorldDataPlatform")]
[JsonSerializable(typeof(GetWorldData.Role), TypeInfoPropertyName = "GetWorldDataRole")]
[JsonSerializable(typeof(GetWorldData.Role[]), TypeInfoPropertyName = "GetWorldDataRoleArray")]
[JsonSerializable(typeof(CreateVRChatWorld.Request), TypeInfoPropertyName = "CreateVRChatWorldRequest")]
[JsonSerializable(typeof(CreateVRChatWorld.Response), TypeInfoPropertyName = "CreateVRChatWorldResponse")]
[JsonSerializable(typeof(UpdateVRChatWorld.Request), TypeInfoPropertyName = "UpdateVRChatWorldRequest")]
[JsonSerializable(typeof(UpdateVRChatWorld.Response), TypeInfoPropertyName = "UpdateVRChatWorldResponse")]
[JsonSerializable(typeof(DeleteVRChatWorld.Request), TypeInfoPropertyName = "DeleteVRChatWorldRequest")]
[JsonSerializable(typeof(DeleteVRChatWorld.Response), TypeInfoPropertyName = "DeleteVRChatWorldResponse")]
[JsonSerializable(typeof(CreateRestaurant.Request), TypeInfoPropertyName = "CreateRestaurantRequest")]
[JsonSerializable(typeof(CreateRestaurant.Response), TypeInfoPropertyName = "CreateRestaurantResponse")]
[JsonSerializable(typeof(UpdateRestaurant.Request), TypeInfoPropertyName = "UpdateRestaurantRequest")]
[JsonSerializable(typeof(UpdateRestaurant.Response), TypeInfoPropertyName = "UpdateRestaurantResponse")]
[JsonSerializable(typeof(DeleteRestaurant.Request), TypeInfoPropertyName = "DeleteRestaurantRequest")]
[JsonSerializable(typeof(DeleteRestaurant.Response), TypeInfoPropertyName = "DeleteRestaurantResponse")]
[JsonSerializable(typeof(CreateComment.Request), TypeInfoPropertyName = "CreateCommentRequest")]
[JsonSerializable(typeof(CreateComment.Response), TypeInfoPropertyName = "CreateCommentResponse")]
[JsonSerializable(typeof(UpdateComment.Request), TypeInfoPropertyName = "UpdateCommentRequest")]
[JsonSerializable(typeof(UpdateComment.Response), TypeInfoPropertyName = "UpdateCommentResponse")]
[JsonSerializable(typeof(DeleteComment.Request), TypeInfoPropertyName = "DeleteCommentRequest")]
[JsonSerializable(typeof(DeleteComment.Response), TypeInfoPropertyName = "DeleteCommentResponse")]
[JsonSerializable(typeof(RegisterDiscordUser.Request), TypeInfoPropertyName = "RegisterDiscordUserRequest")]
[JsonSerializable(typeof(RegisterDiscordUser.Response), TypeInfoPropertyName = "RegisterDiscordUserResponse")]
[JsonSerializable(typeof(AuthSession.CurrentUserResponse), TypeInfoPropertyName = "AuthSessionCurrentUserResponse")]
[JsonSerializable(typeof(AuthSession.LogoutResponse), TypeInfoPropertyName = "AuthSessionLogoutResponse")]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}
