using UnityEngine;

namespace UI.CarEditor.Cells;

public class EditorNestedCell : EditorCellBase
{
	[SerializeField]
	public RectTransform elementCellsRect;

	public void Configure(string text)
	{
		label.text = text;
	}
}
