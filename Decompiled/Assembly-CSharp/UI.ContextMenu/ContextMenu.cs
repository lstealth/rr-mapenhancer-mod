using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using Serilog;
using TMPro;
using UI.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace UI.ContextMenu;

public class ContextMenu : MonoBehaviour, ICancelHandler, IEventSystemHandler
{
	[SerializeField]
	protected RectTransform contentRectTransform;

	[SerializeField]
	private ContextMenuItem itemPrefab;

	[SerializeField]
	private RectTransform dividerPrefab;

	[SerializeField]
	private float radius = 100f;

	[SerializeField]
	private RectTransform centerRectTransform;

	[SerializeField]
	private CanvasGroup centerCanvasGroup;

	[SerializeField]
	private TMP_Text centerLabel;

	[SerializeField]
	private float showTime = 0.15f;

	[SerializeField]
	private float hideTime = 0.25f;

	private GameObject _blocker;

	private bool _hasSetupTemplate;

	private const int HighSortingLayer = 30000;

	private const int NumZones = 4;

	private readonly List<List<ContextMenuItem>> _quadrants = new List<List<ContextMenuItem>>(4);

	private readonly List<RectTransform> _dividers = new List<RectTransform>();

	private Coroutine _hideCoroutine;

	private readonly Dictionary<(ContextMenuQuadrant quadrant, int index), float> _itemAngles = new Dictionary<(ContextMenuQuadrant, int), float>();

	private static ContextMenu _shared;

	private GameObject ContentGameObject => contentRectTransform.gameObject;

	public static bool IsShown { get; private set; }

	public static ContextMenu Shared
	{
		get
		{
			if (_shared == null)
			{
				_shared = UnityEngine.Object.FindObjectOfType<ContextMenu>();
			}
			return _shared;
		}
	}

	private void OnEnable()
	{
		contentRectTransform.gameObject.SetActive(value: false);
	}

	private void OnDisable()
	{
		SetContentInactive();
		if (_blocker != null)
		{
			DestroyBlocker(_blocker);
		}
		_blocker = null;
		IsShown = false;
		CancelHideCoroutine();
	}

