using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Shared._RMC14.Chemistry.Reagent;

public sealed class RMCReagentSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;

    private FrozenDictionary<string, Reagent> _reagents = FrozenDictionary<string, Reagent>.Empty;

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

        _reagents = dict.ToFrozenDictionary();
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
}
