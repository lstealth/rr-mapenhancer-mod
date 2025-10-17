using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UI.Console;

public class ConsoleLinePool : MonoBehaviour
{
	[SerializeField]
	private TMP_Text linePrefab;

	private CanvasGroup _poolCanvasGroup;

	private readonly List<TMP_Text> _pool = new List<TMP_Text>();

	private void Awake()
	{
		GameObject gameObject = new GameObject
		{
			name = "Pool",
			hideFlags = HideFlags.DontSave
		};
		gameObject.transform.SetParent(base.transform);
		_poolCanvasGroup = gameObject.AddComponent<CanvasGroup>();
		_poolCanvasGroup.alpha = 0f;
		_poolCanvasGroup.interactable = false;
		_poolCanvasGroup.blocksRaycasts = false;
	}

	public TMP_Text CreateLine(string str, Transform parent)
	{
		TMP_Text tMP_Text;
		if (_pool.Count > 0)
		{
			tMP_Text = _pool[0];
			_pool.RemoveAt(0);
			tMP_Text.transform.SetParent(parent);
		}
		else
		{
			tMP_Text = Object.Instantiate(linePrefab, parent);
			tMP_Text.hideFlags = HideFlags.DontSave;
			tMP_Text.name = "Line";
			tMP_Text.text = str;
		}
		tMP_Text.text = str;
		RectTransform component = tMP_Text.GetComponent<RectTransform>();
		component.anchorMin = new Vector2(0f, 0f);
		component.anchorMax = new Vector2(1f, 1f);
		component.pivot = new Vector2(0f, 1f);
		component.offsetMin = new Vector2(0f, 0f);
		component.offsetMax = new Vector2(0f, 0f);
		tMP_Text.ForceMeshUpdate();
		return tMP_Text;
	}

	public void Recycle(TMP_Text text)
	{
		if (!(text == null))
		{
			if (_pool.Count > 10)
			{
				Object.Destroy(text.gameObject);
				return;
			}
			text.transform.SetParent(_poolCanvasGroup.transform);
			_pool.Add(text);
		}
	}
}
