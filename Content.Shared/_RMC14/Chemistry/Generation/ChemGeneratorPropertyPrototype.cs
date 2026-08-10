using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Generation;

public enum ChemPropertyCategory : byte
{
    Positive,
    Negative,
    Neutral,
}

public enum ChemPropertyRarity : byte
{
    Common,
    Uncommon,
    Rare,
}

[Flags]
public enum ChemPropertyType : byte
{
    None = 0,
    Unadjustable = 1 << 0,
    Catalyst = 1 << 1,
    Anomalous = 1 << 2,
    Disabled = 1 << 3,
}

[Prototype]
public sealed partial class ChemGeneratorPropertyPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public EntityEffect Effect = default!;

    [DataField]
    public string Description = string.Empty;

    [DataField]
    public string Code = string.Empty;

    [DataField(required: true)]
    public ChemPropertyCategory Category;

    [DataField]
    public ChemPropertyRarity Rarity = ChemPropertyRarity.Common;

    [DataField]
    public int Value = 1;

    [DataField]
    public int MaxLevel = 8;

    [DataField]
    public ChemPropertyType Flags = ChemPropertyType.None;
}
