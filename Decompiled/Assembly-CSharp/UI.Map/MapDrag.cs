using System;
using UI.Common;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Map;

public class MapDrag : MonoBehaviour, IPointerDownHandler, IEventSystemHandler, IDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
	public Action OnDragStart;

	public Action<Vector2> OnDragChange;

	public Action<float, Vector2> OnZoom;

	public Action<Vector2> OnTeleport;

	public Action<Vector2> OnClick;

	private RectTransform _rectTransform;

	private Window _window;

	private Vector2 _downPointerPosition;

	private Vector2 _currentPointerPosition;

	private bool _pointerOver;

	private bool _isDragging;

	public float RectHeight => _rectTransform.rect.height;

	private void Awake()
	{
		_rectTransform = GetComponent<RectTransform>();
		_window = GetComponentInParent<Window>();
	}

	private void Update()
	{
		if (_pointerOver && GameInput.IsMouseOverGameWindow(_window))
		{
			float y = Input.mouseScrollDelta.y;
			if ((double)Mathf.Abs(y) > 0.001)
			{
				Vector2 arg = NormalizedMousePosition();
				OnZoom?.Invoke(0f - y, arg);
			}
			if (GameInput.shared.Teleport)
			{
				Vector2 obj = NormalizedMousePosition();
				OnTeleport?.Invoke(obj);
			}
		}
	}

	private Vector2 NormalizedMousePosition()
	{
		RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, Input.mousePosition, null, out var localPoint);
		Rect rect = _rectTransform.rect;
		return new Vector2((localPoint.x - rect.x) / rect.width, (localPoint.y - rect.y) / rect.height);
	}

	public void OnPointerDown(PointerEventData data)
	{
		_rectTransform.SetAsLastSibling();
		RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, data.position, data.pressEventCamera, out _downPointerPosition);
		OnDragStart?.Invoke();
		_isDragging = false;
	}

	public void OnDrag(PointerEventData data)
	{
		if (!(_rectTransform == null))
		{
			RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, data.position, data.pressEventCamera, out _currentPointerPosition);
			Vector2 vector = _currentPointerPosition - _downPointerPosition;
			_isDragging = _isDragging || vector.magnitude > 3f;
			OnDragChange?.Invoke(new Vector2(0f - vector.x, 0f - vector.y));
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		_pointerOver = true;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		_pointerOver = false;
	}

	public void OnPointerClick(PointerEventData data)
	{
		if (!_isDragging)
		{
			Vector2 obj = NormalizedMousePosition();
			OnClick?.Invoke(obj);
		}
	}
}
