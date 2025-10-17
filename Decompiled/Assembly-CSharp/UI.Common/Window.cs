using System;
using System.Collections;
using Game;
using Helpers;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Common;

[RequireComponent(typeof(RectTransform))]
public class Window : MonoBehaviour, IPointerDownHandler, IEventSystemHandler
{
	public enum Position
	{
		LowerLeft,
		LowerRight,
		UpperLeft,
		UpperRight,
		Center,
		CenterRight
	}

	public readonly struct Sizing : IEquatable<Sizing>
	{
		public readonly Vector2Int MinSize;

		public readonly Vector2Int MaxSize;

		public bool IsResizable => MinSize != MaxSize;

		private Sizing(Vector2Int minSize, Vector2Int maxSize)
		{
			MinSize = minSize;
			MaxSize = maxSize;
		}

		public static Sizing Fixed(Vector2Int size)
		{
			return new Sizing(size, size);
		}

		public static Sizing Resizable(Vector2Int minSize, Vector2Int maxSize)
		{
			return new Sizing(minSize, maxSize);
		}

		public static Sizing Resizable(Vector2Int minSize)
		{
			return new Sizing(minSize, new Vector2Int(int.MaxValue, int.MaxValue));
		}

		public Vector2Int Clamp(Vector2Int size)
		{
			return new Vector2Int(Mathf.Clamp(size.x, MinSize.x, MaxSize.x), Mathf.Clamp(size.y, MinSize.y, MaxSize.y));
		}

		public bool Equals(Sizing other)
		{
			if (MinSize.Equals(other.MinSize))
			{
				return MaxSize.Equals(other.MaxSize);
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is Sizing other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(MinSize, MaxSize);
		}
	}

	public TMP_Text titleLabel;

	public RectTransform contentRectTransform;

	[SerializeField]
	private PanelResizer resizer;

	[SerializeField]
	private DraggablePanel draggablePanel;

	public Action DelegateRequestClose;

	private bool _resizable;

	private bool _hasRestoredSize;

	private RectTransform _rectTransform;

	public bool IsShown { get; private set; }

	public string Title
	{
		get
		{
			return titleLabel.text;
		}
		set
		{
			titleLabel.text = value;
		}
	}

	public Vector2 InitialContentSize { get; private set; }

	public bool HasUserResized
	{
		get
		{
			if (!_hasRestoredSize)
			{
				if (_resizable)
				{
					return resizer.HasUserResized;
				}
				return false;
			}
			return true;
		}
	}

	private RectTransform RectTransform
	{
		get
		{
			if (_rectTransform == null)
			{
				_rectTransform = GetComponent<RectTransform>();
			}
			return _rectTransform;
		}
	}

	public event Action<bool> OnShownWillChange;

	public event Action<bool> OnShownDidChange;

	public event Action<Vector2> OnDidResize;

	public event Action<Vector2> OnDidPosition;

	private void Awake()
	{
		InitialContentSize = GetContentSize();
		draggablePanel.OnPanelDragged += delegate(Vector2 pos)
		{
			this.OnDidPosition?.Invoke(pos);
		};
	}

	private void Start()
	{
		UpdateForShown();
	}

	private void SetShown(bool shown)
	{
		if (IsShown != shown)
		{
			IsShown = shown;
			StartCoroutine(ShowCoroutine(shown));
		}
	}

	private IEnumerator ShowCoroutine(bool shown)
	{
		if (!shown)
		{
			yield return new WaitForEndOfFrame();
		}
		this.OnShownWillChange?.Invoke(shown);
		UpdateForShown();
		this.OnShownDidChange?.Invoke(shown);
	}

	public void OrderFront()
	{
		RectTransform.SetAsLastSibling();
	}

	public void ShowWindow()
	{
		SetShown(shown: true);
		ClampToParentBounds();
		OrderFront();
	}

	public void CloseWindow()
	{
		SetShown(shown: false);
	}

	public void HandleRequestCloseWindow()
	{
		if (DelegateRequestClose != null)
		{
			DelegateRequestClose();
		}
		else
		{
			CloseWindow();
		}
	}

	public void SetResizable(Vector2 minSize, Vector2 maxSize)
	{
		_resizable = true;
		if (IsShown)
		{
			resizer.gameObject.SetActive(value: true);
		}
		resizer.minSize = minSize;
		resizer.maxSize = maxSize;
	}

	private void ClampToParentBounds()
	{
		RectTransform rectTransform = RectTransform;
		Vector3 localPosition = rectTransform.localPosition;
		RectTransform component = rectTransform.parent.GetComponent<RectTransform>();
		Vector3 vector = component.rect.min - rectTransform.rect.min;
		Vector3 vector2 = component.rect.max - rectTransform.rect.max;
		localPosition.x = Mathf.Clamp(localPosition.x, vector.x, vector2.x);
		localPosition.y = Mathf.Clamp(localPosition.y, vector.y, vector2.y);
		rectTransform.localPosition = localPosition;
	}

	public void SetPosition(Position position)
	{
		RectTransform rectTransform = RectTransform;
		Vector2 size = rectTransform.rect.size;
		RectTransform rectTransform2 = rectTransform;
		rectTransform2.position = (position switch
		{
			Position.LowerLeft => new Vector2(-100f, -100f), 
			Position.LowerRight => new Vector2(Screen.width, -100f), 
			Position.UpperLeft => new Vector2(-100f, Screen.height), 
			Position.UpperRight => new Vector2(Screen.width, Screen.height), 
			Position.Center => new Vector2((float)Screen.width * 0.5f - size.x * 0.5f, (float)Screen.height * 0.5f + size.y * 0.5f), 
			Position.CenterRight => new Vector2((float)Screen.width * 0.75f - size.x * 0.5f, (float)Screen.height * 0.5f + size.y * 0.5f), 
			_ => throw new ArgumentOutOfRangeException("position", position, null), 
		}).Round();
		ClampToParentBounds();
	}

	public void SetPositionRestoring(Vector2 position)
	{
		RectTransform.anchoredPosition = position;
		ClampToParentBounds();
		_hasRestoredSize = true;
	}

	public void SetContentSize(Vector2 contentSize)
	{
		RectTransform rectTransform = RectTransform;
		Vector2 vector = contentRectTransform.rect.size - rectTransform.rect.size;
		Vector2 sizeDelta = contentSize - vector;
		float graphicsCanvasScale = Preferences.GraphicsCanvasScale;
		sizeDelta.x = Mathf.Min(sizeDelta.x, (float)Screen.width / graphicsCanvasScale);
		sizeDelta.y = Mathf.Min(sizeDelta.y, (float)Screen.height / graphicsCanvasScale);
		rectTransform.sizeDelta = sizeDelta;
	}

	public Vector2 GetContentSize()
	{
		return contentRectTransform.rect.size;
	}

	public void SetContentWidth(int width)
	{
		Vector2 contentSize = GetContentSize();
		contentSize.x = width;
		InitialContentSize = contentSize;
		SetContentSize(contentSize);
	}

	public void SetContentHeight(int height)
	{
		Vector2 contentSize = GetContentSize();
		contentSize.y = height;
		InitialContentSize = contentSize;
		SetContentSize(contentSize);
	}

	private void UpdateForShown()
	{
		RectTransform rectTransform = RectTransform;
		if (rectTransform == null)
		{
			return;
		}
		bool isShown = IsShown;
		foreach (RectTransform item in rectTransform)
		{
			if (item.gameObject == resizer.gameObject)
			{
				item.gameObject.SetActive(_resizable && isShown);
			}
			else
			{
				item.gameObject.SetActive(isShown);
			}
		}
		if (isShown)
		{
			ClampToParentBounds();
		}
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		OrderFront();
	}

	public void UpdateContentSizeFixedHorizontal()
	{
		(contentRectTransform.GetComponent<LayoutElement>() ?? contentRectTransform.gameObject.AddComponent<LayoutElement>()).preferredWidth = InitialContentSize.x;
		LayoutRebuilder.ForceRebuildLayoutImmediate(contentRectTransform);
		LayoutRebuilder.ForceRebuildLayoutImmediate(contentRectTransform);
		SetContentSize(new Vector2(InitialContentSize.x, contentRectTransform.rect.height));
	}

	public void FireDidResize(Vector2 sizeDelta)
	{
		this.OnDidResize?.Invoke(GetContentSize());
	}

	public Vector2 GetPosition()
	{
		return RectTransform.anchoredPosition;
	}
}
