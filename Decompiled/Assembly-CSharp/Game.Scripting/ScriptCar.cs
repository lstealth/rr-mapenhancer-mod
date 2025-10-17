using System;
using System.Collections.Generic;
using System.Linq;
using Model;
using Model.Definition.Data;
using Model.Ops;
using MoonSharp.Interpreter;
using UI.Console;
using UnityEngine;

namespace Game.Scripting;

public class ScriptCar
{
	public readonly string id;

	private ScriptProperties _properties;

	private ScriptCarAir _air;

	internal Car Car { get; }

	public string name => Car.DisplayName;

	public string car_type => Car.CarType;

	public bool is_locomotive => Car.IsLocomotive;

	public ScriptProperties properties
	{
		get
		{
			if (_properties == null)
			{
				_properties = new ScriptProperties(Car.KeyValueObject);
			}
			return _properties;
		}
	}

	public ScriptCarAir air
	{
		get
		{
			if (_air == null)
			{
				_air = new ScriptCarAir(Car);
			}
			return _air;
		}
	}

	public float speed_mph
	{
		get
		{
			return Car.velocity * 2.23694f;
		}
		set
		{
			float num = value * 0.44703928f;
			List<Car> cars = Car.EnumerateCoupled().ToList();
			Car.set.SetVelocity(num * Car.Orientation, cars);
			Car.velocity = num;
			Car.ResetAtRest();
		}
	}

	public ScriptLocation location_front => new ScriptLocation(Car.LocationF);

	public ScriptLocation location_rear => new ScriptLocation(Car.LocationR);

	public ScriptWaybill waybill
	{
		get
		{
			Waybill? waybill = Car.Waybill;
			if (waybill.HasValue)
			{
				Waybill valueOrDefault = waybill.GetValueOrDefault();
				return new ScriptWaybill(valueOrDefault);
			}
			return null;
		}
	}

	internal ScriptCar(Car car)
	{
		Car = car;
		id = car.id;
	}

	public bool stopped(float duration)
	{
		return Car.IsStopped(duration);
	}

	public List<ScriptCar> get_coupled_cars(string endString)
	{
		Car.LogicalEnd fromEnd = LogicalEndFromString(endString);
		return (from c in Car.EnumerateCoupled(fromEnd)
			select c.ScriptCar()).ToList();
	}

	public List<ScriptCar> get_air_open_cars(string endString)
	{
		Car.LogicalEnd fromEnd = LogicalEndFromString(endString);
		return (from c in Car.EnumerateAirOpen(fromEnd)
			select c.ScriptCar()).ToList();
	}

	private Car.LogicalEnd LogicalEndFromString(string endString)
	{
		if (string.IsNullOrEmpty(endString))
		{
			return Car.LogicalEnd.A;
		}
		return endString.ToLower() switch
		{
			"a" => Car.LogicalEnd.A, 
			"b" => Car.LogicalEnd.B, 
			"f" => Car.EndToLogical(Car.End.F), 
			"r" => Car.EndToLogical(Car.End.R), 
			_ => Car.LogicalEnd.A, 
		};
	}

	public void set_load_percent(string loadId, float percent)
	{
		try
		{
			SetLoadCommand.SetLoadPercent(Car, loadId, percent);
		}
		catch (Exception ex)
		{
			throw new ScriptRuntimeException(ex);
		}
	}

	public bool has_load(string loadId)
	{
		int slotIndex;
		CarLoadInfo? loadInfo = Car.GetLoadInfo(loadId, out slotIndex);
		if (!loadInfo.HasValue)
		{
			return false;
		}
		return loadInfo.GetValueOrDefault().Quantity > 0f;
	}

	public float get_load_percent(string loadId)
	{
		int slotIndex;
		CarLoadInfo? loadInfo = Car.GetLoadInfo(loadId, out slotIndex);
		if (loadInfo.HasValue)
		{
			CarLoadInfo valueOrDefault = loadInfo.GetValueOrDefault();
			LoadSlot loadSlot = Car.Definition.LoadSlots[slotIndex];
			if (loadSlot.MaximumCapacity == 0f)
			{
				return 0f;
			}
			return Mathf.Clamp01(valueOrDefault.Quantity / loadSlot.MaximumCapacity);
		}
		return 0f;
	}

	public void set_passenger_destination(string destinationId, bool enabled)
	{
		AssertIsPassengerCar();
		PassengerMarker value = Car.GetPassengerMarker() ?? PassengerMarker.Empty();
		if (enabled)
		{
			value.Destinations.Add(destinationId);
		}
		else
		{
			value.Destinations.Remove(destinationId);
		}
		Car.SetPassengerMarker(value);
	}

	public bool has_passenger_destination(string destinationId)
	{
		AssertIsPassengerCar();
		return Car.GetPassengerMarker()?.Destinations.Contains(destinationId) ?? false;
	}

	public void add_passengers(string originId, string destinationId, int count)
	{
		AssertIsPassengerCar();
		PassengerMarker value = Car.GetPassengerMarker() ?? PassengerMarker.Empty();
		value.AddPassengers(originId, destinationId, count, TimeWeather.Now);
		Car.SetPassengerMarker(value);
	}

	public void remove_passengers(string destinationId, int count)
	{
		AssertIsPassengerCar();
		PassengerMarker? passengerMarker = Car.GetPassengerMarker();
		if (!passengerMarker.HasValue)
		{
			return;
		}
		PassengerMarker valueOrDefault = passengerMarker.GetValueOrDefault();
		for (int i = 0; i < count; i++)
		{
			if (!valueOrDefault.TryRemovePassenger(destinationId, out var _, out var _, out var _))
			{
				break;
			}
		}
		Car.SetPassengerMarker(valueOrDefault);
	}

	public int get_passenger_count(string destinationId)
	{
		AssertIsPassengerCar();
		PassengerMarker? passengerMarker = Car.GetPassengerMarker();
		if (passengerMarker.HasValue)
		{
			PassengerMarker valueOrDefault = passengerMarker.GetValueOrDefault();
			int num = 0;
			{
				foreach (PassengerGroup group in valueOrDefault.Groups)
				{
					if (group.Destination == destinationId)
					{
						num += group.Count;
					}
				}
				return num;
			}
		}
		return 0;
	}

	private void AssertIsPassengerCar()
	{
		if (!Car.IsPassengerCar())
		{
			throw new ScriptRuntimeException("Not a passenger car");
		}
	}

	public void follow()
	{
		CameraSelector.shared.FollowCar(Car);
	}

	public void select()
	{
		TrainController.Shared.SelectedCar = Car;
	}

	protected bool Equals(ScriptCar other)
	{
		return id == other.id;
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (this == obj)
		{
			return true;
		}
		if (obj.GetType() != GetType())
		{
			return false;
		}
		return Equals((ScriptCar)obj);
	}

	public override int GetHashCode()
	{
		if (id == null)
		{
			return 0;
		}
		return id.GetHashCode();
	}
}
