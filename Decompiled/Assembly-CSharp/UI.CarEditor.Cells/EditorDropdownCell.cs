using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UI.CarEditor.Cells;

public class EditorDropdownCell : EditorCellBase
{
	[SerializeField]
	private TMP_Dropdown dropdown;

	private Action<int> _didSelect;

	private void Awake()
	{
		dropdown.onValueChanged.AddListener(delegate(int i)
		{
			_didSelect?.Invoke(i);
		});
	}

	public void Configure(string labelText, List<string> values, int selected, Action<int> didSelect)
	{
		label.text = labelText;
		dropdown.ClearOptions();
		dropdown.AddOptions(values);
		dropdown.value = selected;
		_didSelect = didSelect;
	}
}
