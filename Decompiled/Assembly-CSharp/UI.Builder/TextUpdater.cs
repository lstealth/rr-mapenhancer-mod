using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace UI.Builder;

[RequireComponent(typeof(TMP_Text))]
public class TextUpdater : MonoBehaviour
{
	private TMP_Text _text;

	private Coroutine _coroutine;

	private float _interval = 0.1f;

	private Func<string> _textClosure = () => (string)null;

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
		while (true)
		{
			_text.text = _textClosure();
			yield return new WaitForSecondsRealtime(_interval);
		}
	}

	public void Configure(Func<string> valueClosure, float interval)
	{
		PrepareComponents();
		_textClosure = valueClosure;
		_text.text = valueClosure();
		_interval = interval;
	}

	private void PrepareComponents()
	{
		if (!(_text != null))
		{
			_text = GetComponent<TMP_Text>();
		}
	}
}
