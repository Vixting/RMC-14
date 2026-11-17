using System.Globalization;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.UserInterface.Tgui;

public sealed class RMCTguiTable : GridContainer
{
    private const float GapAbove = 10;

    private int _dataColumns;

    public RMCTguiTable()
    {
        HorizontalExpand = true;
    }

    public void Reset(int dataColumns)
    {
        DisposeAllChildren();
        _dataColumns = dataColumns;
        Columns = dataColumns + 1;
    }

    public void AddRow(string header, IReadOnlyList<RMCTguiTableCell> cells, bool headerRow = false, bool gapAbove = false, string? headerTooltip = null)
    {
        var headerCell = new RMCTguiTableCell(header, headerRow, tooltip: headerTooltip) { RowHeader = true };
        headerCell.Margin = RowMargin(headerRow, gapAbove, true, false);
        AddChild(headerCell);

        for (var i = 0; i < _dataColumns; i++)
        {
            var cell = i < cells.Count ? cells[i] : new RMCTguiTableCell(string.Empty, headerRow);
            cell.Margin = RowMargin(headerRow, gapAbove, false, i == _dataColumns - 1);
            AddChild(cell);
        }
    }

    public static List<RMCTguiTableCell> HeaderCells(IEnumerable<int> values)
    {
        var cells = new List<RMCTguiTableCell>();
        foreach (var value in values)
        {
            cells.Add(new RMCTguiTableCell(value.ToString(CultureInfo.InvariantCulture), true));
        }

        return cells;
    }

    public static List<RMCTguiTableCell> HeatCells(IReadOnlyList<int?> values, string referenceName, string? missingTooltip = null)
    {
        var cells = new List<RMCTguiTableCell>();
        var full = values.Count > 0 ? values[0] : null;
        foreach (var value in values)
        {
            if (value is not { } amount)
            {
                cells.Add(new RMCTguiTableCell("—", color: RMCTguiStyle.DimText, tooltip: missingTooltip));
                continue;
            }

            var text = amount.ToString(CultureInfo.InvariantCulture);
            if (amount <= 0 || full is not { } fullAmount || fullAmount <= 0)
            {
                cells.Add(new RMCTguiTableCell(text, color: RMCTguiStyle.DimText));
                continue;
            }

            var fraction = Math.Clamp(amount / (float) fullAmount, 0, 1);
            var percent = MathF.Round(fraction * 100).ToString(CultureInfo.InvariantCulture);
            cells.Add(new RMCTguiTableCell(text,
                background: RMCTguiStyle.HeatColor(fraction).WithAlpha(RMCTguiStyle.HeatAlpha),
                tooltip: $"{percent}% of {referenceName}"));
        }

        return cells;
    }

    private static Thickness RowMargin(bool headerRow, bool gapAbove, bool first, bool last)
    {
        return new Thickness(first ? 0 : 1, gapAbove ? GapAbove : 0, last ? 0 : 1, headerRow ? 4 : 1);
    }
}
