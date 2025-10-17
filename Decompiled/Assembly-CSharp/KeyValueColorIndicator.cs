using System;
using KeyValue.Runtime;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class KeyValueColorIndicator : MonoBehaviour
{
	public string key;

	public Gradient gradient;

	private IDisposable _observer;

	private KeyValueObject kvObject => GetComponentInParent<KeyValueObject>();

	private void OnEnable()
	{
		_observer = kvObject.Observe(key, KeyDidChange);
	}

	private void OnDisable()
	{
		_observer.Dispose();
	}

	private void KeyDidChange(Value value)
	{
		Color color = gradient.Evaluate(value.FloatValue);
		GetComponent<Renderer>().material.color = color;
	}
}
