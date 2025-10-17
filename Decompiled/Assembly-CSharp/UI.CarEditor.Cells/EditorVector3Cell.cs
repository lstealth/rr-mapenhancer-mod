using System;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace UI.CarEditor.Cells;

public class EditorVector3Cell : EditorCellBase
{
	private Vector3 _value;

	[SerializeField]
	private TMP_InputField inputFieldX;

	[SerializeField]
	private TMP_InputField inputFieldY;

	[SerializeField]
	private TMP_InputField inputFieldZ;

	[SerializeField]
	private SliderArea sliderAreaX;

	[SerializeField]
	private SliderArea sliderAreaY;

	[SerializeField]
	private SliderArea sliderAreaZ;

	private Action<Vector3> _action;

	private void Awake()
	{
		inputFieldX.onValueChanged.AddListener(delegate(string value)
		{
			if (float.TryParse(value, out var result))
			{
				_value.x = result;
				FireAction();
			}
		});
		inputFieldY.onValueChanged.AddListener(delegate(string value)
		{
			if (float.TryParse(value, out var result))
			{
				_value.y = result;
				FireAction();
			}
		});
		inputFieldZ.onValueChanged.AddListener(delegate(string value)
		{
			if (float.TryParse(value, out var result))
			{
				_value.z = result;
				FireAction();
			}
		});
		SliderArea sliderArea = sliderAreaX;
		sliderArea.onValueChanged = (Action<float>)Delegate.Combine(sliderArea.onValueChanged, (Action<float>)delegate(float d)
		{
			_value.x += d;
			FireAction();
			UpdateFields();
		});
		SliderArea sliderArea2 = sliderAreaY;
		sliderArea2.onValueChanged = (Action<float>)Delegate.Combine(sliderArea2.onValueChanged, (Action<float>)delegate(float d)
		{
			_value.y += d;
			FireAction();
			UpdateFields();
		});
		SliderArea sliderArea3 = sliderAreaZ;
		sliderArea3.onValueChanged = (Action<float>)Delegate.Combine(sliderArea3.onValueChanged, (Action<float>)delegate(float d)
		{
			_value.z += d;
			FireAction();
			UpdateFields();
		});
	}

	private void FireAction()
	{
		_action?.Invoke(_value);
	}

	public void Configure(string labelText, Vector3 vector, Action<Vector3> didSet)
	{
		_value = vector;
		label.text = labelText;
		UpdateFields();
		_action = didSet;
	}

	public void Configure(string labelText, Vector2 vector, Action<Vector2> didSet)
	{
		_value = vector;
		label.text = labelText;
		UpdateFields();
		inputFieldZ.interactable = false;
		_action = delegate(Vector3 vector2)
		{
			didSet(vector2);
		};
	}

	private void UpdateFields()
	{
		inputFieldX.text = _value.x.ToString(CultureInfo.InvariantCulture);
		inputFieldY.text = _value.y.ToString(CultureInfo.InvariantCulture);
		inputFieldZ.text = _value.z.ToString(CultureInfo.InvariantCulture);
	}
}
