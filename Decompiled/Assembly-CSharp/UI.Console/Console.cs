using System;
using System.Collections;
using Game;
using TMPro;
using UI.Common;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UI.Console;

[RequireComponent(typeof(ConsoleLinePool))]
public class Console : MonoBehaviour, IConsoleChildDelegate
{
	public struct Entry
	{
		public GameDateTime Timestamp;

		public string Text;
	}

	private static Console _instance;

	[SerializeField]
	private Window window;

	[SerializeField]
	private ExpandedConsole expanded;

	[SerializeField]
	private InputActionAsset inputActions;

	private CollapsedConsole _collapsed;

	private ConsoleLinePool _pool;

	private bool _isCollapsing;

	private float _lastCollapsed;

	private InputActionMap _consoleActionMap;

	public static Console shared
	{
		get
		{
			if ((bool)_instance)
			{
				return _instance;
			}
			_instance = UnityEngine.Object.FindObjectOfType<Console>();
			if (!_instance)
			{
				Debug.LogWarning("Couldn't find Console in scene.");
			}
			return _instance;
		}
	}

	public event Action<bool> OnFocusedChanged;

	public event Action<string> OnUserInput;

	private void Awake()
	{
		_pool = GetComponent<ConsoleLinePool>();
		_collapsed = GetComponentInChildren<CollapsedConsole>();
		_consoleActionMap = inputActions.FindActionMap("Console", throwIfNotFound: true);
		expanded.ConfigureInputActions(inputActions);
		_collapsed.Console = this;
		expanded.Console = this;
		window.OnShownWillChange += delegate(bool shown)
		{
			expanded.OnWindowShownWillChange(shown);
		};
		window.OnShownDidChange += delegate(bool shown)
		{
			this.OnFocusedChanged?.Invoke(shown);
		};
	}

	private void Start()
	{
		Collapse();
	}

	private void Update()
	{
		if (GameInput.shared.InputToggleConsole)
		{
			if (window.IsShown)
			{
				expanded.Focus();
				window.OrderFront();
			}
			else if (!window.IsShown && _lastCollapsed + 0.1f < Time.realtimeSinceStartup)
			{
				Expand();
			}
		}
	}

	public void AddLine(string text)
	{
		AddLine(text, TimeWeather.Now);
	}

	public void AddLine(string text, GameDateTime gameDateTime)
	{
		if (expanded == null || _collapsed == null)
		{
			Debug.LogWarning("Can't add line - not Awake yet: " + text);
			return;
		}
		Entry entry = new Entry
		{
			Timestamp = gameDateTime,
			Text = text
		};
		expanded.Add(entry);
		_collapsed.Add(entry);
	}

	public void Toggle()
	{
		if (window.IsShown)
		{
			Collapse();
		}
		else
		{
			Expand();
		}
	}

	public void Expand()
	{
		_collapsed.WillDisable();
		_collapsed.gameObject.SetActive(value: false);
		window.ShowWindow();
		_consoleActionMap.Enable();
		this.OnFocusedChanged?.Invoke(obj: true);
	}

	public void Collapse()
	{
		if (_isCollapsing)
		{
			return;
		}
		try
		{
			_isCollapsing = true;
			_lastCollapsed = Time.realtimeSinceStartup;
			_consoleActionMap.Disable();
			window.CloseWindow();
			_collapsed.gameObject.SetActive(value: true);
			StartCoroutine(SetFocusedDelayed(focused: false));
		}
		finally
		{
			_isCollapsing = false;
		}
	}

	public void CreateLabelIfNeeded(ConsoleLine line, Transform parent)
	{
		if (!(line.Label != null))
		{
			line.Label = CreateLine(line.Text, parent);
		}
	}

	public TMP_Text CreateLine(string text, Transform parent)
	{
		return _pool.CreateLine(text, parent);
	}

	public void Recycle(TMP_Text text)
	{
		_pool.Recycle(text);
	}

	public void InputFieldFocusDidChange(bool focused)
	{
		StartCoroutine(SetFocusedDelayed(focused));
	}

	private IEnumerator SetFocusedDelayed(bool focused)
	{
		yield return new WaitForEndOfFrame();
		this.OnFocusedChanged?.Invoke(focused);
	}

	public void HandleUserInput(string line)
	{
		try
		{
			this.OnUserInput?.Invoke(line);
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
	}
}
