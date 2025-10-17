using System;
using KeyValue.Runtime;
using UnityEngine;

namespace Effects;

public class ClassLightToggle : MonoBehaviour, IPickable
{
	public ClassLight lamp;

	public string keyBase = "class_light";

	private IDisposable _colorObserver;

	private IDisposable _litObserver;

	private KeyValueObject keyValueObject => GetComponentInParent<KeyValueObject>();

	public float MaxPickDistance => 10f;

	public int Priority => 1;

	public PickableActivationFilter ActivationFilter => PickableActivationFilter.PrimaryOnly;

	public TooltipInfo TooltipInfo => new TooltipInfo("Class Light", "Click to Cycle");

	private void Start()
	{
		if (!string.IsNullOrEmpty(keyBase))
		{
			_litObserver = keyValueObject.Observe(keyBase + ".lit", delegate(Value value)
			{
				lamp.lit = value.BoolValue;
			});
			_colorObserver = keyValueObject.Observe(keyBase + ".color", delegate(Value value)
			{
				lamp.color = (ClassLight.LensColor)value.IntValue;
			});
		}
	}

	private void OnDestroy()
	{
		_colorObserver?.Dispose();
		_colorObserver = null;
		_litObserver?.Dispose();
		_litObserver = null;
	}

	public void Activate(PickableActivateEvent evt)
	{
		(bool, ClassLight.LensColor) tuple = lamp.NextState();
		bool item = tuple.Item1;
		ClassLight.LensColor item2 = tuple.Item2;
		KeyValueObject obj = keyValueObject;
		obj[keyBase + ".lit"] = Value.Bool(item);
		obj[keyBase + ".color"] = Value.Int((int)item2);
	}

	public void Deactivate()
	{
	}
}
