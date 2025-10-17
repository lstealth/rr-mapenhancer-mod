using UnityEngine;
using UnityEngine.InputSystem;

namespace UI.Builder;

public class BuilderDemo : MonoBehaviour
{
	[SerializeField]
	private UIBuilderAssets builderAssets;

	[SerializeField]
	private RectTransform rectTransform;

	[SerializeField]
	private InputActionAsset inputActionAsset;

	private UIPanel _panel;

	private string _hexString = "4444aa";

	private void OnEnable()
	{
		_panel?.Dispose();
		_panel = UIPanel.Create(rectTransform, builderAssets, BuildPanelContent);
	}

	private void BuildPanelContent(UIPanelBuilder builder)
	{
		builder.AddField("Base Color", builder.AddColorDropdown(_hexString, delegate(string newValue)
		{
			Debug.Log("onApply received: " + newValue);
			_hexString = newValue;
		}));
		builder.AddInputBindingControl(inputActionAsset["Game/Move"], conflict: false, delegate
		{
		});
		builder.AddInputBindingControl(inputActionAsset["Game/Run"], conflict: false, delegate
		{
		});
		builder.AddInputBindingControl(inputActionAsset["Game/Jump"], conflict: false, delegate
		{
		});
		builder.AddInputBindingControl(inputActionAsset["Game/Teleport"], conflict: false, delegate
		{
		});
		builder.AddExpandingVerticalSpacer();
		builder.AddButtonCompact("Rebuild", ((UIPanelBuilder)builder).Rebuild);
	}

	private void OnDisable()
	{
		_panel?.Dispose();
	}
}
