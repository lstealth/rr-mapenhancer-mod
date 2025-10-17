using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Helpers;
using KeyValue.Runtime;
using Markroader;
using MoonSharp.Interpreter;
using Serilog;
using Track;
using UI;
using UI.Builder;
using UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripting.Interactive;

public class InteractiveBookWindow : MonoBehaviour, IProgrammaticWindow, IBuilderWindow, IPageUI
{
	private struct ElementSay
	{
		public string Text;
	}

	private struct ElementGoal
	{
		public string Title;

		public string Message;

		public float Value;

		public GoalStyle Style;

		public string CustomDisplay;
	}

	private enum GoalStyle
	{
		Percent,
		Boolean
	}

	private Window _window;

	private UIPanel _panel;

	private InteractiveBookRunner _runner;

	private UnityEngine.Coroutine _coroutine;

	private UnityEngine.Coroutine _refreshCoroutine;

	private UnityEngine.Coroutine _rebuildCoroutine;

	private RectTransform _scrollViewRectTransform;

	private UIPanelBuilder? _scrollViewBuilder;

	private UIPanelBuilder? _bottomBarBuilder;

	private bool _showReloadButton;

	private IKeyValueObject _keyValueObject;

	private IDisposable _completeObserver;

	private bool _pendingCloseRequest;

	private readonly List<int> _arrowOverlayIds = new List<int>();

	public Action OnPlayerClosed;

	private readonly List<object> _elements = new List<object>();

	private readonly List<ElementButton> _navButtons = new List<ElementButton>();

	public string WindowIdentifier => "InteractiveBook";

	public Vector2Int DefaultSize => new Vector2Int(425, 650);

	public Window.Position DefaultPosition => Window.Position.CenterRight;

	public Window.Sizing Sizing => Window.Sizing.Resizable(new Vector2Int(300, 200), new Vector2Int(800, 1000));

	public UIBuilderAssets BuilderAssets { get; set; }

	public bool IsShown
	{
		get
		{
			if (_window != null)
			{
				return _window.IsShown;
			}
			return false;
		}
	}

	public static InteractiveBookWindow Shared => WindowManager.Shared.GetWindow<InteractiveBookWindow>();

	public void Show(string directoryName, string bookName, IKeyValueObject keyValueObject)
	{
		_runner = base.gameObject.GetComponent<InteractiveBookRunner>() ?? base.gameObject.AddComponent<InteractiveBookRunner>();
		_runner.OnWillReload -= HandleRunnerWillReload;
		_runner.OnWillReload += HandleRunnerWillReload;
		_keyValueObject = keyValueObject;
		RemoveAllArrows();
		Populate();
		_window.ShowWindow();
		ConfigureKeyValueObject();
		string basePath = Path.Combine(Application.streamingAssetsPath, directoryName);
		if (!_runner.Open(basePath, bookName, this, _keyValueObject))
		{
			ModalAlertController.PresentOkay("Error opening " + bookName, "Please submit a bug report and include your log file. Thank you!");
			return;
		}
		_window.Title = _runner.BookTitle ?? "Book";
		if (_rebuildCoroutine != null)
		{
			StopCoroutine(_rebuildCoroutine);
			_rebuildCoroutine = null;
		}
	}

	private void ConfigureKeyValueObject()
	{
		_completeObserver?.Dispose();
		_completeObserver = null;
		_completeObserver = _keyValueObject.Observe("complete", delegate(Value value)
		{
			if ((bool)value)
			{
				_window.CloseWindow();
			}
		}, callInitial: false);
	}

	public void RequestReload()
	{
		_runner.Reload();
	}

	private void HandleRunnerWillReload()
	{
		RemoveAllArrows();
	}

	private void RemoveAllArrows()
	{
		ArrowOverlayController.Shared.RemoveArrows(_arrowOverlayIds, animated: true);
		_arrowOverlayIds.Clear();
	}

	private void Awake()
	{
		_window = GetComponent<Window>();
		_window.DelegateRequestClose = HandleRequestCloseWindow;
	}

	private void OnDisable()
	{
		StopRefreshLoop();
		_panel?.Dispose();
		_panel = null;
	}

	private void Populate()
	{
		_window.Title = "Interactive Book";
		_panel?.Dispose();
		_panel = UIPanel.Create(_window.contentRectTransform, BuilderAssets, Build);
		_window.OnShownDidChange += HandleShownDidChange;
	}

	private void HandleShownDidChange(bool shown)
	{
		StopRefreshLoop();
		if (shown)
		{
			_refreshCoroutine = StartCoroutine(RefreshLoop());
		}
	}

	private void StopRefreshLoop()
	{
		if (_refreshCoroutine != null)
		{
			StopCoroutine(_refreshCoroutine);
		}
		_refreshCoroutine = null;
	}

