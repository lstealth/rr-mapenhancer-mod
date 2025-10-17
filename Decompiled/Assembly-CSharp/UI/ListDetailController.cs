using System;
using System.Collections.Generic;
using System.Linq;
using UI.Builder;
using UnityEngine;
using UnityEngine.UI;

namespace UI;

public class ListDetailController : MonoBehaviour
{
	[SerializeField]
	private ListController listController;

	[SerializeField]
	private RectTransform detailRectTransform;

	private UIBuilderAssets _assets;

	private UIPanel _parent;

	private UIPanel _detailPanel;

	private object _selected;

	private UIState<string> _selectedItemState;

	private readonly Dictionary<string, object> _valueLookup = new Dictionary<string, object>();

	public void Configure<TValue>(UIPanel parent, UIBuilderAssets assets, IEnumerable<UIPanelBuilder.ListItem<TValue>> data, UIState<string> selectedItem, Action<UIPanelBuilder, TValue> builderClosure, float? listWidth) where TValue : class
	{
		_parent = parent;
		_assets = assets;
		_selectedItemState = selectedItem;
		_valueLookup.Clear();
		foreach (UIPanelBuilder.ListItem<TValue> datum in data)
		{
			_valueLookup[datum.Identifier] = datum.Value;
		}
		listController.SetData(data.Select((UIPanelBuilder.ListItem<TValue> i) => new ListController.Item(i.Identifier, i.Section, i.Text, i.Value)).ToList(), _selectedItemState.Value);
		listController.OnSelectItem = Select;
		if (listWidth.HasValue)
		{
			float valueOrDefault = listWidth.GetValueOrDefault();
			LayoutElement component = listController.GetComponent<LayoutElement>();
			component.minWidth = valueOrDefault;
			component.preferredWidth = valueOrDefault;
		}
		_detailPanel = _parent.AddChild(detailRectTransform, delegate(UIPanelBuilder detailBuilder)
		{
			if (_selected is TValue arg)
			{
				builderClosure(detailBuilder, arg);
			}
			else
			{
				builderClosure(detailBuilder, null);
			}
		});
		if (!string.IsNullOrEmpty(_selectedItemState.Value))
		{
			Select(_selectedItemState.Value);
		}
	}

	private void Select(string identifier)
	{
		_selectedItemState.Value = identifier;
		if (!string.IsNullOrEmpty(identifier) && _valueLookup.TryGetValue(identifier, out var value))
		{
			_selected = value;
		}
		else
		{
			_selected = null;
		}
		_detailPanel?.Rebuild();
	}
}
