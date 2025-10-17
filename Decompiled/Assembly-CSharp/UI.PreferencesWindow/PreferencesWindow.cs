using UI.Builder;
using UI.Common;
using UnityEngine;

namespace UI.PreferencesWindow;

[RequireComponent(typeof(Window))]
public class PreferencesWindow : MonoBehaviour, IBuilderWindow
{
	private Window _window;

	private static PreferencesWindow _instance;

	private UIPanel _panel;

	private readonly UIState<string> _selectedTabState = new UIState<string>(null);

	public UIBuilderAssets BuilderAssets { get; set; }

	private static PreferencesWindow Instance => WindowManager.Shared.GetWindow<PreferencesWindow>();

	private void Awake()
	{
		_window = GetComponent<Window>();
	}

	private void OnDisable()
	{
		_panel?.Dispose();
		_panel = null;
	}

	public static void Toggle()
	{
		if (Instance._window.IsShown)
		{
			Instance._window.CloseWindow();
		}
		else
		{
			Show();
		}
	}

	public static void Show()
	{
		Instance.Populate();
		Instance._window.ShowWindow();
	}

	private void Populate()
	{
		_window.Title = "Preferences";
		_panel?.Dispose();
		_panel = UIPanel.Create(_window.contentRectTransform, BuilderAssets, PreferencesBuilder.Build);
	}
}
