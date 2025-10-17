using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI;

[RequireComponent(typeof(Slider))]
public class SliderPointerUpEventer : MonoBehaviour, IPointerUpHandler, IEventSystemHandler
{
	private Slider _slider;

	public event Action<float> onPointerUp;

	private void Awake()
	{
		_slider = GetComponent<Slider>();
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		this.onPointerUp?.Invoke(_slider.value);
	}
}