	private void SetupTemplate(Canvas rootCanvas)
	{
		if (_hasSetupTemplate)
		{
			return;
		}
		_hasSetupTemplate = true;
		GameObject gameObject = contentRectTransform.gameObject;
		gameObject.SetActive(value: true);
		Canvas canvas = null;
		Transform parent = contentRectTransform.parent;
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

	public virtual void OnCancel(BaseEventData eventData)
	{
		Hide();
	}

	public void Show(string centerText)
	{
		if (!GetRootCanvas(out var rootCanvas))
		{
			Log.Warning("Couldn't get root canvas");
			return;
		}
		CancelHideCoroutine();
		SetupTemplate(rootCanvas);
		centerLabel.text = centerText;
		Canvas componentInParent = contentRectTransform.GetComponentInParent<Canvas>();
		Vector3 mousePosition = Input.mousePosition;
		Vector2 anchoredPosition = componentInParent.ScreenToCanvasPosition(mousePosition).XY();
		Vector2 renderingDisplaySize = rootCanvas.renderingDisplaySize;
		float num = radius + 50f;
		if (anchoredPosition.x < num)
		{
			anchoredPosition.x = num;
		}
		if (anchoredPosition.x > renderingDisplaySize.x - num)
		{
			anchoredPosition.x = renderingDisplaySize.x - num;
		}
		if (anchoredPosition.y < num)
		{
			anchoredPosition.y = num;
		}
		if (anchoredPosition.y > renderingDisplaySize.y - num)
		{
			anchoredPosition.y = renderingDisplaySize.y - num;
		}
		contentRectTransform.anchoredPosition = anchoredPosition;
		BuildItemAngles();
		int num2 = _quadrants.Sum((List<ContextMenuItem> items) => items.Count);
		for (int num3 = 0; num3 < 4; num3++)
		{
			for (int num4 = 0; num4 < _quadrants[num3].Count; num4++)
			{
				ContextMenuQuadrant contextMenuQuadrant = (ContextMenuQuadrant)num3;
				GetItemExtentAngles(contextMenuQuadrant, num4, out var middleL, out var middleR);
				float angleRange = ((middleL == middleR) ? 360f : Mathf.Abs(Mathf.DeltaAngle(middleL, middleR)));
				ContextMenuItem contextMenuItem = _quadrants[num3][num4];
				contextMenuItem.SetAngle(middleR, angleRange);
				Vector2 vector = PositionForItem(contextMenuQuadrant, num4);
				float deltaAngle = Mathf.DeltaAngle(_itemAngles[(contextMenuQuadrant, num4)], 0f);
				contextMenuItem.textContainer.pivot = CalculateTextPivot(deltaAngle);
				contextMenuItem.textContainer.anchoredPosition = PositionForItem(contextMenuQuadrant, num4, 1.5f) - vector;
				((RectTransform)contextMenuItem.wedgeImage.transform).localPosition = -vector;
				if (num2 > 1)
				{
					RectTransform rectTransform = UnityEngine.Object.Instantiate(dividerPrefab, contentRectTransform);
					rectTransform.sizeDelta = new Vector2(8f, 70f);
					rectTransform.pivot = new Vector2(0.5f, -1f);
					rectTransform.localPosition = Vector3.zero;
					rectTransform.rotation = Quaternion.Euler(0f, 0f, middleR - 90f);
					_dividers.Add(rectTransform);
				}
			}
		}
		StartCoroutine(AnimateButtonsShown());
		contentRectTransform.gameObject.SetActive(value: true);
		IsShown = true;
		_blocker = CreateBlocker(rootCanvas);
		GameInput.RegisterEscapeHandler(GameInput.EscapeHandler.Transient, delegate
		{
			Hide();
			return true;
		});
	}

	private void GetItemExtentAngles(ContextMenuQuadrant quadrant, int itemIndex, out float middleL, out float middleR)
	{
		float num = _itemAngles[(quadrant, itemIndex)];
		float a = GetAngle(quadrant, itemIndex - 1);
		float b = GetAngle(quadrant, itemIndex + 1);
		middleL = Mathf.LerpAngle(a, num, 0.5f);
		middleR = Mathf.LerpAngle(num, b, 0.5f);
		float GetAngle(ContextMenuQuadrant contextMenuQuadrant, int index)
		{
			if (_itemAngles.TryGetValue((contextMenuQuadrant, index), out var value))
			{
				return value;
			}
			while (index < 0)
			{
				contextMenuQuadrant = ((contextMenuQuadrant > ContextMenuQuadrant.General) ? (contextMenuQuadrant - 1) : ContextMenuQuadrant.Unused2);
				index += _quadrants[(int)contextMenuQuadrant].Count;
			}
			while (index >= _quadrants[(int)contextMenuQuadrant].Count)
			{
				index -= _quadrants[(int)contextMenuQuadrant].Count;
				contextMenuQuadrant = (ContextMenuQuadrant)((int)(contextMenuQuadrant + 1) % 4);
			}
			return _itemAngles[(contextMenuQuadrant, index)];
		}
	}

	private bool GetRootCanvas(out Canvas rootCanvas)
	{
		rootCanvas = null;
		List<Canvas> list = CollectionPool<List<Canvas>, Canvas>.Get();
		base.gameObject.GetComponentsInParent(includeInactive: false, list);
		if (list.Count == 0)
		{
			return false;
		}
		int count = list.Count;
		rootCanvas = list[count - 1];
		for (int i = 0; i < count; i++)
		{
			if (list[i].isRootCanvas || list[i].overrideSorting)
			{
				rootCanvas = list[i];
				break;
			}
		}
		CollectionPool<List<Canvas>, Canvas>.Release(list);
		return true;
	}

	protected virtual GameObject CreateBlocker(Canvas rootCanvas)
	{
		GameObject gameObject = new GameObject("Context Menu Blocker");
		RectTransform rectTransform = gameObject.AddComponent<RectTransform>();
		rectTransform.SetParent(rootCanvas.transform, worldPositionStays: false);
		rectTransform.anchorMin = Vector3.zero;
		rectTransform.anchorMax = Vector3.one;
		rectTransform.sizeDelta = Vector2.zero;
		Canvas canvas = gameObject.AddComponent<Canvas>();
		canvas.overrideSorting = true;
		Canvas component = ContentGameObject.GetComponent<Canvas>();
		canvas.sortingLayerID = component.sortingLayerID;
		canvas.sortingOrder = component.sortingOrder - 1;
		Canvas canvas2 = null;
		Transform parent = contentRectTransform.parent;
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

	public void Hide()
	{
		Hide(null);
	}

	private void Hide((ContextMenuQuadrant, int)? selected)
	{
		GameInput.UnregisterEscapeHandler(GameInput.EscapeHandler.Transient);
		if (_blocker != null)
		{
			DestroyBlocker(_blocker);
		}
		_blocker = null;
		IsShown = false;
		foreach (List<ContextMenuItem> quadrant in _quadrants)
		{
			foreach (ContextMenuItem item in quadrant)
			{
				item.OnClick = null;
			}
		}
		_hideCoroutine = StartCoroutine(HideInternal());
		ClearQuadrants();
		IEnumerator HideInternal()
		{
			AnimateButtonsHidden(selected);
			yield return new WaitForSecondsRealtime(hideTime * 1.25f);
			if (!IsShown)
			{
				SetContentInactive();
			}
			_hideCoroutine = null;
		}
	}

	private void CancelHideCoroutine()
	{
		if (_hideCoroutine != null)
		{
			StopCoroutine(_hideCoroutine);
			_hideCoroutine = null;
			SetContentInactive();
		}
	}

	private void SetContentInactive()
	{
		contentRectTransform.gameObject.SetActive(value: false);
	}

	private void ClearQuadrants()
	{
		_quadrants.Clear();
		for (int i = 0; i < 4; i++)
		{
			_quadrants.Add(new List<ContextMenuItem>());
		}
	}

	public void Clear()
	{
		SetContentInactive();
		contentRectTransform.DestroyChildrenExcept(centerRectTransform);
		ClearQuadrants();
		_dividers.Clear();
	}

	public void AddButton(ContextMenuQuadrant quadrant, string title, SpriteName spriteName, Action action)
	{
		AddButton(quadrant, title, spriteName.Sprite(), action);
	}

	public void AddButton(ContextMenuQuadrant quadrant, string title, Sprite sprite, Action action)
	{
		List<ContextMenuItem> list = _quadrants[(int)quadrant];
		int index = list.Count;
		ContextMenuItem contextMenuItem = UnityEngine.Object.Instantiate(itemPrefab, contentRectTransform);
		contextMenuItem.transform.SetAsFirstSibling();
		contextMenuItem.image.sprite = sprite;
		contextMenuItem.label.text = title;
		contextMenuItem.OnClick = delegate
		{
			action();
			Hide((quadrant, index));
		};
		contextMenuItem.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;
		list.Add(contextMenuItem);
	}

	private static Vector2 CalculateTextPivot(float deltaAngle)
	{
		float x = ((deltaAngle > 0f) ? Mathf.Lerp(1f, 0f, Mathf.InverseLerp(105f, 75f, deltaAngle)) : Mathf.Lerp(1f, 0f, Mathf.InverseLerp(-105f, -75f, deltaAngle)));
		float y = ((deltaAngle > 0f) ? Mathf.Lerp(0.5f, 1f, Mathf.InverseLerp(75f, 105f, deltaAngle)) : Mathf.Lerp(0.5f, 0f, Mathf.InverseLerp(-75f, -105f, deltaAngle)));
		return new Vector2(x, y);
	}

	private IEnumerator AnimateButtonsShown()
	{
		float animationTime = showTime;
		centerCanvasGroup.alpha = 0f;
		centerCanvasGroup.transform.localScale = Vector3.one * 0.5f;
		for (int i = 0; i < 4; i++)
		{
			List<ContextMenuItem> list = _quadrants[i];
			ContextMenuQuadrant quadrant = (ContextMenuQuadrant)i;
			for (int j = 0; j < list.Count; j++)
			{
				Vector2 vector = PositionForItem(quadrant, j, 0.25f);
				ContextMenuItem contextMenuItem = list[j];
				contextMenuItem.transform.localPosition = vector;
				contextMenuItem.canvasGroup.alpha = 0f;
			}
		}
		foreach (RectTransform divider in _dividers)
		{
			GetOrAddComponent<CanvasGroup>(divider.gameObject).alpha = 0f;
		}
		yield return null;
		LeanTween.cancel(centerCanvasGroup.gameObject);
		LeanTween.alphaCanvas(centerCanvasGroup, 1f, animationTime).setEaseInQuad().setIgnoreTimeScale(useUnScaledTime: true);
		LeanTween.scale(centerCanvasGroup.gameObject, Vector3.one, animationTime).setEaseOutQuart().setIgnoreTimeScale(useUnScaledTime: true);
		foreach (RectTransform divider2 in _dividers)
		{
			LeanTween.alphaCanvas(divider2.GetComponent<CanvasGroup>(), 1f, animationTime).setEaseOutQuart().setIgnoreTimeScale(useUnScaledTime: true);
		}
		for (int k = 0; k < 4; k++)
		{
			List<ContextMenuItem> list2 = _quadrants[k];
			ContextMenuQuadrant quadrant2 = (ContextMenuQuadrant)k;
			for (int l = 0; l < list2.Count; l++)
			{
				Vector2 vector2 = PositionForItem(quadrant2, l);
				ContextMenuItem contextMenuItem2 = list2[l];
				LeanTween.moveLocal(((RectTransform)contextMenuItem2.transform).gameObject, vector2, animationTime).setEaseOutQuart().setIgnoreTimeScale(useUnScaledTime: true);
				LeanTween.alphaCanvas(contextMenuItem2.canvasGroup, 1f, animationTime).setEaseOutQuad().setIgnoreTimeScale(useUnScaledTime: true);
			}
		}
	}

	private void AnimateButtonsHidden((ContextMenuQuadrant, int)? maybeSelected)
	{
		float num = hideTime;
		LeanTween.cancel(centerCanvasGroup.gameObject);
		LeanTween.alphaCanvas(centerCanvasGroup, 0f, num).setEaseOutQuad().setIgnoreTimeScale(useUnScaledTime: true);
		LeanTween.scale(centerCanvasGroup.gameObject, Vector3.one * 0.25f, num).setEaseInBack().setIgnoreTimeScale(useUnScaledTime: true);
		for (int i = 0; i < 4; i++)
		{
			List<ContextMenuItem> list = _quadrants[i];
			ContextMenuQuadrant contextMenuQuadrant = (ContextMenuQuadrant)i;
			for (int j = 0; j < list.Count; j++)
			{
				bool flag = maybeSelected.HasValue && maybeSelected.Value.Item1 == contextMenuQuadrant && maybeSelected.Value.Item2 == j;
				ContextMenuItem contextMenuItem = list[j];
				if (!flag)
				{
					Vector2 vector = PositionForItem(contextMenuQuadrant, j, 0.25f);
					LeanTween.moveLocal(((RectTransform)contextMenuItem.transform).gameObject, vector, num).setEaseInBack().setIgnoreTimeScale(useUnScaledTime: true);
				}
				LeanTween.alphaCanvas(contextMenuItem.canvasGroup, 0f, num).setEaseInQuad().setDelay(flag ? 0.15f : 0f)
					.setIgnoreTimeScale(useUnScaledTime: true);
				LeanTween.alphaCanvas(contextMenuItem.wedgeImage.GetComponent<CanvasGroup>(), 0f, num / 2f).setEaseInQuad().setIgnoreTimeScale(useUnScaledTime: true);
			}
		}
		foreach (RectTransform divider in _dividers)
		{
			LeanTween.alphaCanvas(divider.GetComponent<CanvasGroup>(), 0f, num).setIgnoreTimeScale(useUnScaledTime: true);
		}
	}

	private Vector2 PositionForItem(ContextMenuQuadrant quadrant, int index, float normalizedRadius = 1f)
	{
		GetItemExtentAngles(quadrant, index, out var middleL, out var middleR);
		float f = Mathf.LerpAngle(middleL, middleR, 0.5f) * (MathF.PI / 180f);
		float num = radius * normalizedRadius;
		return new Vector2(Mathf.Cos(f) * num, Mathf.Sin(f) * num);
	}

	private void BuildItemAngles()
	{
		int num = 0;
		int num2 = -1;
		for (int i = 0; i < _quadrants.Count; i++)
		{
			int count = _quadrants[i].Count;
			if (count > 0 && num2 < 0)
			{
				num2 = i;
			}
			num += count;
		}
		float[] angles = new float[num];
		int num3 = 0;
		for (int j = 0; j < 4; j++)
		{
			List<ContextMenuItem> list = _quadrants[j];
			for (int k = 0; k < list.Count; k++)
			{
				angles[num3] = DefaultAngleForItem((ContextMenuQuadrant)j, k, list.Count);
				num3++;
			}
		}
		if (angles.Length > 1)
		{
			for (int l = 0; l < 100; l++)
			{
				bool flag = false;
				int al = angles.Length;
				int ai;
				for (ai = 0; ai < al; ai++)
				{
					float num4 = angles[ai];
					float target = Angle(-1);
					float target2 = Angle(1);
					float num5;
					for (num5 = Mathf.DeltaAngle(num4, target); num5 > 0f; num5 -= 360f)
					{
					}
					float num6;
					for (num6 = Mathf.DeltaAngle(num4, target2); num6 < 0f; num6 += 360f)
					{
					}
					if (!(num5 < -60f) || !(num6 > 60f))
					{
						float target3 = ((num5 > -60f) ? (Mathf.Lerp(num5, num6, 0.75f) + num4) : ((!(num6 < 60f)) ? (Mathf.Lerp(num5, num6, 0.5f) + num4) : (Mathf.Lerp(num5, num6, 0.25f) + num4)));
						float num7 = Mathf.MoveTowardsAngle(num4, target3, 1f);
						if (Mathf.Abs(num7 - num4) > 0.1f)
						{
							flag = true;
						}
						angles[ai] = num7;
					}
				}
				if (!flag)
				{
					break;
				}
				float Angle(int offset)
				{
					int n;
					for (n = ai + offset; n < 0; n += al)
					{
					}
					while (n >= al)
					{
						n -= al;
					}
					return angles[n];
				}
			}
		}
		_itemAngles.Clear();
		int num8 = num2;
		int num9 = 0;
		float[] array = angles;
		foreach (float value in array)
		{
			_itemAngles[((ContextMenuQuadrant)num8, num9)] = value;
			num9++;
			while (num8 < _quadrants.Count && num9 >= _quadrants[num8].Count)
			{
				num8++;
				num9 = 0;
			}
		}
	}

	private static float DefaultAngleForItem(ContextMenuQuadrant quadrant, int index, int quadrantItemCount)
	{
		int num = quadrant switch
		{
			ContextMenuQuadrant.General => 90, 
			ContextMenuQuadrant.Unused1 => 180, 
			ContextMenuQuadrant.Brakes => 270, 
			ContextMenuQuadrant.Unused2 => 0, 
			_ => throw new ArgumentOutOfRangeException("quadrant", quadrant, null), 
		};
		if (quadrantItemCount <= 1)
		{
			return num;
		}
		int num2 = ((quadrantItemCount <= 3) ? 30 : (90 / (quadrantItemCount - 1)));
		return (float)num + -0.5f * (float)((quadrantItemCount - 1) * num2) + (float)(num2 * (quadrantItemCount - 1 - index));
	}
}
