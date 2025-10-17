using System;
using System.Collections.Generic;
using TMPro;
using UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace UI;

public class DropdownLocationPicker : DropdownPickerBase
{
	[SerializeField]
	private TMP_Text promptLabel;

	[SerializeField]
	private TMP_Text currentValueLabel;

	[SerializeField]
	private DropdownLocationPickerRow templateRow;

	[SerializeField]
	private ScrollRect scrollRect;

	private Action<int> _onApply;

	public void Configure(string prompt, IReadOnlyList<DropdownLocationPickerRowData> options, int selectedIndex, Action<int> onApply)
	{
		promptLabel.text = prompt;
		UpdateForSelection(options[selectedIndex]);
		_onApply = onApply;
		templateRow.gameObject.SetActive(value: false);
		scrollRect.content.DestroyChildrenExcept(templateRow);
		for (int i = 0; i < options.Count; i++)
		{
			DropdownLocationPickerRowData data = options[i];
			DropdownLocationPickerRow dropdownLocationPickerRow = UnityEngine.Object.Instantiate(templateRow, scrollRect.content);
			int theIndex = i;
			dropdownLocationPickerRow.Configure(data, i == selectedIndex, delegate
			{
				UpdateForSelection(options[theIndex]);
				Notify(theIndex);
			});
			dropdownLocationPickerRow.gameObject.SetActive(value: true);
		}
	}

	private void UpdateForSelection(DropdownLocationPickerRowData data)
	{
		currentValueLabel.text = data.Title;
	}

	private void Notify(int index)
	{
		_onApply?.Invoke(index);
	}
}
