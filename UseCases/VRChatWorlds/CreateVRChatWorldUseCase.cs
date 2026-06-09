using Kawa.Abstractions;
using VrcWebMap.Backend.Contracts.VRChatWorlds;
using VrcWebMap.Backend.Models;
using VrcWebMap.Backend.UseCases.Spots;

namespace VrcWebMap.Backend.UseCases.VRChatWorlds;

[KawaUseCase(
    "vrchat-worlds.create",
    Summary = "Create VRChat world",
    Description = "指定されたスポットに VRChat ワールド情報を追加します。",
    Version = "v1",
    Tags = new[] { "VRChat Worlds" })]
[KawaErrorResponse(KawaErrorKind.Validation, Description = "VRChat ワールド情報の入力値が不正です。")]
[KawaErrorResponse(KawaErrorKind.NotFound, Description = "スポットが見つかりません。")]
/// <summary>
/// スポットに VRChat ワールド情報を追加するユースケースです。
/// </summary>
public sealed class CreateVRChatWorldUseCase(ISpotRepository spots)
    : IUseCase<CreateVRChatWorld.Request, CreateVRChatWorld.Response>
{
    /// <summary>
    /// 指定されたスポットに VRChat ワールド情報を追加します。
    /// </summary>
    public Task<KawaResult<CreateVRChatWorld.Response>> ExecuteAsync(
        CreateVRChatWorld.Request request,
        CancellationToken cancellationToken = default)
    {
        if (!spots.Exists(request.SpotId))
        {
            var error = new KawaError(KawaErrorKind.NotFound, "スポットが見つかりません。");
            return Task.FromResult(KawaResult<CreateVRChatWorld.Response>.Failure(error));
        }

        if (string.IsNullOrWhiteSpace(request.RegisteredByUserId) ||
            string.IsNullOrWhiteSpace(request.VRChatWorldId) ||
            string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Description))
        {
            var error = new KawaError(KawaErrorKind.Validation, "登録者 ID、VRChat ワールド ID、名前、説明は必須です。");
            return Task.FromResult(KawaResult<CreateVRChatWorld.Response>.Failure(error));
        }

        var world = new VRChatWorld(
            Guid.NewGuid(),
            request.SpotId,
            request.RegisteredByUserId.Trim(),
            request.VRChatWorldId.Trim(),
            request.Name.Trim(),
            request.RecommendedCapacity,
            request.Capacity,
            request.Description.Trim(),
            request.PC,
            request.Android,
            request.IOS,
            request.IsPrivate);

        spots.UpsertWorld(world);

        var response = new CreateVRChatWorld.Response(world);
        return Task.FromResult(KawaResult<CreateVRChatWorld.Response>.Success(response));
    }
}
