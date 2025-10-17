using System.Collections.Generic;
using UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Builder;

[RequireComponent(typeof(RectTransform))]
public class TableBuilder : MonoBehaviour
{
	[SerializeField]
	private TableCell cellPrefab;

	private RectTransform _rectTransform;

	private LayoutElement _layoutElement;

	private void Awake()
	{
	}

	public void Build(List<TableRow> rows, List<float> columnWidths, TableBuilderConfig config)
	{
		if ((object)_rectTransform == null)
		{
			_rectTransform = GetComponent<RectTransform>();
		}
		if ((object)_layoutElement == null)
		{
			_layoutElement = GetComponent<LayoutElement>() ?? base.gameObject.AddComponent<LayoutElement>();
		}
		_rectTransform.DestroyChildren();
		float num = 1f + config.LeadingInset;
		float num2 = 1f;
		float num3 = num;
		foreach (float columnWidth in columnWidths)
		{
			num3 += columnWidth;
		}
		for (int i = 0; i < rows.Count; i++)
		{
			TableRow tableRow = rows[i];
			float num4 = num;
			int num5 = 0;
			for (int j = 0; j < tableRow.Cells.Count; j++)
			{
				TableCellEntry tableCellEntry = tableRow.Cells[j];
				TableCell tableCell = Object.Instantiate(cellPrefab, _rectTransform);
				tableCell.name = $"{i}-{j}";
				RectTransform component = tableCell.GetComponent<RectTransform>();
				float num6 = 0f;
				for (int k = num5; k < num5 + tableCellEntry.ColumnSpan; k++)
				{
					num6 += columnWidths[k];
				}
				component.anchorMin = new Vector2(0f, 1f);
				component.anchorMax = new Vector2(0f, 1f);
				component.pivot = new Vector2(0f, 1f);
				component.sizeDelta = new Vector2(num6, tableRow.Height);
				component.anchoredPosition = new Vector2(num4, 0f - num2);
				tableCell.Configure(config, tableCellEntry.Text, tableCellEntry.TrailingText, tableCellEntry.BorderMask, tableCellEntry.Highlighted, tableCellEntry.OnClick);
				(int, int) autoSize = tableCellEntry.AutoSize;
				if (autoSize.Item1 != 0 || autoSize.Item2 != 0)
				{
					tableCell.ConfigureAutoSize(tableCellEntry.AutoSize.Item1, tableCellEntry.AutoSize.Item2);
				}
				num4 += num6;
				num5 += tableCellEntry.ColumnSpan;
			}
			num2 += tableRow.Height;
		}
		_layoutElement.preferredWidth = num3;
		_layoutElement.preferredHeight = num2;
	}
}
