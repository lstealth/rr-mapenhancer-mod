using System;
using KeyValue.Runtime;
using UnityEngine;

namespace Effects;

public class MarkerLampToggle : MonoBehaviour, IPickable
{
	public MarkerLamp lamp;

	public string keyBase;

	private IDisposable _positionObserver;

	private IDisposable _litObserver;

	private KeyValueObject _keyValueObject;

	private string KeyPosition => keyBase + ".position";

	private string KeyLit => keyBase + ".lit";

	public float MaxPickDistance => 10f;

	public int Priority => 1;

	public TooltipInfo TooltipInfo => new TooltipInfo("Marker Lamp", "Click to Cycle");

	public PickableActivationFilter ActivationFilter => PickableActivationFilter.PrimaryOnly;

	private void Awake()
	{
		_keyValueObject = GetComponentInParent<KeyValueObject>();
	}

	private void OnEnable()
	{
		if (!string.IsNullOrEmpty(keyBase))
		{
			_litObserver = _keyValueObject.Observe(KeyLit, delegate(Value value)
			{
				lamp.lit = value.BoolValue;
			});
			_positionObserver = _keyValueObject.Observe(KeyPosition, delegate(Value value)
			{
				lamp.position = value.IntValue;
			});
		}
	}

	private void OnDisable()
	{
		_positionObserver?.Dispose();
		_positionObserver = null;
		_litObserver?.Dispose();
		_litObserver = null;
	}

	public void Activate(PickableActivateEvent evt)
	{
		var (value, value2) = lamp.NextState();
		_keyValueObject[KeyLit] = Value.Bool(value);
		_keyValueObject[KeyPosition] = Value.Int(value2);
	}

	public void Deactivate()
	{
	}
}
