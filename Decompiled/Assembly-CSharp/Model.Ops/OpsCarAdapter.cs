using System.Collections.Generic;
using Model.Definition.Data;
using Model.Ops.Definition;
using Serilog;
using UnityEngine;

namespace Model.Ops;

public readonly struct OpsCarAdapter : IOpsCar
{
	private readonly Car _car;

	private readonly List<LoadSlot> _loadSlots;

	private readonly IOpsCarPositionResolver _opsCarPositionResolver;

	public string Id => _car.id;

	public string CarType => _car.CarType;

	public string DisplayName => _car.DisplayName;

	public bool IsOwnedByPlayer => _car.IsOwnedByPlayer;

	public Waybill? Waybill => _car.GetWaybill(_opsCarPositionResolver);

	public PassengerMarker? PassengerMarker
	{
		get
		{
			return _car.GetPassengerMarker();
		}
		set
		{
			_car.SetPassengerMarker(value);
		}
	}

	public float Condition => _car.Condition;

	public int WeightInTons => Mathf.CeilToInt(_car.Weight / 2000f);

	public OpsCarAdapter(Car car, IOpsCarPositionResolver opsCarPositionResolver)
	{
		_car = car;
		_loadSlots = car.Definition.LoadSlots;
		_opsCarPositionResolver = opsCarPositionResolver;
	}

	public override string ToString()
	{
		return _car.ToString();
	}

	public void SetWaybill(Waybill? waybill, IndustryComponent setter, string reason)
	{
		Log.Information("SetWaybill: {car} {waybill} {setter}, reason: {reason}", _car, waybill, setter, reason);
		_car.SetWaybillAuto(waybill, _opsCarPositionResolver);
	}

	public bool IsEmptyOrContains(Load load)
	{
		for (int i = 0; i < _loadSlots.Count; i++)
		{
			if (_loadSlots[i].LoadRequirementsMatch(load.id))
			{
				CarLoadInfo? loadInfo = _car.GetLoadInfo(i);
				if (!loadInfo.HasValue)
				{
					return true;
				}
				if (loadInfo.Value.LoadId == load.id)
				{
					return true;
				}
			}
		}
		return false;
	}

	public (float quantity, float capacity) QuantityOfLoad(Load load)
	{
		return _car.QuantityCapacityOfLoad(load);
	}

	public float Unload(Load load, float quantityToUnload)
	{
		if (quantityToUnload < 1E-07f)
		{
			return 0f;
		}
		for (int i = 0; i < _loadSlots.Count; i++)
		{
			LoadSlot loadSlot = _loadSlots[i];
			CarLoadInfo? loadInfo = _car.GetLoadInfo(i);
			if (!loadInfo.HasValue)
			{
				continue;
			}
			CarLoadInfo valueOrDefault = loadInfo.GetValueOrDefault();
			if (!(valueOrDefault.LoadId != load.id))
			{
				float quantity = valueOrDefault.Quantity;
				float num = Mathf.Clamp(valueOrDefault.Quantity - quantityToUnload, 0f, loadSlot.MaximumCapacity);
				if (num < 0.001f)
				{
					_car.SetLoadInfo(i, null);
				}
				else
				{
					valueOrDefault.Quantity = num;
					_car.SetLoadInfo(i, valueOrDefault);
				}
				return quantity - num;
			}
		}
		return 0f;
	}

	public float Load(Load load, float quantityToLoad)
	{
		if (quantityToLoad < 1E-07f)
		{
			return 0f;
		}
		for (int i = 0; i < _loadSlots.Count; i++)
		{
			LoadSlot loadSlot = _loadSlots[i];
			if (loadSlot.LoadRequirementsMatch(load) && loadSlot.LoadUnits == load.units)
			{
				CarLoadInfo? loadInfo = _car.GetLoadInfo(i);
				if (!loadInfo.HasValue)
				{
					float num = Mathf.Clamp(quantityToLoad, 0f, loadSlot.MaximumCapacity);
					loadInfo = new CarLoadInfo(load.id, num);
					_car.SetLoadInfo(i, loadInfo.Value);
					return num;
				}
				CarLoadInfo value = loadInfo.Value;
				float quantity = value.Quantity;
				float num2 = (value.Quantity = Mathf.Clamp(quantityToLoad + value.Quantity, 0f, loadSlot.MaximumCapacity));
				_car.SetLoadInfo(i, value);
				return num2 - quantity;
			}
		}
		return 0f;
	}

	public bool IsFull(Load load)
	{
		var (num, num2) = QuantityOfLoad(load);
		return Mathf.Abs(num - num2) < 0.001f;
	}

	public bool GetOverrideDestination(OverrideDestination overrideDestination, out OpsCarPosition destination, out string tag)
	{
		if (_car.TryGetOverrideDestination(overrideDestination, _opsCarPositionResolver, out (OpsCarPosition, string)? result))
		{
			destination = result.Value.Item1;
			tag = result.Value.Item2;
			return true;
		}
		destination = default(OpsCarPosition);
		tag = null;
		return false;
	}
}
