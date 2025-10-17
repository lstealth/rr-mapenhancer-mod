using System;
using System.Collections.Generic;
using Serilog;
using TMPro;
using UI.Builder;
using UnityEngine;

namespace UI.Common;

public class ModalAlertController : MonoBehaviour
{
	[SerializeField]
	private ModalAlert alertPrefab;

	[SerializeField]
	private Canvas canvas;

	private string _inputString;

	public static ModalAlertController Shared { get; private set; }

	private void Awake()
	{
		Shared = this;
	}

	public static void Present<T>(string title, string message, IEnumerable<(T, string)> buttons, Action<T> onButton)
	{
		if (Shared == null)
		{
			throw new Exception("No ModalAlertController");
		}
		Shared._Run(title, message, null, buttons, delegate((T, string) tuple)
		{
			onButton(tuple.Item1);
		});
	}

	public static void Present<T>(string title, string message, string inputString, IEnumerable<(T, string)> buttons, Action<(T, string)> onButton)
	{
		if (Shared == null)
		{
			throw new Exception("No ModalAlertController");
		}
		Shared._Run(title, message, inputString, buttons, onButton);
	}

	private void _Run<T>(string title, string message, string inputString, IEnumerable<(T, string)> buttons, Action<(T, string)> onButton)
	{
		_Run(delegate(UIPanelBuilder builder, Action dismiss)
		{
			builder.Spacing = 16f;
			builder.AddLabel(title, delegate(TMP_Text text)
			{
				text.fontSize = 22f;
				text.horizontalAlignment = HorizontalAlignmentOptions.Center;
			});
			if (!string.IsNullOrEmpty(message))
			{
				builder.AddLabel(message, delegate(TMP_Text text)
				{
					text.fontSize = 18f;
					text.horizontalAlignment = HorizontalAlignmentOptions.Center;
				});
			}
			if (inputString != null)
			{
				_inputString = inputString;
				builder.AddInputField(inputString, delegate(string str)
				{
					_inputString = str;
				});
			}
			builder.AlertButtons(delegate(UIPanelBuilder uIPanelBuilder)
			{
				foreach (var button in buttons)
				{
					var (buttonValue, buttonText) = button;
					uIPanelBuilder.AddButtonMedium(buttonText, delegate
					{
						try
						{
							onButton((buttonValue, _inputString));
						}
						catch (Exception exception)
						{
							Log.Error(exception, "Error in action for {button}", buttonText);
							return;
						}
						dismiss?.Invoke();
					});
				}
			});
		});
	}

	public static void PresentOkay(string title, string message, Action onOkay = null)
	{
		Present(title, message, new(int, string)[1] { (0, "Okay") }, delegate
		{
			onOkay?.Invoke();
		});
	}

	[UnityEngine.ContextMenu("Demo Alert")]
	private void PresentDemo()
	{
		string message = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.";
		Present("Lorem ipsum dolor sit amet, consectetur adipiscing elit", message, new(int, string)[2]
		{
			(1, "One"),
			(2, "Two")
		}, delegate(int value)
		{
			Toast.Present($"Alert result: {value}");
		});
	}

	public static void Present(Action<UIPanelBuilder, Action> builderDismissClosure, int width = 400)
	{
		if (Shared == null)
		{
			throw new Exception("No ModalAlertController");
		}
		Shared._Run(builderDismissClosure, width);
	}

	private void _Run(Action<UIPanelBuilder, Action> builderDismissClosure, int width = 400)
	{
		ModalAlert modalAlert = UnityEngine.Object.Instantiate(alertPrefab, canvas.transform);
		modalAlert.gameObject.SetActive(value: true);
		RectTransform component = modalAlert.GetComponent<RectTransform>();
		component.SetFrameFillParent();
		modalAlert.Configure(builderDismissClosure, width);
		TMP_InputField[] componentsInChildren = component.GetComponentsInChildren<TMP_InputField>();
		foreach (TMP_InputField tMP_InputField in componentsInChildren)
		{
			if (tMP_InputField.isActiveAndEnabled)
			{
				tMP_InputField.ActivateInputField();
				break;
			}
		}
	}
}
