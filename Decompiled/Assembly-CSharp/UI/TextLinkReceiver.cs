using System;
using Helpers;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public class TextLinkReceiver : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
{
	private TMP_Text _text;

	public Action<string> OnLinkClicked;

	private void Awake()
	{
		_text = GetComponent<TMP_Text>();
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		int num = TMP_TextUtilities.FindIntersectingLink(position: new Vector3(eventData.position.x, eventData.position.y, 0f), text: _text, camera: null);
		if (num != -1)
		{
			TMP_LinkInfo tMP_LinkInfo = _text.textInfo.linkInfo[num];
			string link = tMP_LinkInfo.GetLink();
			if (OnLinkClicked != null)
			{
				OnLinkClicked?.Invoke(link);
			}
			else
			{
				LinkDispatcher.Open(link);
			}
		}
	}
}
