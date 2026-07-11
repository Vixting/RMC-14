using System.Linq;
using System.Text;
using Content.Shared._RMC14.Chemistry.Generation;

namespace Content.Server._RMC14.Chemistry.Generation;

internal static class RMCChemPaperFormat
{
    private const string Divider = "‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾";
    private const int BodyFontSize = 10;
    public const int PropertiesFontSize = 8;

    // only Hyperthermic & Explosive are flagged `volatile = TRUE`. A reagent with
    // either shows one uniform warning
    private static readonly string[] VolatileProperties = ["Hyperthermic", "Explosive"];

    public static bool IsVolatile(IEnumerable<(ChemGeneratorPropertyPrototype Property, int Level)> properties)
    {
        return properties.Any(p => VolatileProperties.Contains(p.Property.ID));
    }

    public static void AppendHeader(StringBuilder sb, string title, string? subtitle = null)
    {
        sb.AppendLine($"[bolditalic]{title}[/bolditalic]");
        if (subtitle != null)
            sb.AppendLine($"[italic]{subtitle}[/italic]");
        sb.AppendLine(Divider);
    }

    public static void AppendFooter(StringBuilder sb, string text)
    {
        sb.AppendLine(Divider);
        sb.AppendLine($"[italic]{text}[/italic]");
    }

    public static string Wrap(StringBuilder sb)
    {
        return $"[font size={BodyFontSize}]{sb}[/font]";
    }
}
