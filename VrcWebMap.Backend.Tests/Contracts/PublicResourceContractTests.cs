using VrcWebMap.Backend.Contracts.Comments;
using VrcWebMap.Backend.Contracts.PlaceInfos;
using VrcWebMap.Backend.Contracts.Spots;
using VrcWebMap.Backend.Contracts.VRChatWorlds;
using VrcWebMap.Backend.Contracts.WebLinks;

namespace VrcWebMap.Backend.Tests.Contracts;

public sealed class PublicResourceContractTests
{
    public static TheoryData<Type, string, Type> PublicResourceResponses => new()
    {
        { typeof(CreateSpot.Response), nameof(CreateSpot.Response.Spot), typeof(SpotData) },
        { typeof(UpdateSpot.Response), nameof(UpdateSpot.Response.Spot), typeof(SpotData) },
        { typeof(ImportKmlSpots.Response), nameof(ImportKmlSpots.Response.Spots), typeof(SpotData[]) },
        { typeof(CreateVRChatWorld.Response), nameof(CreateVRChatWorld.Response.World), typeof(VRChatWorldData) },
        { typeof(UpdateVRChatWorld.Response), nameof(UpdateVRChatWorld.Response.World), typeof(VRChatWorldData) },
        { typeof(CreatePlaceInfo.Response), nameof(CreatePlaceInfo.Response.PlaceInfo), typeof(PlaceInfoData) },
        { typeof(UpdatePlaceInfo.Response), nameof(UpdatePlaceInfo.Response.PlaceInfo), typeof(PlaceInfoData) },
        { typeof(CreateWebLink.Response), nameof(CreateWebLink.Response.WebLink), typeof(WebLinkData) },
        { typeof(UpdateWebLink.Response), nameof(UpdateWebLink.Response.WebLink), typeof(WebLinkData) },
        { typeof(CreateComment.Response), nameof(CreateComment.Response.Comment), typeof(CommentData) },
        { typeof(UpdateComment.Response), nameof(UpdateComment.Response.Comment), typeof(CommentData) }
    };

    [Theory]
    [MemberData(nameof(PublicResourceResponses))]
    public void Response_UsesPublicResourceDto(Type responseType, string propertyName, Type expectedType)
    {
        Assert.Equal(expectedType, responseType.GetProperty(propertyName)!.PropertyType);
    }
}
