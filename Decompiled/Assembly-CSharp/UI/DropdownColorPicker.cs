using System;
using System.Collections;
using Helpers;
using Serilog;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI;

public class DropdownColorPicker : DropdownPickerBase
{
	[SerializeField]
	private ColorPicker colorPicker;

	[SerializeField]
	private new Image image;

	[SerializeField]
	private TMP_InputField inputField;

	[SerializeField]
	private TMP_Text defaultLabel;

	private Action<string> _onApply;

	private Coroutine _delayedAssign;

	public void Configure(string hexColor, Action<string> onApply)
	{
		inputField.text = hexColor;
		inputField.onEndEdit.AddListener(UpdatePickerColor);
		UpdatePickerColor(inputField.text);
		_onApply = onApply;
		colorPicker.onColorChanged += delegate(Color color)
		{
			Debug.Log($"onColorChanged {color}");
			if (_delayedAssign != null)
			{
				StopCoroutine(_delayedAssign);
			}
			_delayedAssign = StartCoroutine(DelayedPickerToString(color, 0.2f));
		};
	}

	private void UpdatePickerColor(string hexColor)
	{
		Debug.Log("UpdatePickerColor: " + hexColor);
		bool flag = string.IsNullOrEmpty(hexColor);
		if (!flag)
		{
			Color color = ColorHelper.ColorFromHex(hexColor) ?? Color.black;
			colorPicker.SetColor(color, notify: false);
			image.color = color;
		}
		else
		{
			colorPicker.SetColor(Color.gray, notify: false);
			image.color = Color.clear;
		}
		defaultLabel.gameObject.SetActive(flag);
	}

	private void Notify(string hexString)
	{
		_onApply?.Invoke(hexString);
	}

	private IEnumerator DelayedPickerToString(Color color, float delay)
	{
		yield return new WaitForSecondsRealtime(delay);
		try
		{
			string text = ColorHelper.HexFromColor(color);
			Debug.Log($"PickedToString {color} -> {text}");
			inputField.SetTextWithoutNotify(text);
			image.color = color;
			Notify(text);
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Exception while applying color");
		}
		_delayedAssign = null;
	}

	public void Apply()
	{
		string text = inputField.text;
		UpdatePickerColor(text);
		Notify(text);
		Hide();
	}

	public void ApplyDefault()
	{
		inputField.text = null;
		UpdatePickerColor(null);
		Notify(null);
		Hide();
	}
}
