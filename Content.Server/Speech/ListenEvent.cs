using Content.Shared._RMC14.Language.Prototypes;
using Content.Shared._RMC14.Language.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.Speech;

public sealed class ListenEvent : EntityEventArgs
{
    public readonly string Message;
    public readonly EntityUid Source;
    // RMC14
    public readonly ProtoId<LanguagePrototype> Language;
    // RMC14

    // RMC14
    public ListenEvent(string message, EntityUid source, ProtoId<LanguagePrototype>? language = null)
    {
        Message = message;
        Source = source;
        Language = language ?? SharedLanguageSystem.CommonLanguage;
    }
    // RMC14
}

public sealed class ListenAttemptEvent : CancellableEntityEventArgs
{
    public readonly EntityUid Source;

    public ListenAttemptEvent(EntityUid source)
    {
        Source = source;
    }
}
