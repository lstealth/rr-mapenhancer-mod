using System.Collections.Generic;

namespace UI.Builder;

public struct TableRow
{
	public List<TableCellEntry> Cells;

	public float Height;

	public TableRow(List<TableCellEntry> cells, float height)
	{
		Height = height;
		Cells = cells;
	}
}
