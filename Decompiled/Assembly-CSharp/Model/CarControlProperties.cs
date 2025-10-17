using System;
using Game.Messages;
using KeyValue.Runtime;

namespace Model;

public class CarControlProperties
{
	private readonly Car _car;

	private readonly KeyValueObject _object;

	public Value this[PropertyChange.Control control]
	{
		get
		{
			return _object[PropertyChange.KeyForControl(control)];
		}
		set
		{
			_car.SendPropertyChange(control, PropertyValueConverter.RuntimeToSnapshot(value));
		}
	}

	public CarControlProperties(Car car, KeyValueObject keyValueObject)
	{
		_car = car;
		_object = keyValueObject;
	}

	public IDisposable Observe(PropertyChange.Control control, Action<Value> action, bool callInitial)
	{
		return _object.Observe(PropertyChange.KeyForControl(control), action, callInitial);
	}
}
