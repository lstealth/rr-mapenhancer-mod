using System;
using System.Collections.Generic;
using TMPro;
using UI.Builder;
using UnityEngine;
using UnityEngine.UI;

namespace UI.TabView;

public class TabView : MonoBehaviour
{
	[SerializeField]
	private Toggle togglePrefab;

	[SerializeField]
	private RectTransform buttons;

	[SerializeField]
	private RectTransform content;

	public UIBuilderAssets assets;

	private readonly List<string> _tabIds = new List<string>();

	private readonly List<Action<UIPanelBuilder>> _tabBuildClosures = new List<Action<UIPanelBuilder>>();

	private readonly List<Toggle> _toggles = new List<Toggle>();

	private ToggleGroup _toggleGroup;

	private UIPanel _contentPanel;

	public UIState<string> SelectedTabState { get; set; }

	public UIPanel Parent { get; set; }

	public void AddTab(string title, string tabId, Action<UIPanelBuilder> closure)
	{
		if ((object)_toggleGroup == null)
		{
			_toggleGroup = buttons.GetComponent<ToggleGroup>();
		}
		Toggle toggle = UnityEngine.Object.Instantiate(togglePrefab, buttons, worldPositionStays: false);
		toggle.isOn = false;
		toggle.onValueChanged.AddListener(delegate(bool selected)
		{
			if (selected)
			{
				SelectedTabState.Value = tabId;
				UpdateForSelectTab(tabId);
			}
			if (!selected && !_toggleGroup.AnyTogglesOn())
			{
				toggle.SetIsOnWithoutNotify(value: true);
			}
		});
		_toggleGroup.RegisterToggle(toggle);
		toggle.GetComponentInChildren<TMP_Text>().text = title;
		_tabIds.Add(tabId);
		_tabBuildClosures.Add(closure);
		_toggles.Add(toggle);
	}

	private void UpdateForSelectTab(string tabId)
	{
		int num = IndexForTabId(tabId);
		for (int i = 0; i < _toggles.Count; i++)
		{
			Toggle toggle = _toggles[i];
			bool isOnWithoutNotify = i == num;
			toggle.SetIsOnWithoutNotify(isOnWithoutNotify);
		}
		_contentPanel.Rebuild();
	}

	private int IndexForTabId(string tabId)
	{
		int num = _tabIds.IndexOf(tabId);
		if (num < 0)
		{
			num = 0;
		}
		return num;
	}

	public void FinishedAddingTabs()
	{
		float num = 0f;
		foreach (Toggle toggle in _toggles)
		{
			TMP_Text componentInChildren = toggle.GetComponentInChildren<TMP_Text>();
			num += componentInChildren.preferredWidth;
		}
		foreach (Toggle toggle2 in _toggles)
		{
			LayoutElement component = toggle2.GetComponent<LayoutElement>();
			float num2 = (component.preferredWidth = toggle2.GetComponentInChildren<TMP_Text>().preferredWidth);
			component.flexibleWidth = num2 / num;
		}
		_contentPanel = Parent.AddChild(content, delegate(UIPanelBuilder contentBuilder)
		{
			string value = SelectedTabState.Value;
			int num3 = IndexForTabId(value);
			if (num3 >= 0 && num3 < _tabBuildClosures.Count)
			{
				_contentPanel?.UnregisterForEvents();
				contentBuilder.Spacing = 0f;
				_tabBuildClosures[num3](contentBuilder);
			}
		});
		UpdateForSelectTab(SelectedTabState.Value);
	}
}
