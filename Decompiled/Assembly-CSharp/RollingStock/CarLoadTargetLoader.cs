using System;
using System.Collections;
using System.Collections.Generic;
using Game.State;
using Helpers;
using JetBrains.Annotations;
using KeyValue.Runtime;
using Model;
using Model.Definition.Data;
using Model.Ops;
using Model.Ops.Definition;
using Serilog;
using Track;
using UnityEngine;

namespace RollingStock;

public class CarLoadTargetLoader : MonoBehaviour
{
	[Tooltip("If non-null, the industry from which this load is sourced. If null, unlimited loads will be provided.")]
	public Industry sourceIndustry;

	[Tooltip("The load to be loaded.")]
	public Load load;

	[Tooltip("Units per second to swap.")]
	public float outputRate = 1f;

	public float maximumSpeedInMph = 5f;

	[Tooltip("Radius of the loader itself.")]
	[Range(0.1f, 1f)]
	public float radius = 0.2f;

	public KeyValueObject keyValueObject;

	[Tooltip("Writes true when a CarLoadTarget is being loaded.")]
	public string isLoadingBoolKey;

	[Tooltip("Observed. When true, starts checking for a CarLoadTarget to load.")]
	public string canLoadBoolKey;

	public bool onlyLoadPlayerCars = true;

	private readonly HashSet<IDisposable> _observers = new HashSet<IDisposable>();

	private Coroutine _loadCoroutine;

	private bool CanLoad => keyValueObject[canLoadBoolKey].BoolValue;

	private bool IsLoading => keyValueObject[isLoadingBoolKey].BoolValue;

	private bool HasLoadAvailable
	{
		get
		{
			if (sourceIndustry == null)
			{
				return true;
			}
			return sourceIndustry.Storage.QuantityInStorage(load) > 0f;
		}
	}

	private void OnEnable()
	{
		if (!StateManager.IsHost)
		{
			return;
		}
		_observers.Add(keyValueObject.Observe(canLoadBoolKey, delegate(Value value)
		{
			if (_loadCoroutine != null)
			{
				SetLoading(loading: false);
				StopCoroutine(_loadCoroutine);
			}
			if (value.BoolValue)
			{
				_loadCoroutine = StartCoroutine(LoadLoop());
			}
		}));
	}

	private void OnDisable()
	{
		foreach (IDisposable observer in _observers)
		{
			observer.Dispose();
		}
		_observers.Clear();
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireSphere(base.transform.position, radius);
	}

	private IEnumerator LoadLoop()
	{
		TrainController trainController = TrainController.Shared;
		WaitForSeconds wait = new WaitForSeconds(1f);
		HashSet<Car> cars = new HashSet<Car>();
		while (CanLoad)
		{
			yield return wait;
			Vector3 point = WorldTransformer.WorldToGame(base.transform.position);
			trainController.CheckForCarsAtPoint(point, radius + 2f, cars);
			if (cars.Count == 0)
			{
				continue;
			}
			foreach (Car car in cars)
			{
				if (Mathf.Abs(car.velocity) * 2.23694f > maximumSpeedInMph || (onlyLoadPlayerCars && !car.IsOwnedByPlayer))
				{
					continue;
				}
				int slotIndex;
				LoadSlot loadSlot = LoadSlotFromCar(car, point, out slotIndex);
				if (loadSlot == null)
				{
					continue;
				}
				while (!IsFull(car, slotIndex) && CanLoad && HasLoadAvailable)
				{
					SetLoading(loading: true);
					yield return wait;
					Car car2 = trainController.CheckForCarAtPoint(point);
					if (car2 == null || car2 != car)
					{
						break;
					}
					Load(car, loadSlot, slotIndex, 1f);
				}
				SetLoading(loading: false);
				break;
			}
		}
	}

	private void Load(Car car, LoadSlot loadSlot, int slotIndex, float dt)
	{
		CarLoadInfo value = car.GetLoadInfo(slotIndex) ?? new CarLoadInfo(load.id, 0f);
		float num = Mathf.Clamp(outputRate / dt, 0f, loadSlot.MaximumCapacity - value.Quantity);
		if (sourceIndustry != null)
		{
			num = sourceIndustry.Storage.RemoveFromStorage(load, num);
		}
		value.Quantity += num;
		car.SetLoadInfo(slotIndex, value);
	}

	private bool IsFull(Car car, int slotIndex)
	{
		LoadSlot loadSlot = car.Definition.LoadSlots[slotIndex];
		CarLoadInfo? loadInfo = car.GetLoadInfo(slotIndex);
		if (!loadInfo.HasValue)
		{
			return false;
		}
		if (loadSlot.MaximumCapacity == 0f)
		{
			return true;
		}
		return loadInfo.Value.Quantity / loadSlot.MaximumCapacity > 0.999f;
	}

	[CanBeNull]
	private LoadSlot LoadSlotFromCar(Car car, Vector3 point, out int slotIndex)
	{
		Graph graph = TrainController.Shared.graph;
		Matrix4x4 transformMatrix = car.GetTransformMatrix(graph);
		CarLoadTarget[] componentsInChildren = car.GetComponentsInChildren<CarLoadTarget>();
		foreach (CarLoadTarget carLoadTarget in componentsInChildren)
		{
			if (carLoadTarget.slotIndex >= car.Definition.LoadSlots.Count)
			{
				Log.Error("LoadSlotFromCar: {car} target {target} index {index} is out of range", car, carLoadTarget.name, carLoadTarget.slotIndex);
				continue;
			}
			LoadSlot loadSlot = car.Definition.LoadSlots[carLoadTarget.slotIndex];
			if (!string.IsNullOrEmpty(loadSlot.RequiredLoadIdentifier) && !(loadSlot.RequiredLoadIdentifier != load.id))
			{
				Vector3 point2 = car.transform.InverseTransformPoint(carLoadTarget.transform.position);
				Vector3 vector = transformMatrix.MultiplyPoint3x4(point2);
				if (Vector3.Distance(point.ZeroY(), vector.ZeroY()) <= carLoadTarget.radius + radius)
				{
					slotIndex = carLoadTarget.slotIndex;
					return loadSlot;
				}
			}
		}
		slotIndex = -1;
		return null;
	}

	private void SetLoading(bool loading)
	{
		if (!(keyValueObject == null) && !string.IsNullOrEmpty(isLoadingBoolKey))
		{
			keyValueObject[isLoadingBoolKey] = Value.Bool(loading);
		}
	}
}
