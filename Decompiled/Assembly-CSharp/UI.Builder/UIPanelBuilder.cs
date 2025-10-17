using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Helpers;
using Model.Ops;
using TMPro;
using Track;
using UI.Common;
using UI.InputRebind;
using UI.LazyScrollList;
using UI.TabView;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UI.Builder;

public struct UIPanelBuilder
{
	public enum Frequency
	{
		Fast,
		Periodic
	}

	public struct ListItem<TValue> : IComparable<ListItem<TValue>>
	{
		public readonly string Identifier;

		public readonly TValue Value;

		public string Section { get; set; }

		public string Text { get; set; }

		public ListItem(string identifier, TValue value, string section, string text)
		{
			Identifier = identifier;
			Value = value;
			Section = section;
			Text = text;
		}

		public int CompareTo(ListItem<TValue> other)
		{
			return string.Compare(Text, other.Text, StringComparison.Ordinal);
		}
	}

	private readonly UIBuilderAssets _assets;

	private readonly RectTransform _container;

	private readonly UIPanel _panel;

	private static GameObject _rebindOverlay;

	public float? FieldLabelWidth { get; set; }

	public float Spacing
	{
		get
		{
			return _container.GetComponent<HorizontalOrVerticalLayoutGroup>().spacing;
		}
		set
		{
			_container.GetComponent<HorizontalOrVerticalLayoutGroup>().spacing = value;
		}
	}

	internal UIPanelBuilder(RectTransform container, UIBuilderAssets assets, UIPanel panel)
	{
		_assets = assets;
		_container = container;
		_panel = panel;
		FieldLabelWidth = null;
	}

	private T InstantiateInContainer<T>(T prefab) where T : UnityEngine.Object
	{
		return UnityEngine.Object.Instantiate(prefab, _container, worldPositionStays: false);
	}

	public void AddTitle(string title, string subtitle)
	{
		PanelTitle panelTitle = InstantiateInContainer(_assets.panelTitle);
		panelTitle.titleLabel.text = title;
		panelTitle.subtitleLabel.text = subtitle;
	}

	public void AddSection(string sectionTitle)
	{
		InstantiateInContainer(_assets.sectionHeader).GetComponentInChildren<TMP_Text>().text = sectionTitle;
	}

	public void AddSection(string sectionTitle, Action<UIPanelBuilder> closure, float spacing = 0f)
	{
		if (!string.IsNullOrEmpty(sectionTitle))
		{
			InstantiateInContainer(_assets.sectionHeader).GetComponentInChildren<TMP_Text>().text = sectionTitle;
		}
		VerticalLayoutGroup verticalLayoutGroup = VStack(closure);
		verticalLayoutGroup.padding = new RectOffset(0, 0, 0, 0);
		verticalLayoutGroup.spacing = spacing;
	}

	private (RectTransform fieldRectTransform, TMP_Text label, RectTransform valueContainer) _AddField()
	{
		RectTransform rectTransform = InstantiateInContainer(_assets.fieldRow);
		TMP_Text component = rectTransform.Find("Label").GetComponent<TMP_Text>();
		RectTransform component2 = rectTransform.Find("Value").GetComponent<RectTransform>();
		if (FieldLabelWidth.HasValue)
		{
			component.GetComponent<RectTransform>().Width(FieldLabelWidth.Value);
		}
		return (fieldRectTransform: rectTransform, label: component, valueContainer: component2);
	}

	public IConfigurableElement AddField(string label, RectTransform control)
	{
		(RectTransform fieldRectTransform, TMP_Text label, RectTransform valueContainer) tuple = _AddField();
		RectTransform item = tuple.fieldRectTransform;
		TMP_Text item2 = tuple.label;
		RectTransform item3 = tuple.valueContainer;
		item2.text = label;
		control.SetParent(item3, worldPositionStays: false);
		control.SetFrameFillParent();
		control.SetTextMarginsTop(4f);
		if (control.GetComponentInChildren<Toggle>() != null)
		{
			item.Height(30f);
			item3.Height(30f);
			item3.ChildAlignment(TextAnchor.MiddleLeft);
		}
		return new ConfigurableElement(item);
	}

