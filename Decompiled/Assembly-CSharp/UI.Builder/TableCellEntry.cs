using System;

namespace UI.Builder;

public struct TableCellEntry
{
	public string Text;

	public string TrailingText;

	public int ColumnSpan;

	public byte BorderMask;

	public bool Highlighted;

	public Action OnClick;

	public (int, int) AutoSize;

	public TableCellEntry(string text, int columnSpan, byte borderMask, bool highlighted = false, Action onClick = null)
	{
		Text = text;
		TrailingText = null;
		ColumnSpan = columnSpan;
		BorderMask = borderMask;
		Highlighted = highlighted;
		OnClick = onClick;
		AutoSize = default((int, int));
	}

	public TableCellEntry WithAutoSize(int minSize, int maxSize)
	{
		TableCellEntry result = this;
		result.AutoSize = (minSize, maxSize);
		return result;
	}

	public TableCellEntry WithTrailingText(string trailingText)
	{
		TableCellEntry result = this;
		result.TrailingText = trailingText;
		return result;
	}
}
