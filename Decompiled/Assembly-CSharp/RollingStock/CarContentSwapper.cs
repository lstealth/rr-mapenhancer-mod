using System.Collections;
using Game.State;
using Helpers;
using KeyValue.Runtime;
using Model;
using Model.Definition.Data;
using Model.Ops;
using UnityEngine;

namespace RollingStock;

public class CarContentSwapper : MonoBehaviour
{
	public CarTypeFilter carTypeFilter;

	public CarContent inputContent = CarContent.Empty();

	public CarContent outputContent = CarContent.Empty();

	[Tooltip("Units per second to swap.")]
	public float outputRate = 1f;

	public float maximumSpeedInMph = 5f;

	public string loadingBoolKey;

	private KeyValueObject _keyValueObject;

	private void OnEnable()
	{
		_keyValueObject = GetComponent<KeyValueObject>();
		if (StateManager.IsHost)
		{
			StartCoroutine(Loop());
		}
	}

	private IEnumerator Loop()
	{
		TrainController trainController = TrainController.Shared;
		while (true)
		{
			yield return new WaitForSeconds(1f);
			Vector3 point = WorldTransformer.WorldToGame(base.transform.position);
			Car car = trainController.CheckForCarAtPoint(point);
			if (car == null || Mathf.Abs(car.velocity) * 2.23694f > maximumSpeedInMph)
			{
				continue;
			}
			if (!carTypeFilter.Matches(car.CarType))
			{
				Debug.Log($"Expected {carTypeFilter}, got {car.CarType}");
			}
			else
			{
				if (string.IsNullOrEmpty(loadingBoolKey) || outputContent.load == null)
				{
					continue;
				}
				string key = loadingBoolKey;
				_keyValueObject[key] = Value.Bool(value: true);
				while (true)
				{
					yield return new WaitForSeconds(0.5f);
					Car car2 = trainController.CheckForCarAtPoint(point);
					if (car2 == null || car2 != car)
					{
						break;
					}
					int count = car.Definition.LoadSlots.Count;
					for (int i = 0; i < count; i++)
					{
						LoadSlot loadSlot = car.Definition.LoadSlots[i];
						if (loadSlot.LoadRequirementsMatch(outputContent.load))
						{
							CarLoadInfo value = car.GetLoadInfo(i) ?? new CarLoadInfo(outputContent.load.id, 0f);
							if (!(value.LoadId != outputContent.load.id))
							{
								value.Quantity = Mathf.Clamp(value.Quantity + outputRate * 0.5f, 0f, loadSlot.MaximumCapacity);
								car.SetLoadInfo(i, value);
							}
						}
					}
				}
				_keyValueObject[key] = Value.Bool(value: false);
			}
		}
	}
}