	public IConfigurableElement AddField(string label, Func<string> valueClosure, Frequency updateFrequency)
	{
		return AddField(label, AddLabel(valueClosure, updateFrequency));
	}

	public IConfigurableElement AddField(string label, string value)
	{
		return AddField(label, AddLabel(value));
	}

	public IConfigurableElement AddFieldToggle(string label, Func<bool> valueClosure, Action<bool> action, bool interactable = true)
	{
		return new ConfigurableElement(HStack(delegate(UIPanelBuilder builder)
		{
			builder.Spacer(122f);
			builder.AddToggle(valueClosure, action, interactable);
			builder.AddLabel(label);
		}, 8f).Height(30f));
	}

	public IConfigurableElement AddButtonCompact(string text, Action action)
	{
		(RectTransform, TMP_Text) tuple = _assets.CreateButton(UIBuilderAssets.ButtonStyle.Compact, _container, action);
		var (rectTransform, _) = tuple;
		tuple.Item2.text = text;
		return new ConfigurableElement(rectTransform);
	}

	public IConfigurableElement AddButtonCompact(Func<string> textClosure, Action action)
	{
		(RectTransform, TMP_Text) tuple = _assets.CreateButton(UIBuilderAssets.ButtonStyle.Compact, _container, action);
		var (rectTransform, _) = tuple;
		tuple.Item2.gameObject.AddComponent<TextUpdater>().Configure(textClosure, 0.1f);
		return new ConfigurableElement(rectTransform);
	}

	public IConfigurableElement AddButton(string text, Action action)
	{
		(RectTransform, TMP_Text) tuple = _assets.CreateButton(UIBuilderAssets.ButtonStyle.Default, _container, action);
		var (rectTransform, _) = tuple;
		tuple.Item2.text = text;
		return new ConfigurableElement(rectTransform);
	}

	public IConfigurableElement AddButtonMedium(string text, Action action)
	{
		(RectTransform, TMP_Text) tuple = _assets.CreateButton(UIBuilderAssets.ButtonStyle.Medium, _container, action);
		var (rectTransform, _) = tuple;
		tuple.Item2.text = text;
		return new ConfigurableElement(rectTransform);
	}

	public IConfigurableElement AddButtonSelectable(string text, bool selected, Action action)
	{
		UIBuilderAssets.ButtonStyle style = (selected ? UIBuilderAssets.ButtonStyle.Selected : UIBuilderAssets.ButtonStyle.Default);
		(RectTransform, TMP_Text) tuple = _assets.CreateButton(style, _container, action);
		var (rectTransform, _) = tuple;
		tuple.Item2.text = text;
		return new ConfigurableElement(rectTransform);
	}

	public RectTransform AddLabelEmptyState(string text)
	{
		TMP_Text tMP_Text = InstantiateInContainer(_assets.labelEmpty);
		tMP_Text.text = text;
		AddTextLinkReceiverIfNeeded(tMP_Text, text);
		return tMP_Text.GetComponent<RectTransform>();
	}

	public RectTransform AddLabelMarkup(string markup)
	{
		TMP_Text tMP_Text = InstantiateInContainer(_assets.labelControl);
		tMP_Text.SetTextMarkup(markup);
		AddTextLinkReceiverIfNeeded(tMP_Text, tMP_Text.text);
		return tMP_Text.GetComponent<RectTransform>();
	}

	public RectTransform AddLabel(string text)
	{
		TMP_Text tMP_Text = InstantiateInContainer(_assets.labelControl);
		tMP_Text.text = text;
		AddTextLinkReceiverIfNeeded(tMP_Text, text);
		return tMP_Text.GetComponent<RectTransform>();
	}

