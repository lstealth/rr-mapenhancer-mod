using System;
using System.Collections;
using System.Collections.Generic;
using Game;
using TMPro;
using UI.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UI.Console;

public class ExpandedConsole : MonoBehaviour
{
	[NonSerialized]
	public IConsoleChildDelegate Console;

	private Canvas _canvas;

	private RectTransform _rect;

	private List<Console.Entry> _pendingEntries = new List<Console.Entry>();

	private readonly List<string> _history = new List<string>();

	private int _historyOffset;

	private const int MaxHistory = 10;

	private int _lineCount;

	[SerializeField]
	private TMP_InputField inputField;

	[SerializeField]
	private TMP_Text textArea;

	[SerializeField]
	private ScrollRect scrollRect;

	private Window _window;

	private InputAction _actionHistoryUp;

	private InputAction _actionHistoryDown;

	private Coroutine _scrollCoroutine;

	private const int LineCountLimit = 100;

	protected void Awake()
	{
		_window = GetComponentInParent<Window>();
		_window.OnShownDidChange += delegate(bool shown)
		{
			if (!shown)
			{
				DismissDebounced();
			}
		};
		_canvas = base.transform.GetComponentInParent<Canvas>();
		_rect = GetComponent<RectTransform>();
		textArea.text = "";
		inputField.characterLimit = 512;
		inputField.onSelect.RemoveListener(OnSelect);
		inputField.onSelect.AddListener(OnSelect);
		inputField.onDeselect.RemoveListener(OnDeselect);
		inputField.onDeselect.AddListener(OnDeselect);
		inputField.onSubmit.RemoveListener(OnInputSubmit);
		inputField.onSubmit.AddListener(OnInputSubmit);
		inputField.onValueChanged.RemoveListener(OnInputChanged);
		inputField.onValueChanged.AddListener(OnInputChanged);
	}

	private void OnEnable()
	{
		if (_actionHistoryUp != null)
		{
			_actionHistoryUp.performed += HistoryUpPerformed;
			_actionHistoryDown.performed += HistoryDownPerformed;
		}
		StartCoroutine(DidEnable());
	}

	private void OnDisable()
	{
		_actionHistoryUp.performed -= HistoryUpPerformed;
		_actionHistoryDown.performed -= HistoryDownPerformed;
		DeselectInputField();
	}

	private void Update()
	{
		ProcessPendingEntries();
	}

	private IEnumerator DidEnable()
	{
		yield return null;
		ScrollToBottom();
		Focus();
	}

	public void ConfigureInputActions(InputActionAsset inputActions)
	{
		_actionHistoryUp = inputActions["Console/HistoryUp"];
		_actionHistoryDown = inputActions["Console/HistoryDown"];
	}

	private void DismissDebounced()
	{
		Console.Collapse();
	}

	public void Focus()
	{
		inputField.ActivateInputField();
	}

	private void OnSelect(string arg0)
	{
		Console.InputFieldFocusDidChange(focused: true);
	}

	private void OnDeselect(string arg0)
	{
		Console.InputFieldFocusDidChange(focused: false);
	}

	private void OnInputSubmit(string text)
	{
		_historyOffset = 0;
		if (_history.Count == 0 || _history[0] != text)
		{
			_history.Insert(0, text);
		}
		while (_history.Count > 10)
		{
			_history.RemoveAt(_history.Count - 1);
		}
		ScrollToBottom();
		Console.HandleUserInput(text);
		inputField.text = "";
		inputField.ActivateInputField();
	}

	private void OnInputChanged(string text)
	{
		if (text == "`")
		{
			inputField.text = "";
			DeselectInputField();
			DismissDebounced();
		}
		if (text == "\u001b")
		{
			inputField.text = "";
			DeselectInputField();
		}
	}

	private void DeselectInputField()
	{
		EventSystem current = EventSystem.current;
		if (!(current == null) && !current.alreadySelecting)
		{
			current.SetSelectedGameObject(null);
		}
	}

	private void HistoryUpPerformed(InputAction.CallbackContext obj)
	{
		if (_historyOffset < _history.Count)
		{
			StartCoroutine(MoveHistory(1));
		}
	}

	private void HistoryDownPerformed(InputAction.CallbackContext obj)
	{
		if (_historyOffset > 0)
		{
			StartCoroutine(MoveHistory(-1));
		}
	}

	private IEnumerator MoveHistory(int offset)
	{
		_historyOffset += offset;
		inputField.text = ((_historyOffset == 0) ? "" : _history[_historyOffset - 1]);
		yield return null;
		inputField.MoveToEndOfLine(shift: false, ctrl: true);
		inputField.ActivateInputField();
	}

	public void OnWindowShownWillChange(bool shown)
	{
		if (!shown)
		{
			DeselectInputField();
		}
	}

	public void Add(Console.Entry entry)
	{
		_pendingEntries.Add(entry);
	}

	private void ProcessPendingEntries()
	{
		if (_pendingEntries.Count == 0)
		{
			return;
		}
		bool flag = scrollRect.verticalNormalizedPosition < 1E-05f;
		string text = textArea.text;
		foreach (Console.Entry pendingEntry in _pendingEntries)
		{
			_lineCount++;
			if (_lineCount > 100)
			{
				int num = 0;
				while (_lineCount > 100)
				{
					int num2 = text.IndexOf('\n', num);
					if (num2 == -1)
					{
						break;
					}
					num = num2 + 1;
					_lineCount--;
				}
				text = text.Remove(0, num);
			}
			string[] obj = new string[6] { text, "<style=ConsoleTime>", null, null, null, null };
			GameDateTime timestamp = pendingEntry.Timestamp;
			obj[2] = timestamp.ConsoleTimeString();
			obj[3] = "</style><style=ConsoleIndent><style=ConsoleText>";
			obj[4] = pendingEntry.Text;
			obj[5] = "</style></style>\n";
			text = string.Concat(obj);
		}
		_pendingEntries.Clear();
		textArea.text = text;
		if (flag)
		{
			ScrollToBottom();
		}
	}

	private void ScrollToBottom()
	{
		LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)scrollRect.transform);
		scrollRect.verticalNormalizedPosition = 0f;
	}
}
