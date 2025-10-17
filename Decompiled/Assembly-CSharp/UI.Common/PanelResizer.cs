using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Common;

[RequireComponent(typeof(RectTransform))]
public class PanelResizer : MonoBehaviour, IPointerDownHandler, IEventSystemHandler, IDragHandler, IPointerUpHandler
{
	public Vector2 minSize;

	public Vector2 maxSize;

	private Vector2 _initialSizeDelta;

	private Vector2 _currentPointerPosition;

	private Vector2 _initialPointerPosition;

	private Window _window;

	private RectTransform _windowRectTransform;

	public bool HasUserResized { get; private set; }

	private void Awake()
	{
		_window = base.transform.GetComponentInParent<Window>();
		_windowRectTransform = _window.GetComponent<RectTransform>();
	}

	public void OnPointerDown(PointerEventData data)
	{
		RectTransformUtility.ScreenPointToLocalPointInRectangle(_windowRectTransform, data.position, data.pressEventCamera, out _initialPointerPosition);
		_initialSizeDelta = _windowRectTransform.sizeDelta;
	}

	public void OnDrag(PointerEventData data)
	{
		if (!(_window.contentRectTransform == null))
		{
			RectTransformUtility.ScreenPointToLocalPointInRectangle(_windowRectTransform, data.position, data.pressEventCamera, out _currentPointerPosition);
			Vector2 vector = _currentPointerPosition - _initialPointerPosition;
			Vector2 vector2 = _initialSizeDelta + new Vector2(vector.x, 0f - vector.y);
			vector2 = new Vector2(Mathf.Floor(Mathf.Clamp(vector2.x, minSize.x, maxSize.x)), Mathf.Floor(Mathf.Clamp(vector2.y, minSize.y, maxSize.y)));
			_windowRectTransform.sizeDelta = vector2;
			HasUserResized = true;
		}
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		_window.FireDidResize(_windowRectTransform.sizeDelta);
	}
}