	public RectTransform AddLabel(string text, Action<TMP_Text> textConfigure)
	{
		TMP_Text tMP_Text = InstantiateInContainer(_assets.labelControl);
		tMP_Text.text = text;
		AddTextLinkReceiverIfNeeded(tMP_Text, text);
		textConfigure(tMP_Text);
		return tMP_Text.GetComponent<RectTransform>();
	}

	private void AddTextLinkReceiverIfNeeded(TMP_Text label, string text)
	{
		if (!string.IsNullOrEmpty(text) && text.Contains("<link"))
		{
			label.gameObject.AddComponent<TextLinkReceiver>();
		}
	}

	public RectTransform AddLabel(Func<string> valueClosure, Frequency updateFrequency)
	{
		float interval = updateFrequency switch
		{
			Frequency.Fast => 0.1f, 
			Frequency.Periodic => 1f, 
			_ => 1f, 
		};
		TMP_Text tMP_Text = InstantiateInContainer(_assets.labelControl);
		tMP_Text.gameObject.AddComponent<TextUpdater>().Configure(valueClosure, interval);
		return tMP_Text.GetComponent<RectTransform>();
	}

	public RectTransform AddTextArea(string text, Action<string> onLinkClicked)
	{
		RectTransform rectTransform = InstantiateInContainer(_assets.textArea);
		TMP_Text componentInChildren = rectTransform.gameObject.GetComponentInChildren<TMP_Text>();
		componentInChildren.text = text;
		componentInChildren.GetComponent<TextLinkReceiver>().OnLinkClicked = onLinkClicked;
		return rectTransform;
	}

	public RectTransform AddMultilineTextEditor(string text, string placeholder, Action<string> onChange, Action<string> onEndEdit)
	{
		RectTransform rectTransform = InstantiateInContainer(_assets.multilineTextEditor);
		TMP_InputField componentInChildren = rectTransform.gameObject.GetComponentInChildren<TMP_InputField>();
		componentInChildren.text = text;
		if (componentInChildren.placeholder != null)
		{
			componentInChildren.placeholder.GetComponent<TMP_Text>().text = placeholder;
		}
		componentInChildren.onValueChanged.AddListener(delegate(string change)
		{
			onChange(change);
		});
		componentInChildren.onEndEdit.AddListener(delegate(string change)
		{
			onEndEdit(change);
		});
		return rectTransform;
	}

	public RectTransform AddToggle(Func<bool> valueClosure, Action<bool> action, bool interactable = true)
	{
		Toggle toggle = InstantiateInContainer(_assets.toggleControl);
		toggle.interactable = interactable;
		toggle.onValueChanged.AddListener(delegate(bool value)
		{
			action(value);
		});
		toggle.gameObject.AddComponent<ToggleUpdater>().Configure(valueClosure);
		return toggle.GetComponent<RectTransform>();
	}

	public RectTransform AddSlider(Func<float> valueClosure, Func<string> textValueClosure, Action<float> valueChangedAction, float minValue = 0f, float maxValue = 1f, bool wholeNumbers = false, Action<float> editingEndedAction = null)
	{
		CarControlSlider carControlSlider = InstantiateInContainer(_assets.carControlSlider);
		carControlSlider.minValue = minValue;
		carControlSlider.maxValue = maxValue;
		carControlSlider.wholeNumbers = wholeNumbers;
		if (valueChangedAction != null)
		{
			carControlSlider.onValueChanged.AddListener(delegate(float value)
			{
				valueChangedAction(value);
			});
		}
		if (editingEndedAction != null)
		{
			carControlSlider.gameObject.AddComponent<SliderPointerUpEventer>().onPointerUp += editingEndedAction;
		}
		carControlSlider.gameObject.AddComponent<SliderUpdater>().Configure(valueClosure);
		carControlSlider.handleText.gameObject.AddComponent<TextUpdater>().Configure(textValueClosure, 0.1f);
		return carControlSlider.GetComponent<RectTransform>();
	}

