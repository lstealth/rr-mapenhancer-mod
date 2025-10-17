using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Builder;

[RequireComponent(typeof(Slider))]
public class SliderUpdater : MonoBehaviour
{
	private Slider _slider;

	private Coroutine _coroutine;

	private Func<float> _valueClosure = () => 0f;

	private void OnEnable()
	{
		PrepareComponents();
		_coroutine = StartCoroutine(UpdateCoroutine());
	}

	private void OnDisable()
	{
		StopCoroutine(_coroutine);
		_coroutine = null;
	}

	private IEnumerator UpdateCoroutine()
	{
		WaitForSecondsRealtime wait = new WaitForSecondsRealtime(0.1f);
		while (true)
		{
			UpdateValue();
			yield return wait;
		}
	}

	private void UpdateValue()
	{
		float valueWithoutNotify = _valueClosure();
		_slider.SetValueWithoutNotify(valueWithoutNotify);
	}

	public void Configure(Func<float> valueClosure)
	{
		PrepareComponents();
		_valueClosure = valueClosure;
		UpdateValue();
	}

	private void PrepareComponents()
	{
		if (!(_slider != null))
		{
			_slider = GetComponent<Slider>();
		}
	}
}
