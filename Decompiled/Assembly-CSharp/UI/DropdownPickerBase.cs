using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace UI;

public class DropdownPickerBase : Selectable, IPointerClickHandler, IEventSystemHandler, ISubmitHandler, ICancelHandler
{
	[Tooltip("The content window which opens when activating the dropdown.")]
	[SerializeField]
	protected RectTransform dropdown;

	private GameObject _blocker;

	private bool _hasSetupTemplate;

	private const int HighSortingLayer = 30000;

	private const float AlphaFadeSpeed = 0.15f;

	private GameObject DropdownGameObject => dropdown.gameObject;

	protected override void Start()
	{
		base.Start();
		RefreshShownValue();
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		dropdown.gameObject.SetActive(value: false);
	}

	protected override void OnDisable()
	{
		ImmediateDestroyDropdownList();
		if (_blocker != null)
		{
			DestroyBlocker(_blocker);
		}
		_blocker = null;
		base.OnDisable();
	}

	public void RefreshShownValue()
	{
	}

	private void SetupTemplate(Canvas rootCanvas)
	{
		if (_hasSetupTemplate)
		{
			return;
		}
		_hasSetupTemplate = true;
		GameObject gameObject = dropdown.gameObject;
		gameObject.SetActive(value: true);
		Canvas canvas = null;
		Transform parent = dropdown.parent;
		while (parent != null)
		{
			canvas = parent.GetComponent<Canvas>();
			if (canvas != null)
			{
				break;
			}
			parent = parent.parent;
		}
		if (!gameObject.TryGetComponent<Canvas>(out var _))
		{
			Canvas canvas2 = gameObject.AddComponent<Canvas>();
			canvas2.overrideSorting = true;
			canvas2.sortingOrder = 30000;
			canvas2.sortingLayerID = rootCanvas.sortingLayerID;
		}
		if (canvas != null)
		{
			Component[] components = canvas.GetComponents<BaseRaycaster>();
			components = components;
			for (int i = 0; i < components.Length; i++)
			{
				Type type = components[i].GetType();
				if (gameObject.GetComponent(type) == null)
				{
					gameObject.AddComponent(type);
				}
			}
		}
		else
		{
			GetOrAddComponent<GraphicRaycaster>(gameObject);
		}
		GetOrAddComponent<CanvasGroup>(gameObject);
		gameObject.SetActive(value: false);
	}

	private static T GetOrAddComponent<T>(GameObject go) where T : Component
	{
		T val = go.GetComponent<T>();
		if (!val)
		{
			val = go.AddComponent<T>();
		}
		return val;
	}

	public virtual void OnPointerClick(PointerEventData eventData)
	{
		Show();
	}

	public virtual void OnSubmit(BaseEventData eventData)
	{
		Show();
	}

	public virtual void OnCancel(BaseEventData eventData)
	{
		Hide();
	}

	public void Show()
	{
		if (!IsActive() || !IsInteractable() || dropdown.gameObject.activeSelf)
		{
			return;
		}
		List<Canvas> list = CollectionPool<List<Canvas>, Canvas>.Get();
		base.gameObject.GetComponentsInParent(includeInactive: false, list);
		if (list.Count == 0)
		{
			return;
		}
		int count = list.Count;
		Canvas canvas = list[count - 1];
		for (int i = 0; i < count; i++)
		{
			if (list[i].isRootCanvas || list[i].overrideSorting)
			{
				canvas = list[i];
				break;
			}
		}
		CollectionPool<List<Canvas>, Canvas>.Release(list);
		SetupTemplate(canvas);
		dropdown.gameObject.SetActive(value: true);
		Vector3[] array = new Vector3[4];
		dropdown.GetWorldCorners(array);
		RectTransform rectTransform = canvas.transform as RectTransform;
		Rect rect = rectTransform.rect;
		for (int j = 0; j < 2; j++)
		{
			bool flag = false;
			for (int k = 0; k < 4; k++)
			{
				Vector3 vector = rectTransform.InverseTransformPoint(array[k]);
				if ((vector[j] < rect.min[j] && !Mathf.Approximately(vector[j], rect.min[j])) || (vector[j] > rect.max[j] && !Mathf.Approximately(vector[j], rect.max[j])))
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				RectTransformUtility.FlipLayoutOnAxis(dropdown, j, keepPositioning: false, recursive: false);
			}
		}
		AlphaFadeList(0.15f, 0f, 1f);
		_blocker = CreateBlocker(canvas);
	}

	protected virtual GameObject CreateBlocker(Canvas rootCanvas)
	{
		GameObject gameObject = new GameObject("Blocker");
		RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
		rectTransform.SetParent(rootCanvas.transform, worldPositionStays: false);
		rectTransform.anchorMin = Vector3.zero;
		rectTransform.anchorMax = Vector3.one;
		rectTransform.sizeDelta = Vector2.zero;
		Canvas canvas = gameObject.AddComponent<Canvas>();
		canvas.overrideSorting = true;
		Canvas component = DropdownGameObject.GetComponent<Canvas>();
		canvas.sortingLayerID = component.sortingLayerID;
		canvas.sortingOrder = component.sortingOrder - 1;
		Canvas canvas2 = null;
		Transform parent = dropdown.parent;
		while (parent != null)
		{
			canvas2 = parent.GetComponent<Canvas>();
			if (canvas2 != null)
			{
				break;
			}
			parent = parent.parent;
		}
		if (canvas2 != null)
		{
			Component[] components = canvas2.GetComponents<BaseRaycaster>();
			components = components;
			for (int i = 0; i < components.Length; i++)
			{
				Type type = components[i].GetType();
				if (gameObject.GetComponent(type) == null)
				{
					gameObject.AddComponent(type);
				}
			}
		}
		else
		{
			GetOrAddComponent<GraphicRaycaster>(gameObject);
		}
		gameObject.AddComponent<Image>().color = Color.clear;
		gameObject.AddComponent<Button>().onClick.AddListener(Hide);
		return gameObject;
	}

	protected virtual void DestroyBlocker(GameObject blocker)
	{
		UnityEngine.Object.Destroy(blocker);
	}

	private void AlphaFadeList(float duration, float alpha)
	{
		CanvasGroup component = DropdownGameObject.GetComponent<CanvasGroup>();
		AlphaFadeList(duration, component.alpha, alpha);
	}

	private void AlphaFadeList(float duration, float start, float end)
	{
		if (!end.Equals(start))
		{
			CanvasGroup component = DropdownGameObject.GetComponent<CanvasGroup>();
			component.alpha = start;
			LeanTween.alphaCanvas(component, end, duration).setIgnoreTimeScale(useUnScaledTime: true);
		}
	}

	public void Hide()
	{
		if (DropdownGameObject != null)
		{
			AlphaFadeList(0.15f, 0f);
			if (IsActive())
			{
				StartCoroutine(DelayedDestroyDropdownList(0.15f));
			}
		}
		if (_blocker != null)
		{
			DestroyBlocker(_blocker);
		}
		_blocker = null;
		Select();
		Debug.Log("Hide");
	}

	private IEnumerator DelayedDestroyDropdownList(float delay)
	{
		yield return new WaitForSecondsRealtime(delay);
		ImmediateDestroyDropdownList();
	}

	private void ImmediateDestroyDropdownList()
	{
		dropdown.gameObject.SetActive(value: false);
	}
}
