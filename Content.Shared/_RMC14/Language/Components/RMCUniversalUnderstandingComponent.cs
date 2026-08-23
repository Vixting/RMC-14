namespace Content.Shared._RMC14.Language.Components;

[RegisterComponent]
public sealed partial class RMCUniversalUnderstandingComponent : Component
{
    [DataField]
    public TimeSpan ExpiresAt;
}
