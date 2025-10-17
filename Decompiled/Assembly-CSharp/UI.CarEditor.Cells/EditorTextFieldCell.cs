using System;
using TMPro;
using UnityEngine;

namespace UI.CarEditor.Cells;

public class EditorTextFieldCell : EditorCellBase
{
	[SerializeField]
	private TMP_InputField inputField;

	private Action<string> _action;

	private string _value;

	private void Awake()
	{
		inputField.onValueChanged.AddListener(delegate(string value)
		{
			if (!string.Equals(_value, value))
			{
				_value = value;
				_action?.Invoke(value);
			}
		});
	}

	public void Configure(string labelText, string value, bool editable, Action<string> didSet)
	{
		_value = value;
		_action = didSet;
		label.text = labelText;
		inputField.text = _value;
		inputField.interactable = editable;
	}

	public void Configure(string labelText, int value, bool editable, Action<int> didSet)
	{
		Configure(labelText, value.ToString(), editable, delegate(string str)
		{
			if (int.TryParse(str, out var result))
			{
				didSet(result);
			}
		});
	}
}
