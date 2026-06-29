namespace VrcWebMap.Backend.UseCases.Users;

/// <summary>
/// transportが確認した現在ユーザーをUseCaseへ提供する境界です。
/// </summary>
public interface ICurrentActorAccessor
{
    CurrentActor? GetCurrent();
}
