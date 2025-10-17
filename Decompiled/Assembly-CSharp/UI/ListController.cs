using System;
using System.Collections;
using System.Collections.Generic;
using Serilog;
using UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace UI;

public class ListController : MonoBehaviour
{
	public struct Item
	{
		public readonly string Identifier;

		public string SectionText { get; set; }

		public string ItemText { get; set; }

		public object Value { get; set; }

		public Item(string identifier, string sectionText, string itemText, object value)
		{
			Identifier = identifier;
			SectionText = sectionText;
			ItemText = itemText;
			Value = value;
		}
	}

	[SerializeField]
	private ListRow sectionRowTemplate;

	[SerializeField]
	private ListRow itemRowTemplate;

	[SerializeField]
	private RectTransform scrollContent;

	[SerializeField]
	private ScrollRect scrollRect;

	public Action<string> OnSelectItem;

	private readonly Dictionary<string, ListRow> _rows = new Dictionary<string, ListRow>();

	private string _selectedId;

	private void Awake()
	{
		sectionRowTemplate.gameObject.SetActive(value: false);
		itemRowTemplate.gameObject.SetActive(value: false);
	}

	private void OnEnable()
	{
		if (_selectedId != null)
		{
			ScrollToVisible(_selectedId);
		}
	}

	private void Clear()
	{
		scrollContent.DestroyChildrenExcept(new ListRow[2] { sectionRowTemplate, itemRowTemplate });
		_rows.Clear();
	}

	public void SetData(List<Item> items, string selectedId)
	{
		Clear();
		string text = null;
		foreach (Item item in items)
		{
			if (item.SectionText != text)
			{
				ListRow listRow = UnityEngine.Object.Instantiate(sectionRowTemplate, scrollContent);
				listRow.gameObject.SetActive(value: true);
				listRow.text.text = item.SectionText;
				text = item.SectionText;
			}
			ListRow listRow2 = UnityEngine.Object.Instantiate(itemRowTemplate, scrollContent);
			listRow2.gameObject.SetActive(value: true);
			listRow2.text.text = item.ItemText;
			listRow2.button.onClick.AddListener(delegate
			{
				SetSelected(item.Identifier);
				OnSelectItem?.Invoke(item.Identifier);
			});
			_rows[item.Identifier] = listRow2;
		}
		SetSelected(selectedId);
		if (base.gameObject.activeInHierarchy)
		{
			ScrollToVisible(selectedId);
		}
	}

	private void SetSelected(string selectedId)
	{
		_selectedId = selectedId;
		foreach (var (text2, listRow2) in _rows)
		{
			listRow2.SetSelected(selectedId == text2);
		}
	}

	public void ScrollToVisible(string id)
	{
		if (id == null)
		{
			return;
		}
		ListRow listRow = null;
		foreach (var (text2, listRow3) in _rows)
		{
			if (!(id != text2))
			{
				listRow = listRow3;
				break;
			}
		}
		if (listRow == null)
		{
			Log.Warning("No row with id {id}", id);
		}
		else
		{
			StartCoroutine(_ScrollToVisible(listRow));
		}
	}

	private IEnumerator _ScrollToVisible(ListRow row)
	{
		yield return null;
		scrollRect.ScrollToVisible(row.GetComponent<RectTransform>(), ScrollPosition.OneThird);
	}
}
