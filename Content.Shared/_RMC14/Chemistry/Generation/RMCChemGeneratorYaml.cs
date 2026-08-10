using System.Linq;
using System.Text;
using Content.Shared.Chemistry.Reagent;

namespace Content.Shared._RMC14.Chemistry.Generation;

/// welcome to jankville RMC14 TODO: REPLACE THIS AAAAAAAAAAA
public static class RMCChemGeneratorYaml
{
    public static string Build(
        string id,
        string name,
        string color,
        string physicalDesc,
        List<(ChemGeneratorPropertyPrototype Property, int Level)> properties,
        int overdose,
        int criticalOverdose,
        List<(string Id, int Amount, bool Catalyst)> ingredients,
        ChemClass chemClass,
        ReactionIndicator indicator = ReactionIndicator.Calm)
    {
        var sb = new StringBuilder();

        void Line(string s) => sb.AppendLine(s);

        Line("- type: reagent");
        Line($"  id: {id}");
        Line($"  name: \"{name}\"");
        Line($"  desc: \"A procedurally synthesized compound with unpredictable properties.\"");
        Line($"  physicalDesc: reagent-physical-desc-{physicalDesc}");
        Line($"  color: \"{color}\"");
        Line($"  chemClass: {chemClass}");
        Line("  unknown: true");
        Line("  fireEntity: RMCTileFireGenerated");
        Line($"  overdose: {overdose}");
        Line($"  criticalOverdose: {criticalOverdose}");

        if (properties.Any(p => p.Property.ID is "Defibrillating" or "Neurocryogenic"))
            Line("  worksOnTheDead: true");

        Line("  metabolisms:");
        Line("    Poison:");
        Line("      metabolismRate: 0.1");
        Line("      effects:");
        foreach (var (property, level) in properties)
        {
            Line($"      - !type:{property.ID}");
            Line($"        potency: {level}");
        }

        if (ingredients.Count > 0)
        {
            sb.AppendLine();
            Line("- type: reaction");
            Line($"  id: {id}Recipe");
            Line("  reactants:");
            foreach (var (ingredientId, amount, catalyst) in ingredients)
            {
                Line($"    {ingredientId}:");
                Line($"      amount: {amount}");
                if (catalyst)
                    Line("      catalyst: true");
            }

            var yield = properties.Any(p => p.Property.ID == "Optimized") ? 3 : 1;
            Line("  products:");
            Line($"    {id}: {yield}");

            if (indicator != ReactionIndicator.Calm)
            {
                Line("  effects:");
                Line("  - !type:RMCReactionIndicatorEffect");
                Line($"    indicator: {indicator}");
            }
        }

        return sb.ToString();
    }
}
