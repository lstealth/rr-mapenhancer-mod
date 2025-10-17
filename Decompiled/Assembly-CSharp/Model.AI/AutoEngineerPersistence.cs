using System;
using System.Collections.Generic;
using System.Linq;
using KeyValue.Runtime;

namespace Model.AI;

public struct AutoEngineerPersistence
{
	private readonly KeyValueObject _object;

	public const string AutoEngineerOrdersKey = "aiOrders";

	public const string AutoEngineerContextualOrdersKey = "aiCtxOrders";

	private const string AutoEngineerManualStopDistanceKey = "aiManualStopDistance";

	public const string AutoEngineerPlannerStatus = "aiPlannerStatus";

	private const string AutoEngineerPassModeStatus = "aiPassModeStatus";

	public string PlannerStatus
	{
		get
		{
			return _object["aiPlannerStatus"].StringValue;
		}
		set
		{
			_object["aiPlannerStatus"] = (string.IsNullOrEmpty(value) ? Value.Null() : Value.String(value));
		}
	}

	public Orders Orders
	{
		get
		{
			return Orders.FromPropertyValue(_object["aiOrders"]) ?? Orders.Disabled;
		}
		set
		{
			_object["aiOrders"] = value.PropertyValue;
		}
	}

	public List<ContextualOrder> ContextualOrders
	{
		get
		{
			return _object["aiCtxOrders"].ArrayValue.Select(ContextualOrder.FromPropertyValue).ToList();
		}
		set
		{
			_object["aiCtxOrders"] = Value.Array(value.Select((ContextualOrder aa) => aa.PropertyValue).ToList());
		}
	}

	public float? ManualStopDistance
	{
		get
		{
			Value value = _object["aiManualStopDistance"];
			if (!value.IsNull)
			{
				return value.FloatValue;
			}
			return null;
		}
		set
		{
			_object["aiManualStopDistance"] = ((!value.HasValue) ? Value.Null() : Value.Float(value.Value));
		}
	}

	public string PassengerModeStatus
	{
		get
		{
			return _object["aiPassModeStatus"];
		}
		set
		{
			_object["aiPassModeStatus"] = value;
		}
	}

	public AutoEngineerPersistence(KeyValueObject keyValueObject)
	{
		_object = keyValueObject;
	}

	public void ClearOrders()
	{
		_object["aiOrders"] = Value.Null();
	}

	public IDisposable ObserveOrders(Action<Orders> action, bool callInitial = true)
	{
		return _object.Observe("aiOrders", delegate(Value value)
		{
			action(Orders.FromPropertyValue(value) ?? Orders.Disabled);
		}, callInitial);
	}

	public IDisposable ObserveManualStopDistance(Action<float?> action)
	{
		return _object.Observe("aiManualStopDistance", delegate(Value value)
		{
			action(value.IsNull ? ((float?)null) : new float?(value.FloatValue));
		});
	}

	public IDisposable ObservePlannerStatusChanged(Action action)
	{
		return _object.Observe("aiPlannerStatus", delegate
		{
			action();
		}, callInitial: false);
	}

	public IDisposable ObservePassengerModeStatusChanged(Action action)
	{
		return _object.Observe("aiPassModeStatus", delegate
		{
			action();
		}, callInitial: false);
	}

	public IDisposable ObserveContextualOrdersChanged(Action action)
	{
		return _object.Observe("aiCtxOrders", delegate
		{
			action();
		}, callInitial: false);
	}
}
