using System;
using System.Collections.Generic;
using UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace UI;

public class DropdownMenu : DropdownPickerBase
{
	public struct RowData
	{
		public readonly CheckState Check;

		public readonly string Title;

		public readonly string Subtitle;

		public RowData(CheckState checkState, string title, string subtitle)
		{
			Check = checkState;
			Title = title;
			Subtitle = subtitle;
		}

		public RowData(string title, string subtitle)
		{
			Check = CheckState.None;
			Title = title;
			Subtitle = subtitle;
		}
	}

	public enum CheckState
	{
		None,
		Checked,
		Unchecked
	}

	[SerializeField]
	private DropdownMenuItem templateItem;

	[SerializeField]
	private ScrollRect scrollRect;

	private Action<int> _onApply;

	public void Configure(IReadOnlyList<RowData> options, Action<int> onApply)
	{
		_onApply = onApply;
		templateItem.gameObject.SetActive(value: false);
		scrollRect.content.DestroyChildrenExcept(templateItem);
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < options.Count; i++)
		{
			RowData data = options[i];
			DropdownMenuItem dropdownMenuItem = UnityEngine.Object.Instantiate(templateItem, scrollRect.content);
			int theIndex = i;
			dropdownMenuItem.Configure(data, delegate
			{
				Notify(theIndex);
				Hide();
			});
			int num3 = (string.IsNullOrEmpty(data.Subtitle) ? 30 : 44);
			dropdownMenuItem.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num3);
			dropdownMenuItem.gameObject.SetActive(value: true);
			num2 += (float)num3;
			num = Mathf.Max(num, dropdownMenuItem.GetPreferredWidth());
		}
		dropdown.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num2 + 6f);
		dropdown.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, num + 6f);
	}

	private void Notify(int index)
	{
		_onApply?.Invoke(index);
	}
}
