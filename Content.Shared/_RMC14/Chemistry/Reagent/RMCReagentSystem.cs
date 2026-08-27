using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._RMC14.Chemistry.Effects;
using Content.Shared._RMC14.Chemistry.Generation;
using Content.Shared.Body.Prototypes;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using ReactiveReagentEffectEntry = Content.Shared.Chemistry.Reagent.ReactiveReagentEffectEntry;

namespace Content.Shared._RMC14.Chemistry.Reagent;

public sealed class RMCReagentSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;

    private static readonly ProtoId<MetabolismGroupPrototype> GeneratedMetabolismGroup = "Poison";

    private static readonly ProtoId<ReactiveGroupPrototype> GeneratedReactiveGroup = "RMCGenerated";

    private FrozenDictionary<string, Reagent> _reagents = FrozenDictionary<string, Reagent>.Empty;
    private readonly Dictionary<string, RMCGeneratedReagentData> _generated = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        ReloadAllPrototypes();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (!ev.WasModified<ReagentPrototype>())
            return;

        var dict = new Dictionary<string, Reagent>(_reagents);

        if (ev.Removed != null && ev.Removed.TryGetValue(typeof(ReagentPrototype), out var removed))
        {
            foreach (var id in removed)
                dict.Remove(id);
        }

        if (ev.ByType.TryGetValue(typeof(ReagentPrototype), out var changeSet))
        {
            foreach (var (id, prototype) in changeSet.Modified)
            {
                if (prototype is not ReagentPrototype reagentProto)
                    continue;

                object? reagentObj = new Reagent();
                _serialization.CopyTo(reagentProto, ref reagentObj);
                if (reagentObj is Reagent reagent)
                    dict[id] = reagent;
            }
        }

        ApplyGenerated(dict);
        _reagents = dict.ToFrozenDictionary();
    }

    private void ReloadAllPrototypes()
    {
        var dict = new Dictionary<string, Reagent>();
        foreach (var reagentProto in _prototypes.EnumeratePrototypes<ReagentPrototype>())
        {
            object? reagentObj = new Reagent();
            _serialization.CopyTo(reagentProto, ref reagentObj);
            if (reagentObj is not Reagent reagent)
                continue;

            dict[reagentProto.ID] = reagent;
        }

        ApplyGenerated(dict);
        _reagents = dict.ToFrozenDictionary();
    }

    private void ApplyGenerated(Dictionary<string, Reagent> dict)
    {
        foreach (var data in _generated.Values)
        {
            if (BuildGenerated(data) is { } reagent)
                dict[data.Id] = reagent;
        }
    }

    public void RegisterGenerated(RMCGeneratedReagentData data)
    {
        _generated[data.Id] = data;

        if (BuildGenerated(data) is not { } reagent)
            return;

        var dict = new Dictionary<string, Reagent>(_reagents) { [data.Id] = reagent };
        _reagents = dict.ToFrozenDictionary();
    }

    private Reagent? BuildGenerated(RMCGeneratedReagentData data)
    {
        var desc = string.IsNullOrEmpty(data.PhysicalDescription) ? data.Name : data.PhysicalDescription;
        var node = new MappingDataNode();
        node.Add("id", data.Id);
        node.Add("name", data.Name);
        node.Add("desc", desc);
        node.Add("physicalDesc", desc);
        if (!string.IsNullOrEmpty(data.Color))
            node.Add("color", data.Color);
        node.Add("group", "Generated");

        Reagent reagent;
        try
        {
            reagent = _serialization.Read<Reagent>(node, notNullableOverride: true);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to build generated reagent {data.Id}: {e}");
            return null;
        }

        reagent.ChemClass = data.ChemClass;
        reagent.Overdose = data.Overdose > 0 ? FixedPoint2.New(data.Overdose) : null;
        reagent.CriticalOverdose = data.CriticalOverdose > 0 ? FixedPoint2.New(data.CriticalOverdose) : null;
        reagent.Recognizable = true;

        var effects = new List<EntityEffect>();
        var touchEffects = new List<EntityEffect>();

        var hyperLevel = 0f;
        var hypoLevel = 0f;

        foreach (var prop in data.Properties)
        {
            if (!_prototypes.TryIndex<ChemGeneratorPropertyPrototype>(prop.PropertyId, out var propProto))
                continue;

            var effect = _serialization.CreateCopy(propProto.Effect, notNullableOverride: true);
            if (effect is RMCChemicalEffect rmc)
                rmc.Potency = prop.Level;

            effects.Add(effect);
            if (effect is RMCChemicalEffect { ReactsOnTouch: true })
                touchEffects.Add(effect);

            switch (prop.PropertyId)
            {
                case "Hypermetabolic":
                    hyperLevel = prop.Level;
                    break;
                case "Hypometabolic":
                    hypoLevel = prop.Level;
                    break;
            }
        }

        if (data.Properties.Any(p => p.PropertyId is "Defibrillating" or "Neurocryogenic"))
            reagent.WorksOnTheDead = true;

        if (effects.Count > 0)
        {
            var rate = 0.1f;
            if (hyperLevel > 0f)
                rate *= 1f + 0.25f * hyperLevel;
            if (hypoLevel > 0f)
                rate = MathF.Max(rate / (1f + 0.35f * hypoLevel), 0.005f);

            var entry = new ReagentEffectsEntry { Effects = effects.ToArray(), MetabolismRate = FixedPoint2.New(rate) };
            reagent.Metabolisms = new Dictionary<ProtoId<MetabolismGroupPrototype>, ReagentEffectsEntry>
            {
                [GeneratedMetabolismGroup] = entry,
            }.ToFrozenDictionary();
        }

        if (touchEffects.Count > 0)
        {
            reagent.ReactiveEffects = new Dictionary<ProtoId<ReactiveGroupPrototype>, ReactiveReagentEffectEntry>
            {
                [GeneratedReactiveGroup] = new ReactiveReagentEffectEntry
                {
                    Methods = new HashSet<ReactionMethod> { ReactionMethod.Touch },
                    Effects = touchEffects.ToArray(),
                },
            };
        }

        return reagent;
    }

    public Reagent Index(ProtoId<ReagentPrototype> id)
    {
        return _reagents[id];
    }

    public bool TryIndex(ProtoId<ReagentPrototype> id, [NotNullWhen(true)] out Reagent? reagent)
    {
        return _reagents.TryGetValue(id, out reagent);
    }

    public bool TryIndex(ReagentId id, [NotNullWhen(true)] out Reagent? reagent)
    {
        return _reagents.TryGetValue(id.Prototype, out reagent);
    }

    public bool IsGenerated(ProtoId<ReagentPrototype> id)
    {
        return _generated.ContainsKey(id);
    }
}
