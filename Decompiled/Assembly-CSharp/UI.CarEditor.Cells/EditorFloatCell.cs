using System;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace UI.CarEditor.Cells;

public class EditorFloatCell : EditorCellBase
{
	[SerializeField]
	private TMP_InputField inputField;

	[SerializeField]
	private SliderArea sliderArea;

	private Action<float> _action;

	private float _value;

	private void Awake()
	{
		inputField.onValueChanged.AddListener(delegate(string value)
		{
			if (float.TryParse(value, out var result) && _value != result)
			{
				_value = result;
				FireAction();
			}
		});
		SliderArea obj = sliderArea;
		obj.onValueChanged = (Action<float>)Delegate.Combine(obj.onValueChanged, (Action<float>)delegate(float delta)
		{
			_value += delta;
			UpdateFieldFromValue();
			FireAction();
		});
	}

	public void Configure(string labelText, float value, Action<float> didSet)
	{
		_value = value;
		label.text = labelText;
		_action = didSet;
		UpdateFieldFromValue();
	}

	private void UpdateFieldFromValue()
	{
		inputField.text = _value.ToString(CultureInfo.CurrentCulture);
	}

	private void FireAction()
	{
		_action?.Invoke(_value);
	}
}
