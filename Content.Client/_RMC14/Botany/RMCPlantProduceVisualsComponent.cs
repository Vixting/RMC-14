namespace Content.Client._RMC14.Botany;

[RegisterComponent]
public sealed partial class RMCPlantProduceVisualsComponent : Component
{
    [DataField]
    public float MinimumScale = 1f;

    [DataField]
    public float MaximumScale = 2f;
}