	public RectTransform AddSliderQuantized(Func<float> valueClosure, Func<string> textValueClosure, Action<float> valueChangedAction, float increment, float minValue = 0f, float maxValue = 1f, Action<float> editingEndedAction = null)
	{
		if (increment == 0f)
		{
			throw new ArgumentException("increment must be non-zero", "increment");
		}
		return AddSlider(() => valueClosure() / increment, textValueClosure, delegate(float value)
		{
			valueChangedAction(value * increment);
		}, minValue / increment, maxValue / increment, wholeNumbers: true, (editingEndedAction == null) ? null : ((Action<float>)delegate(float value)
		{
			editingEndedAction(value * increment);
		}));
	}

	public RectTransform AddDropdown(List<string> values, int currentSelectedIndex, Action<int> onSelect)
	{
		TMP_Dropdown tMP_Dropdown = InstantiateInContainer(_assets.dropdownControl);
		tMP_Dropdown.ClearOptions();
		tMP_Dropdown.AddOptions(values);
		tMP_Dropdown.value = currentSelectedIndex;
		tMP_Dropdown.onValueChanged.AddListener(delegate(int index)
		{
			onSelect(index);
		});
		return tMP_Dropdown.GetComponent<RectTransform>();
	}

	public RectTransform AddColorDropdown(List<string> values, int currentSelectedIndex, Action<int> onSelect)
	{
		RectTransform rectTransform = InstantiateInContainer(_assets.colorDropdownControl);
		TMP_Dropdown componentInChildren = rectTransform.GetComponentInChildren<TMP_Dropdown>();
		Sprite uiSprite = _assets.uiSprite;
		componentInChildren.ClearOptions();
		List<TMP_Dropdown.OptionData> list = new List<TMP_Dropdown.OptionData>();
		foreach (string value in values)
		{
			Color? color = ColorHelper.ColorFromHex(value);
			if (color.HasValue)
			{
				list.Add(new TMP_Dropdown.OptionData(value, uiSprite, color.Value));
			}
		}
		componentInChildren.AddOptions(list);
		componentInChildren.value = currentSelectedIndex;
		componentInChildren.onValueChanged.AddListener(delegate(int index)
		{
			onSelect(index);
		});
		return rectTransform;
	}

	public RectTransform AddOptionsDropdown(IReadOnlyList<DropdownMenu.RowData> options, Action<int> action)
	{
		DropdownMenu dropdownMenu = InstantiateInContainer(_assets.dropdownOptionsControl);
		dropdownMenu.Configure(options, action);
		return dropdownMenu.GetComponent<RectTransform>();
	}

	public RectTransform AddInputField(string value, Action<string> onApply, string placeholder = null, int? characterLimit = null)
	{
		TMP_InputField tMP_InputField = InstantiateInContainer(_assets.inputField);
		tMP_InputField.text = value;
		tMP_InputField.characterLimit = characterLimit.GetValueOrDefault();
		((TMP_Text)tMP_InputField.placeholder).text = placeholder;
		tMP_InputField.onEndEdit.AddListener(delegate(string newValue)
		{
			onApply(newValue);
		});
		return tMP_InputField.GetComponent<RectTransform>();
	}

