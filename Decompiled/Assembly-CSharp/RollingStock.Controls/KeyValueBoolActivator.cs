using System;
using System.Collections;
using Game.State;
using KeyValue.Runtime;
using UnityEngine;

namespace RollingStock.Controls;

public class KeyValueBoolActivator : MonoBehaviour
{
	public string objectId = "";

	public string key = "key";

	[Tooltip("If the key is null, this value will be assumed.")]
	public bool defaultValue;

	public GameObject[] targets = Array.Empty<GameObject>();

	private IDisposable _observer;

	private void OnEnable()
	{
		StartCoroutine(StartObserving());
	}

	private void OnDisable()
	{
		_observer?.Dispose();
	}

	private IEnumerator StartObserving()
	{
		IKeyValueObject keyValueObject;
		while (true)
		{
			keyValueObject = StateManager.Shared.KeyValueObjectForId(objectId);
			if (keyValueObject != null)
			{
				break;
			}
			yield return new WaitForSeconds(1f);
		}
		_observer = keyValueObject.Observe(key, PropertyDidChange);
	}

	private void PropertyDidChange(Value value)
	{
		bool active = (value.IsNull ? defaultValue : value.BoolValue);
		GameObject[] array = targets;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(active);
		}
	}
}