	private void HandleRequestCloseWindow()
	{
		if (_pendingCloseRequest)
		{
			return;
		}
		_pendingCloseRequest = true;
		ModalAlertController.Present("Close " + _runner.BookTitle + "?", _runner.CloseMessage, new List<(int, string)>
		{
			(0, "Cancel"),
			(1, "Close")
		}, delegate(int choice)
		{
			_pendingCloseRequest = false;
			if (choice != 0 && choice == 1)
			{
				_runner.Close();
				OnPlayerClosed?.Invoke();
				RemoveAllArrows();
				_window.CloseWindow();
			}
		});
	}

	private IEnumerator RefreshLoop()
	{
		WaitForSecondsRealtime wait = new WaitForSecondsRealtime(1f);
		while (true)
		{
			if (_runner.ReloadIfModified())
			{
				Reload();
			}
			yield return wait;
		}
	}

	private void Build(UIPanelBuilder builder)
	{
		builder.Spacing = 4f;
		_scrollViewRectTransform = builder.VScrollView(delegate(UIPanelBuilder value)
		{
			value.Spacing = 16f;
			_scrollViewBuilder = value;
			foreach (object element in _elements)
			{
				if (!(element is ElementSay elementSay))
				{
					ElementButton elementButton = element as ElementButton;
					if (elementButton == null)
					{
						if (element is ElementGoal elementGoal)
						{
							string value2 = PrepareStringForDisplay(elementGoal.Title);
							bool flag = elementGoal.Style switch
							{
								GoalStyle.Percent => elementGoal.Value > 0.999f, 
								GoalStyle.Boolean => elementGoal.Value > 0.5f, 
								_ => throw new ArgumentOutOfRangeException(), 
							};
							StringBuilder stringBuilder = new StringBuilder();
							stringBuilder.Append(flag ? "<style=\"Goal_Complete\">" : "<style=\"Goal_Open\">");
							string value3 = elementGoal.Style switch
							{
								GoalStyle.Percent => $"{Mathf.RoundToInt(elementGoal.Value * 100f)}%", 
								GoalStyle.Boolean => "", 
								_ => throw new ArgumentOutOfRangeException(), 
							};
							if (!string.IsNullOrEmpty(elementGoal.CustomDisplay))
							{
								value3 = elementGoal.CustomDisplay;
							}
							stringBuilder.Append(value2);
							if (!string.IsNullOrEmpty(value3))
							{
								stringBuilder.Append(" ");
								stringBuilder.Append(value3);
							}
							if (!string.IsNullOrEmpty(elementGoal.Message))
							{
								string text = PrepareStringForDisplay(elementGoal.Message);
								stringBuilder.Append("\n" + text);
							}
							stringBuilder.Append("</style>");
							value.AddLabel(stringBuilder.ToString());
						}
						else
						{
							Log.Warning("Unexpected element: {e}", element);
						}
					}
					else
					{
						value.AddButton(elementButton.Text, delegate
						{
							InteractiveBookRunner.TryRun(elementButton.Closure, elementButton.Text);
						});
					}
				}
				else
				{
					value.AddLabel(PrepareStringForDisplay(elementSay.Text));
				}
			}
			value.AddExpandingVerticalSpacer();
		}, new RectOffset(0, 8, 0, 0));
		builder.AddHRule();
		builder.HStack(delegate(UIPanelBuilder value)
		{
			_bottomBarBuilder = value;
			if (_showReloadButton)
			{
				value.AddButton("Reload", delegate
				{
					_runner.Reload();
					Reload();
				});
			}
			value.Spacer();
			foreach (ElementButton button in _navButtons)
			{
				value.AddButton(button.Text, delegate
				{
					InteractiveBookRunner.TryRun(button.Closure, button.Text);
				});
			}
		}).Height(38f).GetComponent<LayoutGroup>()
			.padding = new RectOffset(4, 4, 4, 4);
	}

