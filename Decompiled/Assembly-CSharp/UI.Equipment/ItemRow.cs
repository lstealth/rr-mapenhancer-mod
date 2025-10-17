using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Equipment;

public class ItemRow : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
{
	public Image selectedImage;

	public TMP_Text titleLabel;

	public TMP_Text subtitleLabel;

	public Action OnClick;

	public void OnPointerClick(PointerEventData eventData)
	{
		Debug.Log("OnPointerClick");
		OnClick?.Invoke();
	}
}
