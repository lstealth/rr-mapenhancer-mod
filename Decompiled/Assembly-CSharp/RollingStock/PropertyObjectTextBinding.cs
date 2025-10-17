using System;
using System.Collections.Generic;
using KeyValue.Runtime;
using TMPro;
using UnityEngine;

namespace RollingStock;

public class PropertyObjectTextBinding : MonoBehaviour
{
	public string key;

	public List<TMP_Text> labels = new List<TMP_Text>();

	private IDisposable _observer;

	private void OnEnable()
	{
		KeyValueObject componentInParent = GetComponentInParent<KeyValueObject>();
		if (componentInParent != null)
		{
			_observer = componentInParent.Observe(key, PropertyDidChange);
		}
	}

	private void OnDisable()
	{
		_observer?.Dispose();
		_observer = null;
	}

	private void PropertyDidChange(Value value)
	{
		foreach (TMP_Text label in labels)
		{
			label.text = value.StringValue;
		}
	}
}