	private static string PrepareStringForDisplay(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return s;
		}
		return s.RemovingLeadingWhitespaceFromLines().ToTMPMarkup();
	}

	private void Reload()
	{
		UIPanelBuilder? scrollViewBuilder = _scrollViewBuilder;
		if (scrollViewBuilder.HasValue)
		{
			scrollViewBuilder.GetValueOrDefault().Rebuild();
		}
		else
		{
			_panel.Rebuild();
		}
	}

	private void AddElement(object element)
	{
		_elements.Add(element);
		ScrollRect scrollRect;
		float contentHeight;
		if (_rebuildCoroutine == null)
		{
			scrollRect = _scrollViewRectTransform.GetComponentInChildren<ScrollRect>();
			contentHeight = scrollRect.content.sizeDelta.y;
			_rebuildCoroutine = StartCoroutine(RebuildBody());
		}
		IEnumerator RebuildBody()
		{
			yield return new WaitForEndOfFrame();
			_scrollViewBuilder?.Rebuild();
			_rebuildCoroutine = null;
			yield return new WaitForEndOfFrame();
			if (scrollRect != null)
			{
				SmoothScrollToNewContent(scrollRect, contentHeight);
			}
		}
	}

	private void SmoothScrollToNewContent(ScrollRect scrollRect, float contentHeightBefore, float scrollSpeed = 200f)
	{
		RectTransform content = scrollRect.content;
		float height = scrollRect.viewport.rect.height;
		float y = content.sizeDelta.y;
		float num = (1f - scrollRect.verticalNormalizedPosition) * (y - height);
		float num2 = Mathf.Clamp(contentHeightBefore, 0f, y - height);
		float duration = Mathf.Abs(num2 - num) / scrollSpeed;
		float targetPosition = 1f;
		if (y > height)
		{
			targetPosition = 1f - num2 / (y - height);
		}
		scrollRect.StopAllCoroutines();
		scrollRect.StartCoroutine(scrollRect.ScrollAnimated(targetPosition, duration));
	}

	public void say(string text)
	{
		AddElement(new ElementSay
		{
			Text = text
		});
	}

	public void clear()
	{
		_elements.Clear();
		_navButtons.Clear();
		_showReloadButton = false;
		_scrollViewBuilder?.Rebuild();
		_bottomBarBuilder?.Rebuild();
	}

	public int start_goal(string title, string message, string style)
	{
		GoalStyle style2 = ((!(style == "percent")) ? GoalStyle.Boolean : GoalStyle.Percent);
		Log.Information("InteractiveBookWindow: start_goal: {title}, {message}", title, message);
		AddElement(new ElementGoal
		{
			Title = title,
			Message = message,
			Value = 0f,
			Style = style2
		});
		return _elements.Count((object e) => e is ElementGoal) - 1;
	}

	public void update_goal(int goalId, object objectValue, string customDisplay)
	{
		float num4;
		if (!(objectValue is float num))
		{
			if (!(objectValue is double num2))
			{
				if (!(objectValue is int num3))
				{
					if (!(objectValue is bool))
					{
						throw new ArgumentOutOfRangeException("Unexpected value type: " + objectValue.GetType().Name);
					}
					num4 = (((bool)objectValue) ? 1f : 0f);
				}
				else
				{
					num4 = num3;
				}
			}
			else
			{
				num4 = (float)num2;
			}
		}
		else
		{
			num4 = num;
		}
		float value = num4;
		value = Mathf.Clamp01(value);
		int num5 = 0;
		for (int i = 0; i < _elements.Count; i++)
		{
			if (!(_elements[i] is ElementGoal elementGoal))
			{
				continue;
			}
			if (num5 == goalId)
			{
				float num6 = Mathf.Abs(elementGoal.Value - value);
				if ((value == 1f && elementGoal.Value < 1f) || !(num6 < 0.01f) || !(elementGoal.CustomDisplay == customDisplay))
				{
					elementGoal.Value = value;
					elementGoal.CustomDisplay = customDisplay;
					_elements[i] = elementGoal;
					_scrollViewBuilder?.Rebuild();
				}
				break;
			}
			num5++;
		}
	}

	public void finish_goal(int goalId)
	{
		Log.Debug("InteractiveBookWindow: finish_goal: {goalId}", goalId);
		update_goal(goalId, 1f, null);
	}

	public void reload_button()
	{
		_showReloadButton = true;
		_bottomBarBuilder?.Rebuild();
	}

	public void button(string text, Closure closure)
	{
		AddElement(new ElementButton
		{
			Text = text,
			Closure = closure
		});
	}

	public void nav_button(string text, Closure closure)
	{
		_navButtons.Add(new ElementButton
		{
			Text = text,
			Closure = closure
		});
		_bottomBarBuilder?.Rebuild();
	}

	public void remove_last()
	{
		_elements.RemoveAt(_elements.Count - 1);
		_scrollViewBuilder?.Rebuild();
	}

	public int add_arrow_overlay(object o, string hexColor)
	{
		Vector3 position2;
		Quaternion rotation2;
		if (o is ScriptLocation scriptLocation)
		{
			Graph.PositionRotation positionRotation = Graph.Shared.GetPositionRotation(scriptLocation.Location);
			Vector3 position = positionRotation.Position;
			Quaternion rotation = positionRotation.Rotation;
			position2 = position;
			rotation2 = rotation;
		}
		else
		{
			if (!(o is Table v))
			{
				throw new ScriptRuntimeException("Arrow locator should be Location or Vector3 table");
			}
			position2 = ScriptVector3.FromTable(v);
			rotation2 = Quaternion.identity;
		}
		Color color = ColorHelper.ColorFromHex(hexColor) ?? Color.black;
		int num = ArrowOverlayController.Shared.AddArrow(position2, rotation2, color, 1f, animated: true, dancing: true);
		_arrowOverlayIds.Add(num);
		return num;
	}

	public void remove_arrow_overlay(int arrowId)
	{
		_arrowOverlayIds.Remove(arrowId);
		ArrowOverlayController.Shared.RemoveArrow(arrowId, animated: true);
	}
}
