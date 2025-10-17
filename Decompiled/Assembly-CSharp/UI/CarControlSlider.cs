using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI;

public class CarControlSlider : Slider
{
	private bool _isPointerDown;

	public TMP_Text handleText;

	protected override void Awake()
	{
		base.Awake();
		handleText = GetComponentInChildren<TMP_Text>();
	}

	public override void OnPointerDown(PointerEventData eventData)
	{
		_isPointerDown = true;
		base.OnPointerDown(eventData);
	}

	public override void OnPointerUp(PointerEventData eventData)
	{
		_isPointerDown = false;
		base.OnPointerUp(eventData);
	}

	public void SetValueUnlessDragging(float newValue)
	{
		if (!_isPointerDown)
		{
			value = newValue;
		}
	}
}