	public RectTransform AddInputFieldValidated(string value, Action<string> onApply, string regex, string placeholder = null, int? characterLimit = null)
	{
		TMP_InputField tMP_InputField = InstantiateInContainer(_assets.inputField);
		tMP_InputField.text = value;
		tMP_InputField.characterLimit = characterLimit.GetValueOrDefault();
		tMP_InputField.characterValidation = TMP_InputField.CharacterValidation.Regex;
		((TMP_Text)tMP_InputField.placeholder).text = placeholder;
		Type.GetType("TMPro.TMP_InputField, Unity.TextMeshPro", throwOnError: true, ignoreCase: true).GetField("m_RegexValue", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(tMP_InputField, regex);
		tMP_InputField.onEndEdit.AddListener(delegate(string newValue)
		{
			onApply(newValue);
		});
		return tMP_InputField.GetComponent<RectTransform>();
	}

	public RectTransform AddInputFieldReportingMark(string value, Action<string> onApply)
	{
		return AddInputFieldValidated(value, onApply, "[\\p{L}&]", $"Up to {6} letters", 6);
	}

	public RectTransform AddColorDropdown(string hexColor, Action<string> onApply)
	{
		DropdownColorPicker dropdownColorPicker = InstantiateInContainer(_assets.dropdownColorPicker);
		dropdownColorPicker.Configure(hexColor, onApply);
		return dropdownColorPicker.GetComponent<RectTransform>();
	}

	public RectTransform AddLocationPicker(string prompt, List<(IndustryComponent ic, Area area)> options, IndustryComponent selected, Action<IndustryComponent> onApply)
	{
		List<DropdownLocationPickerRowData> options2 = options.Select(((IndustryComponent ic, Area area) option) => DropdownLocationPickerRowData.From(option.ic, option.area)).ToList();
		int selectedIndex = options.FindIndex(((IndustryComponent ic, Area area) tuple) => tuple.ic == selected);
		return AddLocationPicker(prompt, options2, selectedIndex, delegate(int index)
		{
			onApply((index < 0) ? null : options[index].ic);
		});
	}

	public RectTransform AddLocationPicker(string prompt, List<DropdownLocationPickerRowData> options, int selectedIndex, Action<int> onApply)
	{
		DropdownLocationPicker dropdownLocationPicker = InstantiateInContainer(_assets.dropdownLocationPicker);
		dropdownLocationPicker.Configure(prompt, options, selectedIndex, onApply);
		return dropdownLocationPicker.GetComponent<RectTransform>();
	}

	public RectTransform AddLocationField(string locationName, IIndustryTrackDisplayable industryTrackDisplayable, Action jump)
	{
		return _AddLocationField(locationName, null, industryTrackDisplayable, jump);
	}

	public RectTransform AddLocationField(Func<string> icNameClosure, IIndustryTrackDisplayable industryTrackDisplayable, Action jump)
	{
		return _AddLocationField(null, icNameClosure, industryTrackDisplayable, jump);
	}

	private RectTransform _AddLocationField(string icName, Func<string> icNameClosure, IIndustryTrackDisplayable ic, Action jump)
	{
		RectTransform rectTransform = InstantiateInContainer(_assets.locationField);
		Button componentInChildren = rectTransform.GetComponentInChildren<Button>();
		TMP_Text componentInChildren2 = rectTransform.GetComponentInChildren<TMP_Text>();
		LocationIndicatorHoverArea componentInChildren3 = rectTransform.GetComponentInChildren<LocationIndicatorHoverArea>();
		if (icName != null)
		{
			componentInChildren2.text = icName;
			componentInChildren2.GetComponent<RectTransform>().Tooltip("Location", icName);
		}
		else
		{
			componentInChildren2.gameObject.AddComponent<TextUpdater>().Configure(icNameClosure, 1f);
		}
		componentInChildren.onClick.AddListener(delegate
		{
			jump();
		});
		componentInChildren.GetComponent<RectTransform>().Tooltip("<sprite name=\"MouseLeft\"> Jump To", icName);
		componentInChildren3.spanIds.AddRange(ic.TrackSpans.Select((TrackSpan ts) => ts.id));
		componentInChildren3.descriptors.Add(new LocationIndicatorController.Descriptor(ic.CenterPoint, ic.DisplayName, null));
		return rectTransform;
	}

	public RectTransform AddLocationFieldFallback(string icName, Action jump)
	{
		RectTransform rectTransform = InstantiateInContainer(_assets.locationField);
		Button componentInChildren = rectTransform.GetComponentInChildren<Button>();
		TMP_Text componentInChildren2 = rectTransform.GetComponentInChildren<TMP_Text>();
		componentInChildren2.text = icName;
		componentInChildren2.GetComponent<RectTransform>().Tooltip("Location", icName);
		componentInChildren.onClick.AddListener(delegate
		{
			jump();
		});
		componentInChildren.GetComponent<RectTransform>().Tooltip("<sprite name=\"MouseLeft\"> Jump To", icName);
		return rectTransform;
	}

	private RectTransform CreateRectView(string name, int width, int height)
	{
		GameObject gameObject = new GameObject(name);
		gameObject.hideFlags = HideFlags.DontSave;
		gameObject.transform.SetParent(_container, worldPositionStays: false);
		RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
		rectTransform.SetFrame(0f, 0f, width, height);
		return rectTransform;
	}

	public RectTransform HStack(Action<UIPanelBuilder> closure, float spacing = 4f)
	{
		RectTransform rectTransform = CreateRectView("HStack", 0, 0);
		HorizontalLayoutGroup horizontalLayoutGroup = rectTransform.gameObject.AddComponent<HorizontalLayoutGroup>();
		horizontalLayoutGroup.spacing = spacing;
		horizontalLayoutGroup.childForceExpandWidth = false;
		_panel.AddChild(rectTransform, closure);
		return rectTransform;
	}

	public RectTransform AlertButtons(Action<UIPanelBuilder> closure)
	{
		RectTransform rectTransform = CreateRectView("AlertButtons", 0, 0);
		HorizontalLayoutGroup horizontalLayoutGroup = rectTransform.gameObject.AddComponent<HorizontalLayoutGroup>();
		horizontalLayoutGroup.spacing = 8f;
		horizontalLayoutGroup.childForceExpandWidth = false;
		bool cancelLast = Application.platform switch
		{
			RuntimePlatform.WindowsPlayer => true, 
			RuntimePlatform.WindowsEditor => true, 
			_ => false, 
		};
		_panel.AddChild(rectTransform, delegate(UIPanelBuilder builder)
		{
			builder.Spacer();
			closure(builder);
			int num = rectTransform.childCount - 1;
			for (int i = 0; i < rectTransform.childCount; i++)
			{
				Transform child = rectTransform.GetChild(i);
				if (child.GetComponentsInChildren<TMP_Text>().Any((TMP_Text t) => t.text == "Cancel"))
				{
					if (i == 1 && !cancelLast)
					{
						break;
					}
					if (i != num && cancelLast)
					{
						child.SetSiblingIndex(num);
						break;
					}
					if (i != 1 && !cancelLast)
					{
						child.SetSiblingIndex(1);
						break;
					}
				}
			}
		});
		return rectTransform;
	}

	public RectTransform ButtonStrip(Action<UIPanelBuilder> closure, int spacing = 8)
	{
		RectTransform rectTransform = HStack(closure, spacing);
		rectTransform.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = false;
		return rectTransform;
	}

	public VerticalLayoutGroup VStack(Action<UIPanelBuilder> closure)
	{
		RectTransform rectTransform = CreateRectView("VStack", 0, 0);
		VerticalLayoutGroup verticalLayoutGroup = rectTransform.gameObject.AddComponent<VerticalLayoutGroup>();
		verticalLayoutGroup.childForceExpandHeight = false;
		verticalLayoutGroup.childControlHeight = true;
		_panel.AddChild(rectTransform, closure);
		return verticalLayoutGroup;
	}

	public RectTransform HVScrollView(Action<UIPanelBuilder> closure, RectOffset padding = null)
	{
		ScrollRect scrollRect = InstantiateInContainer(_assets.scrollRectHorizontalVertical);
		ContentSizeFitter contentSizeFitter = scrollRect.content.gameObject.AddComponent<ContentSizeFitter>();
		contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
		contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
		VerticalLayoutGroup verticalLayoutGroup = scrollRect.content.gameObject.AddComponent<VerticalLayoutGroup>();
		verticalLayoutGroup.childControlWidth = true;
		verticalLayoutGroup.childControlHeight = true;
		verticalLayoutGroup.childForceExpandWidth = true;
		verticalLayoutGroup.childForceExpandHeight = false;
		RectTransform component = scrollRect.GetComponent<RectTransform>();
		LayoutElement layoutElement = component.gameObject.AddComponent<LayoutElement>();
		layoutElement.flexibleWidth = 10000f;
		layoutElement.flexibleHeight = 10000f;
		if (padding != null)
		{
			verticalLayoutGroup.padding = padding;
		}
		_panel.AddChild(scrollRect.content, closure);
		return component;
	}

	public RectTransform VScrollView(Action<UIPanelBuilder> closure, RectOffset padding = null)
	{
		ScrollRect scrollRect = InstantiateInContainer(_assets.scrollRectVertical);
		RectTransform component = scrollRect.GetComponent<RectTransform>();
		LayoutElement layoutElement = component.gameObject.AddComponent<LayoutElement>();
		layoutElement.flexibleWidth = 10000f;
		layoutElement.flexibleHeight = 10000f;
		if (padding != null)
		{
			scrollRect.content.GetComponent<VerticalLayoutGroup>().padding = padding;
		}
		_panel.AddChild(scrollRect.content, closure);
		return component;
	}

	public RectTransform HScrollView(Action<UIPanelBuilder> closure, RectOffset padding = null)
	{
		ScrollRect scrollRect = InstantiateInContainer(_assets.scrollRectHorizontal);
		RectTransform component = scrollRect.GetComponent<RectTransform>();
		LayoutElement layoutElement = component.gameObject.AddComponent<LayoutElement>();
		layoutElement.flexibleWidth = 10000f;
		layoutElement.flexibleHeight = 10000f;
		if (padding != null)
		{
			scrollRect.content.GetComponent<VerticalLayoutGroup>().padding = padding;
		}
		_panel.AddChild(scrollRect.content, closure);
		return component;
	}

	public void LazyScrollList(List<object> data, string cellPrefabName)
	{
		ScrollRect scrollRect = InstantiateInContainer(_assets.scrollRectVertical);
		LayoutElement layoutElement = scrollRect.GetComponent<RectTransform>().gameObject.AddComponent<LayoutElement>();
		layoutElement.flexibleWidth = 10000f;
		layoutElement.flexibleHeight = 10000f;
		scrollRect.content.GetComponent<ContentSizeFitter>().enabled = false;
		scrollRect.content.GetComponent<VerticalLayoutGroup>().enabled = false;
		scrollRect.gameObject.SetActive(value: false);
		UI.LazyScrollList.LazyScrollList lazyScrollList = scrollRect.gameObject.AddComponent<UI.LazyScrollList.LazyScrollList>();
		UIBuilderAssets.ListCellPrefab[] listCellPrefabs = _assets.listCellPrefabs;
		for (int i = 0; i < listCellPrefabs.Length; i++)
		{
			UIBuilderAssets.ListCellPrefab listCellPrefab = listCellPrefabs[i];
			if (!(listCellPrefab.name != cellPrefabName))
			{
				lazyScrollList.cellPrefab = listCellPrefab.prefab;
				break;
			}
		}
		lazyScrollList.SetData(data);
		scrollRect.gameObject.SetActive(value: true);
	}

	public void AddTable(List<TableRow> tableRows, List<float> columnWidths, TableBuilderConfig config)
	{
		InstantiateInContainer(_assets.tableBuilder).Build(tableRows, columnWidths, config);
	}

	public RectTransform Spacer()
	{
		RectTransform rectTransform = CreateRectView("Spacer", 0, 0);
		LayoutElement layoutElement = rectTransform.gameObject.AddComponent<LayoutElement>();
		layoutElement.flexibleWidth = 1f;
		layoutElement.flexibleHeight = 1f;
		return rectTransform;
	}

	public void AddExpandingVerticalSpacer()
	{
		CreateRectView("ExpandingVSpacer", 0, 0).gameObject.AddComponent<LayoutElement>().flexibleHeight = 10000f;
	}

	public void Spacer(float size)
	{
		GameObject gameObject = new GameObject("Spacer");
		gameObject.hideFlags = HideFlags.DontSave;
		gameObject.transform.SetParent(_container, worldPositionStays: false);
		gameObject.AddComponent<RectTransform>().SetFrame(0f, 0f, size, size);
		LayoutElement layoutElement = gameObject.AddComponent<LayoutElement>();
		layoutElement.preferredWidth = size;
		layoutElement.preferredHeight = size;
	}

	public void AddTabbedPanels(UIState<string> selectedTab, Action<UITabbedPanelBuilder> builderClosure)
	{
		UI.TabView.TabView tabView = InstantiateInContainer(_assets.tabView);
		tabView.Parent = _panel;
		tabView.SelectedTabState = selectedTab;
		tabView.assets = _assets;
		UITabbedPanelBuilder obj = new UITabbedPanelBuilder(tabView);
		builderClosure(obj);
		obj.Finish();
	}

	public void AddInputBindingControl(InputAction inputAction, bool conflict, Action onRebind)
	{
		RebindActionUI rebindActionUI = InstantiateInContainer(_assets.rebindControl);
		rebindActionUI.enabled = false;
		rebindActionUI.Action = inputAction;
		rebindActionUI.BindingId = inputAction.bindings[0].id.ToString();
		if (_rebindOverlay == null)
		{
			_rebindOverlay = UnityEngine.Object.Instantiate(parent: GetRootRectTransform(), original: _assets.rebindOverlay, worldPositionStays: false);
			_rebindOverlay.SetActive(value: false);
		}
		rebindActionUI.RebindOverlay = _rebindOverlay;
		rebindActionUI.RebindPrompt = _rebindOverlay.GetComponentInChildren<TMP_Text>();
		rebindActionUI.Conflict = conflict;
		rebindActionUI.OnSave = onRebind;
		rebindActionUI.enabled = true;
	}

	private static RectTransform GetRootRectTransform()
	{
		return WindowManager.Shared.GetComponent<Canvas>().GetComponent<RectTransform>();
	}

	public void AddListDetail<TValue>(IEnumerable<ListItem<TValue>> data, UIState<string> selectedItem, Action<UIPanelBuilder, TValue> builderClosure, float? listWidth = null) where TValue : class
	{
		InstantiateInContainer(_assets.listDetailController).Configure(_panel, _assets, data, selectedItem, builderClosure, listWidth);
	}

	public void AddBuilderPhoto(string carIdentifier)
	{
		InstantiateInContainer(_assets.builderPhoto).Configure(carIdentifier);
	}

	private RectTransform CreateRule(string name)
	{
		RectTransform rectTransform = CreateRectView(name, 1, 1);
		rectTransform.gameObject.AddComponent<Image>().color = ColorHelper.ColorFromHex("#4E493E") ?? Color.gray;
		return rectTransform;
	}

	public RectTransform AddVRule()
	{
		return CreateRule("VRule").Width(1f).FlexibleHeight();
	}

	public RectTransform AddHRule()
	{
		return CreateRule("HRule").Height(1f).FlexibleWidth();
	}

	public void RebuildOnEvent<T>()
	{
		_panel.RebuildOnEvent<T>();
	}

	public void RebuildOnInterval(float seconds)
	{
		_panel.RebuildOnInterval(seconds);
	}

	public void Rebuild()
	{
		if (_panel != null)
		{
			_panel.Rebuild();
		}
	}

	public void AddObserver(IDisposable disposable)
	{
		_panel.AddObserver(disposable);
	}
}
