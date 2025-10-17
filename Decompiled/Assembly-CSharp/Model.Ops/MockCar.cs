using System;
using Model.Ops.Definition;
using UnityEngine;

namespace Model.Ops;

public class MockCar : IOpsCar
{
	private Load _load;

	private float _loadQuantity;

	public string Id { get; }

	public string CarType { get; }

	public string DisplayName => Id;

	public bool IsOwnedByPlayer => false;

	public int WeightInTons { get; set; }

	public Waybill? Waybill { get; private set; }

	public PassengerMarker? PassengerMarker { get; set; }

	public float Condition { get; } = 1f;

	public MockCar(string id, string carType, Load load, float loadQuantity)
	{
		Id = id;
		CarType = carType;
		_load = load;
		_loadQuantity = loadQuantity;
	}

	public override string ToString()
	{
		return string.Format("{0}{1} {2} {3:F1}", CarType, Id, _load?.name ?? "<empty>", _loadQuantity);
	}

	public bool IsEmptyOrContains(Load load)
	{
		if (!(_loadQuantity < 0.001f))
		{
			return _load.Equals(load);
		}
		return true;
	}

	public (float quantity, float capacity) QuantityOfLoad(Load load)
	{
		if (_load != null && _load.Equals(load))
		{
			return (quantity: _loadQuantity, capacity: load.NominalQuantityPerCarLoad);
		}
		return (quantity: 0f, capacity: load.NominalQuantityPerCarLoad);
	}

	public float Unload(Load load, float quantityToConsume)
	{
		if (!_load.Equals(load))
		{
			return 0f;
		}
		float num = Mathf.Min(_loadQuantity, quantityToConsume);
		_loadQuantity -= num;
		return num;
	}

	public float Load(Load load, float quantityToLoad)
	{
		if (_load != null && !_load.Equals(load))
		{
			throw new ArgumentException("Load mismatch");
		}
		float num = Mathf.Min(load.NominalQuantityPerCarLoad - _loadQuantity, quantityToLoad);
		_loadQuantity += num;
		_load = load;
		return num;
	}

	public bool IsFull(Load load)
	{
		return Mathf.Abs(load.NominalQuantityPerCarLoad - _loadQuantity) < 0.001f;
	}

	public void SetWaybill(Waybill? waybill, IndustryComponent setter, string reason)
	{
		Waybill = waybill;
	}

	public bool GetOverrideDestination(OverrideDestination overrideDestination, out OpsCarPosition destination, out string tag)
	{
		destination = default(OpsCarPosition);
		tag = null;
		return false;
	}
}
