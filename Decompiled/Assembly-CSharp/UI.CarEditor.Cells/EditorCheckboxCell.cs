using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI.CarEditor.Cells;

public class EditorCheckboxCell : EditorCellBase
{
	[SerializeField]
	private Toggle toggle;

	private Action<bool> _action;

	private bool _value;

	private void Awake()
	{
		toggle.onValueChanged.AddListener(delegate(bool value)
		{
			if (_value != value)
			{
				_value = value;
				_action?.Invoke(value);
			}
		});
	}

	public void Configure(string labelText, bool value, Action<bool> didSet)
	{
		_value = value;
		_action = didSet;
		label.text = labelText;
		toggle.isOn = _value;
	}
}
