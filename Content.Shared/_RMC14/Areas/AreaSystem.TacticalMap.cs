using Robust.Shared.Maths;

namespace Content.Shared._RMC14.Areas;

public sealed partial class AreaSystem
{
    public static bool TryGetTacticalMapBounds(AreaGridComponent grid, out Vector2i minimum, out Vector2i maximum)
    {
        minimum = default;
        maximum = default;
        var found = false;
        foreach (var position in grid.Colors.Keys)
        {
            if (!found)
            {
                minimum = position;
                maximum = position;
                found = true;
            }
            else
            {
                minimum = Vector2i.ComponentMin(minimum, position);
                maximum = Vector2i.ComponentMax(maximum, position);
            }
        }

        if (!found || !grid.HasTacMapBounds)
            return found;

        var clippedMin = Vector2i.ComponentMax(minimum, grid.TacMapBoundsMin);
        var clippedMax = Vector2i.ComponentMin(maximum, grid.TacMapBoundsMax);
        if (clippedMax.X >= clippedMin.X && clippedMax.Y >= clippedMin.Y)
        {
            minimum = clippedMin;
            maximum = clippedMax;
        }

        return true;
    }
}
