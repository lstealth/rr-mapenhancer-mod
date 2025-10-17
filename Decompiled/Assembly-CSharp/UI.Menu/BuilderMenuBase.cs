using UI.Builder;
using UI.Common;
using UnityEngine;

namespace UI.Menu;

public abstract class BuilderMenuBase : MonoBehaviour, INavigationView
{
	[SerializeField]
	private UIBuilderAssets panelAssets;

	[SerializeField]
	private RectTransform panelContent;

	private UIPanel _panel;

	public RectTransform RectTransform => GetComponent<RectTransform>();

	private void OnEnable()
	{
		_panel?.Dispose();
		_panel = UIPanel.Create(panelContent, panelAssets, BuildPanelContent);
	}

	private void OnDisable()
	{
		_panel?.Dispose();
	}

	protected abstract void BuildPanelContent(UIPanelBuilder builder);

	public void WillAppear()
	{
	}

	public void WillDisappear()
	{
	}

	public void DidPop()
	{
	}
}
