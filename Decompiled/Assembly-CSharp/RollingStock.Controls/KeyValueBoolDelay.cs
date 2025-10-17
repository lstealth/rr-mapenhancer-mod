using System;
using System.Collections;
using Game.State;
using KeyValue.Runtime;
using UnityEngine;

namespace RollingStock.Controls;

public class KeyValueBoolDelay : MonoBehaviour
{
	public string sourceKey;

	public string targetKey;

	[Range(0f, 10f)]
	public float onDelay;

	[Range(0f, 10f)]
	public float offDelay;

	private IDisposable _observer;

	private KeyValueObject _keyValueObject;

	private bool _isInitial = true;

	private Coroutine _coroutine;

	private void OnEnable()
	{
		if (StateManager.IsHost)
		{
			_keyValueObject = GetComponentInParent<KeyValueObject>();
			_observer = _keyValueObject.Observe(sourceKey, PropertyChanged);
		}
	}

	private void OnDestroy()
	{
		_observer?.Dispose();
		if (_coroutine != null)
		{
			StopCoroutine(_coroutine);
		}
		_coroutine = null;
	}

	private void PropertyChanged(Value value)
	{
		if (_isInitial)
		{
			_keyValueObject[targetKey] = value;
			_isInitial = false;
			return;
		}
		if (_coroutine != null)
		{
			StopCoroutine(_coroutine);
		}
		_coroutine = StartCoroutine(DelayedWrite(value));
	}

	private IEnumerator DelayedWrite(Value value)
	{
		yield return new WaitForSeconds(value.BoolValue ? onDelay : offDelay);
		_keyValueObject[targetKey] = value;
	}
}
