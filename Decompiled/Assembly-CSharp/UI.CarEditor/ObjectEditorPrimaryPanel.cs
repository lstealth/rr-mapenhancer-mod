using System;
using System.Collections.Generic;
using Model.Definition;
using UI.CarEditor.Cells;
using UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace UI.CarEditor;

public class ObjectEditorPrimaryPanel : MonoBehaviour
{
	[SerializeField]
	private RectTransform scrollContent;

	[Header("Cell Prototypes")]
	[SerializeField]
	private EditorHeaderCell headerCellPrototype;

	[SerializeField]
	private EditorSelectableCell itemCellPrototype;

	private ContainerItem _item;

	private Action<object> _onSelect;

	private EditorSelectableCell _selectedCell;

	private object _selectedObject;

	private List<UnityEngine.Component> CellPrototypes => new List<UnityEngine.Component> { headerCellPrototype, itemCellPrototype };

	private void Awake()
	{
		foreach (UnityEngine.Component cellPrototype in CellPrototypes)
		{
			cellPrototype.gameObject.SetActive(value: false);
		}
	}

	public void Configure(ContainerItem item, object selectedObject, Action<object> onSelect)
	{
		_item = item;
		_onSelect = onSelect;
		_selectedObject = selectedObject;
		scrollContent.DestroyChildrenExcept(CellPrototypes);
		_selectedCell = null;
		AddHeader("Object Definition");
		AddCell("Metadata", new CarMetaProxy(_item.Identifier, _item.Metadata));
		AddCell(_item.Definition.Kind, _item.Definition);
		AddHeader("Components");
		foreach (Model.Definition.Component item2 in _item.Definition.Components ?? new List<Model.Definition.Component>())
		{
			string text = (string.IsNullOrEmpty(item2.Name) ? item2.Kind : item2.Name);
			AddCell(text, item2, item2.Enabled);
		}
		ScrollToSelectedObject();
	}

	private void ScrollToSelectedObject()
	{
		if (_selectedObject == null)
		{
			return;
		}
		ScrollRect componentInParent = scrollContent.GetComponentInParent<ScrollRect>();
		RectTransform rectTransform = null;
		foreach (RectTransform item in scrollContent)
		{
			EditorSelectableCell component = item.GetComponent<EditorSelectableCell>();
			if (!(component == null) && component.IsSelected)
			{
				rectTransform = item;
			}
		}
		if (rectTransform == null)
		{
			Debug.LogError("Couldn't find selected cell");
			return;
		}
		Canvas.ForceUpdateCanvases();
		float y = componentInParent.transform.InverseTransformPoint(scrollContent.position).y - componentInParent.transform.InverseTransformPoint(rectTransform.position).y;
		scrollContent.anchoredPosition = new Vector2(0f, y);
	}

	private void AddHeader(string text)
	{
		EditorHeaderCell editorHeaderCell = UnityEngine.Object.Instantiate(headerCellPrototype, scrollContent);
		editorHeaderCell.gameObject.SetActive(value: true);
		editorHeaderCell.Configure(text);
	}

	private void AddCell(string text, object selection, bool componentEnabled = true)
	{
		EditorSelectableCell cell = UnityEngine.Object.Instantiate(itemCellPrototype, scrollContent);
		cell.gameObject.SetActive(value: true);
		cell.IsSelected = selection == _selectedObject;
		cell.Configure(text, componentEnabled, delegate
		{
			if (_selectedCell != null)
			{
				_selectedCell.IsSelected = false;
			}
			_selectedCell = cell;
			_selectedCell.IsSelected = true;
			_selectedObject = selection;
			_onSelect(selection);
		});
	}
}
