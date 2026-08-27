using System.Linq;
using Content.Shared._RMC14.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Chemistry.Generation;

public sealed class RMCReagentPrototypeSyncSystem : EntitySystem
{
    [Dependency] private readonly RMCReagentSystem _reagent = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private readonly Dictionary<string, RMCGeneratedReagentData> _generated = new();

    public IReadOnlyDictionary<string, RMCGeneratedReagentData> Generated => _generated;

    public override void Initialize()
    {
        SubscribeNetworkEvent<RMCGeneratedReagentSyncEvent>(OnSync);
    }

    public void Broadcast(RMCGeneratedReagentData data)
    {
        Apply(data);
        RaiseNetworkEvent(new RMCGeneratedReagentSyncEvent(data));
    }

    public bool TryGet(string id, out RMCGeneratedReagentData data)
    {
        return _generated.TryGetValue(id, out data!);
    }

    private void OnSync(RMCGeneratedReagentSyncEvent ev)
    {
        Apply(ev.Data);
    }

    private void Apply(RMCGeneratedReagentData data)
    {
        _generated[data.Id] = data;
        _reagent.RegisterGenerated(data);

        Inject(data);
    }

    private void Inject(RMCGeneratedReagentData data)
    {
        var yaml = BuildYaml(data);
        try
        {
            var changed = new Dictionary<Type, HashSet<string>>();
            _prototypes.LoadString(yaml, overwrite: true, changed);
            _prototypes.ReloadPrototypes(changed);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to inject generated reagent prototypes for {data.Id}: {e}");
        }
    }

    private static string BuildYaml(RMCGeneratedReagentData data)
    {
        var desc = string.IsNullOrEmpty(data.PhysicalDescription) ? data.Name : data.PhysicalDescription;

        var lines = new List<string>
        {
            "- type: reagent",
            "  id: " + data.Id,
            "  name: " + YamlString(data.Name),
            "  group: Generated",
            "  desc: " + YamlString(desc),
            "  physicalDesc: " + YamlString(desc),
        };

        if (!string.IsNullOrEmpty(data.Color))
            lines.Add("  color: " + YamlString(data.Color));

        if (data.Ingredients.Count > 0)
        {
            var productAmount = data.Properties.Any(p => p.PropertyId == "Optimized")
                ? 3
                : data.Ingredients.Where(i => !i.Catalyst).Sum(i => Math.Max(i.Amount, 1));
            if (productAmount <= 0)
                productAmount = 1;

            lines.Add(string.Empty);
            lines.Add("- type: reaction");
            lines.Add("  id: " + data.Id + "Recipe");
            lines.Add("  reactants:");
            foreach (var ingredient in data.Ingredients)
            {
                lines.Add("    " + ingredient.Id + ":");
                lines.Add("      amount: " + Math.Max(ingredient.Amount, 1));
                if (ingredient.Catalyst)
                    lines.Add("      catalyst: true");
            }

            lines.Add("  products:");
            lines.Add("    " + data.Id + ": " + productAmount);
        }

        return string.Join("\n", lines);
    }

    private static string YamlString(string value)
    {
        return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }
}

[Serializable, NetSerializable]
public sealed class RMCGeneratedReagentSyncEvent : EntityEventArgs
{
    public RMCGeneratedReagentData Data;

    public RMCGeneratedReagentSyncEvent(RMCGeneratedReagentData data)
    {
        Data = data;
    }
}
