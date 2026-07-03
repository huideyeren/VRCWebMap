using VrcWebMap.Backend.UseCases.Users;

namespace VrcWebMap.Backend.Tests.TestDoubles;

internal sealed class FakeCurrentActorAccessor(CurrentActor? actor) : ICurrentActorAccessor
{
    public CurrentActor? GetCurrent() => actor;
}
