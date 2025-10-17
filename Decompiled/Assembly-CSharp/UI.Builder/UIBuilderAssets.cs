using System;
using TMPro;
using UI.Equipment;
using UI.InputRebind;
using UI.TabView;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Builder;

[CreateAssetMenu(fileName = "UIBuilderAssets", menuName = "Railroader/UI/UIBuilderAssets", order = 0)]
public class UIBuilderAssets : ScriptableObject
{
	[Serializable]
	public struct ListCellPrefab
	{
		public string name;

		public GameObject prefab;
	}

	public enum ButtonStyle
	{
		Default,
		Selected,
		Compact,
		Medium
	}

	public PanelTitle panelTitle;

	public RectTransform sectionHeader;

	public RectTransform fieldRow;

	public RectTransform locationField;

	[Header("Buttons")]
	public Button button;

	public Button buttonSelected;

	public Button buttonCompact;

	public Button buttonMedium;

	[Header("Controls")]
	public TMP_Text labelControl;

	public TMP_Text labelEmpty;

	public RectTransform textArea;

	public RectTransform multilineTextEditor;

	public TMP_InputField inputField;

	public TMP_Dropdown dropdownControl;

	public DropdownMenu dropdownOptionsControl;

	public RectTransform colorDropdownControl;

	public DropdownColorPicker dropdownColorPicker;

	public DropdownLocationPicker dropdownLocationPicker;

	public Toggle toggleControl;

	public CarControlSlider carControlSlider;

	[Header("Rebinding")]
	public RebindActionUI rebindControl;

	public GameObject rebindOverlay;

	[Header("Containers")]
	public UI.TabView.TabView tabView;

	public ListDetailController listDetailController;

	public ScrollRect scrollRectVertical;

	public ScrollRect scrollRectHorizontal;

	public ScrollRect scrollRectHorizontalVertical;

	public TableBuilder tableBuilder;

	public Sprite uiSprite;

	[Header("List Cell Prefabs")]
	public ListCellPrefab[] listCellPrefabs;

	[Header("Specialized")]
	public BuilderPhoto builderPhoto;

	public (RectTransform, TMP_Text) CreateButton(ButtonStyle style, Transform parent, Action action)
	{
		return _CreateButton(style switch
		{
			ButtonStyle.Default => button, 
			ButtonStyle.Selected => buttonSelected, 
			ButtonStyle.Compact => buttonCompact, 
			ButtonStyle.Medium => buttonMedium, 
			_ => throw new ArgumentOutOfRangeException("style", style, null), 
		}, parent, action);
	}

	private static (RectTransform, TMP_Text) _CreateButton(Button prefab, Transform parent, Action action)
	{
		Button obj = UnityEngine.Object.Instantiate(prefab, parent, worldPositionStays: false);
		obj.onClick.AddListener(action.Invoke);
		RectTransform component = obj.GetComponent<RectTransform>();
		TMP_Text componentInChildren = obj.GetComponentInChildren<TMP_Text>();
		return (component, componentInChildren);
	}
}
