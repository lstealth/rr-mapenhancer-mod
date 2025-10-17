using System;
using TMPro;
using UnityEngine;

namespace UI.SwitchList;

public class SwitchListRow : MonoBehaviour
{
	public TMP_Text carType;

	public TMP_Text carName;

	public TMP_Text destination;

	public TMP_Text location;

	public RectTransform strikethrough;

	public RectTransform removeButton;

	public Action OnRemoveClicked;

	public void RemoveButtonClicked()
	{
		OnRemoveClicked?.Invoke();
	}
}
