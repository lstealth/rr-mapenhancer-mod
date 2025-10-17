using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Placer;

public class LibraryRow : MonoBehaviour, IPointerDownHandler, IEventSystemHandler
{
	public Action OnDefaultAction;

	public Button defaultActionButton;

	public Image image;

	public TMP_Text titleLabel;

	public TMP_Text subtitleLabel;

	private float _lastClick;

	public void OnPointerDown(PointerEventData eventData)
	{
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		if (realtimeSinceStartup - _lastClick < 0.5f)
		{
			PerformDefaultAction();
			_lastClick = 0f;
		}
		else
		{
			_lastClick = realtimeSinceStartup;
		}
	}

	public void PerformDefaultAction()
	{
		OnDefaultAction?.Invoke();
	}
}
