using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Botany;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class RMCPlantAnalyzerComponent : Component
{
    [DataField]
    public SoundSpecifier ScanSound = new SoundPathSpecifier("/Audio/Machines/scan_finish.ogg");

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    public TimeSpan NextUpdate;

    public EntityUid? User;

    [AutoNetworkedField]
    public EntityUid? Target;

    [AutoNetworkedField]
    public EntityUid? DisplayEntity;

    [AutoNetworkedField]
    public string PlantName = "";

    [AutoNetworkedField]
    public bool IsTray;

    [AutoNetworkedField]
    public bool TrayEmpty;

    [AutoNetworkedField]
    public bool IsProduce;

    [AutoNetworkedField]
    public bool HasPlantData;

    [AutoNetworkedField]
    public float Health;

    [AutoNetworkedField]
    public float Endurance = 100f;

    [AutoNetworkedField]
    public int Age;

    [AutoNetworkedField]
    public float Maturation;

    [AutoNetworkedField]
    public bool ReadyToHarvest;

    [AutoNetworkedField]
    public int Yield;

    [AutoNetworkedField]
    public float Potency;

    [AutoNetworkedField]
    public float Lifespan;

    [AutoNetworkedField]
    public float Production;

    [AutoNetworkedField]
    public HarvestType HarvestRepeat;

    [AutoNetworkedField]
    public bool Seedless;

    [AutoNetworkedField]
    public bool Viable = true;

    [AutoNetworkedField]
    public float WaterLevel;

    [AutoNetworkedField]
    public float NutritionLevel;

    [AutoNetworkedField]
    public float PestLevel;

    [AutoNetworkedField]
    public float WeedLevel;

    [AutoNetworkedField]
    public float Toxins;

    [AutoNetworkedField]
    public float MinHeat;

    [AutoNetworkedField]
    public float MaxHeat;

    [AutoNetworkedField]
    public float MinPressure;

    [AutoNetworkedField]
    public float MaxPressure;

    [AutoNetworkedField]
    public List<string> Traits = new();

    [AutoNetworkedField]
    public List<RMCPlantAnalyzerChemical> Chemicals = new();
}
