using System.Collections.Generic;
using System.Linq;
using UI.Builder;
using UI.Common;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UI.PreferencesWindow;

[RequireComponent(typeof(Window))]
public class BindingsWindow : MonoBehaviour, IBuilderWindow
{
	private Window _window;

	private static BindingsWindow _instance;

	private UIPanel _panel;

	private readonly UIState<string> _selectedTabState = new UIState<string>(null);

	private HashSet<InputAction> _conflicts = new HashSet<InputAction>();

	private UIPanelBuilder _builder;

	public UIBuilderAssets BuilderAssets { get; set; }

	private static BindingsWindow Instance => WindowManager.Shared.GetWindow<BindingsWindow>();

	public static bool CanShow => GameInput.shared != null;

	private static (string title, InputAction[] actions)[] RebindableActions => GameInput.shared.RebindableActions;

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
		_window.Title = "Bindings";
		_panel?.Dispose();
		_panel = UIPanel.Create(_window.contentRectTransform, BuilderAssets, Build);
	}

	private void Build(UIPanelBuilder builder)
	{
		(string title, InputAction[] actions)[] rebindableActions = RebindableActions;
		HashSet<InputAction> conflicts = FindConflicts(rebindableActions.SelectMany(((string title, InputAction[] actions) t) => t.actions));
		_conflicts = conflicts;
		_builder = builder;
		builder.AddTabbedPanels(_selectedTabState, delegate(UITabbedPanelBuilder uITabbedPanelBuilder)
		{
			(string, InputAction[])[] array = rebindableActions;
			for (int i = 0; i < array.Length; i++)
			{
				var (text, actions) = array[i];
				uITabbedPanelBuilder.AddTab(text, text, delegate(UIPanelBuilder uIPanelBuilder)
				{
					uIPanelBuilder.VScrollView(delegate(UIPanelBuilder uIPanelBuilder2)
					{
						InputAction[] array2 = actions;
						foreach (InputAction inputAction in array2)
						{
							uIPanelBuilder2.AddInputBindingControl(inputAction, conflicts.Contains(inputAction), DidRebind);
						}
					});
				});
			}
		});
	}

	private void DidRebind()
	{
		if (!FindConflicts(RebindableActions.SelectMany(((string title, InputAction[] actions) t) => t.actions)).Equals(_conflicts))
		{
			Debug.Log("Conflicts changed, rebuilding");
			_builder.Rebuild();
		}
	}

	private static HashSet<InputAction> FindConflicts(IEnumerable<InputAction> actions)
	{
		HashSet<string> hashSet = new HashSet<string>();
		HashSet<string> hashSet2 = new HashSet<string>();
		foreach (InputAction action in actions)
		{
			if (action.bindings.Count >= 1)
			{
				string item = UniquingKey(action);
				if (hashSet2.Contains(item))
				{
					hashSet.Add(item);
				}
				hashSet2.Add(item);
			}
		}
		HashSet<InputAction> hashSet3 = new HashSet<InputAction>();
		foreach (InputAction action2 in actions)
		{
			string item2 = UniquingKey(action2);
			if (hashSet.Contains(item2))
			{
				hashSet3.Add(action2);
			}
		}
		return hashSet3;
		static string UniquingKey(InputAction action)
		{
			return string.Join("|", action.bindings.Select((InputBinding b) => b.effectivePath));
		}
	}
}
