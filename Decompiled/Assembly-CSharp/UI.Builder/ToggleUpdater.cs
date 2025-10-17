using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Builder;

[RequireComponent(typeof(Toggle))]
public class ToggleUpdater : MonoBehaviour
{
	private Toggle _toggle;

	private Coroutine _coroutine;

	private Func<bool> _valueClosure = () => false;

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
		bool isOnWithoutNotify = _valueClosure();
		_toggle.SetIsOnWithoutNotify(isOnWithoutNotify);
	}

	public void Configure(Func<bool> valueClosure)
	{
		PrepareComponents();
		_valueClosure = valueClosure;
		UpdateValue();
	}

	private void PrepareComponents()
	{
		if (!(_toggle != null))
		{
			_toggle = GetComponent<Toggle>();
		}
	}
}
