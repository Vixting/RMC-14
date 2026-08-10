using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Biomass;

[Prototype("rmcBiomassUpgrade")]
public sealed partial class RMCBiomassUpgradePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField(required: true)]
    public RMCBiomassUpgradeCategory Category;

    [DataField(required: true)]
    public RMCBiomassUpgradeKind Kind;

    [DataField(required: true)]
    public int Cost;

    [DataField]
    public int RequiredClearance = 1;

    [DataField(required: true)]
    public string Name = string.Empty;

    [DataField(required: true)]
    public string Description = string.Empty;

    [DataField]
    public int PriceChange;

    [DataField]
    public int MinimumPrice;

    [DataField]
    public int MaximumPrice = int.MaxValue;

    [DataField]
    public EntProtoId? PrintedItem;
}

public enum RMCBiomassUpgradeCategory
{
    Machinery,
    Items,
    Armor,
}

public enum RMCBiomassUpgradeKind
{
    AutodocInternalBleeding,
    AutodocBrokenBone,
    AutodocOrganDamage,
    AutodocLarvaExtraction,
    SleeperUpgrade,
    AutoHarvestUpgrade,
    ReagentGrinderUpgrade,
    IncineratorTankCapacity,
    SmokeTankCapacity,
    ResearchContractReroll,
    LaserScalpelTier2,
    LaserScalpelTier3,
    IncisionManagement,
    CoagulatorPlate,
    EmergencyInjectorPlate,
    CeramicPlate,
    AntiDecayPlate,
    TranslatorPlate,
    Nanosplints,
    SplintUnlock,
}
