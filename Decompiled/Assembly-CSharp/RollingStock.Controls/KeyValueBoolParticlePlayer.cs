using System;
using KeyValue.Runtime;
using UnityEngine;

namespace RollingStock.Controls;

[RequireComponent(typeof(ParticleSystem))]
public class KeyValueBoolParticlePlayer : MonoBehaviour
{
	public string key;

	[Tooltip("True if the bool should be inverted.")]
	public bool invert;

	private IDisposable _observer;

	private ParticleSystem _particleSystem;

	private void Awake()
	{
		_particleSystem = GetComponent<ParticleSystem>();
	}

	private void OnEnable()
	{
		KeyValueObject componentInParent = GetComponentInParent<KeyValueObject>();
		if (componentInParent != null)
		{
			_observer = componentInParent.Observe(key, PropertyChanged);
		}
	}

	private void OnDisable()
	{
		_observer?.Dispose();
		_observer = null;
	}

	private void PropertyChanged(Value value)
	{
		bool flag = value.BoolValue;
		if (invert)
		{
			flag = !flag;
		}
		if (flag)
		{
			_particleSystem.Play();
		}
		else
		{
			_particleSystem.Stop();
		}
	}
}
