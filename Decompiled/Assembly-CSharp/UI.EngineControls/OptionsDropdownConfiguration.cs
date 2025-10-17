using System;
using System.Collections.Generic;

namespace UI.EngineControls;

public readonly struct OptionsDropdownConfiguration
{
	public readonly List<DropdownMenu.RowData> Rows;

	public readonly Action<int> OnRowSelected;

	public OptionsDropdownConfiguration(List<DropdownMenu.RowData> rows, Action<int> rowSelected)
	{
		Rows = rows;
		OnRowSelected = rowSelected;
	}
}
