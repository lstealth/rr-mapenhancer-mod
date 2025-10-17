using System;
using System.Collections;
using System.Collections.Generic;
using Game.Notices;
using Game.State;
using Model.Definition.Data;
using Model.Ops;
using Model.Ops.Definition;
using Serilog;
using UnityEngine;

namespace Model.AI;

public class AutoEngineerFuelAlerter : MonoBehaviour
{
	private enum LoadCategory
	{
		Fuel,
		Water
	}

	private BaseLocomotive _locomotive;

	private Car _fuelCar;

	private Coroutine _coroutine;

	private readonly Dictionary<string, float> _lastPercentByLoad = new Dictionary<string, float>();

	private readonly float[] _thresholds = new float[4] { 0.2f, 0.1f, 0.05f, 0.01f };

	private const string NoticeKeyFuel = "ai-fuel";

	private const string NoticeKeyWater = "ai-h2o";

	private readonly Dictionary<LoadCategory, bool> _postedNotice = new Dictionary<LoadCategory, bool>();

	private void OnEnable()
	{
		_locomotive = GetComponent<BaseLocomotive>();
		_coroutine = StartCoroutine(Loop());
	}

	private void OnDisable()
	{
		if (_locomotive != null && !StateManager.IsUnloading)
		{
			_locomotive.PostNotice("ai-fuel", null);
			_locomotive.PostNotice("ai-h2o", null);
		}
		StopCoroutine(_coroutine);
		_coroutine = null;
	}

	private IEnumerator Loop()
	{
		while (true)
		{
			yield return new WaitForSeconds(5f);
			if (_fuelCar == null)
			{
				if (_locomotive is SteamLocomotive steamLocomotive)
				{
					_fuelCar = steamLocomotive.FuelCar();
				}
				else
				{
					_fuelCar = _locomotive;
				}
				if (_fuelCar == null)
				{
					Log.Warning("Couldn't find fuel car for {locomotive}", _locomotive);
					continue;
				}
			}
			List<LoadSlot> loadSlots = _fuelCar.Definition.LoadSlots;
			for (int i = 0; i < loadSlots.Count; i++)
			{
				LoadSlot loadSlot = loadSlots[i];
				string requiredLoadIdentifier = loadSlot.RequiredLoadIdentifier;
				if (requiredLoadIdentifier == null)
				{
					continue;
				}
				CarLoadInfo? loadInfo = _fuelCar.GetLoadInfo(i);
				if (!loadInfo.HasValue)
				{
					continue;
				}
				float num = loadInfo.Value.Quantity / loadSlot.MaximumCapacity;
				if (_lastPercentByLoad.TryGetValue(requiredLoadIdentifier, out var value))
				{
					bool flag = true;
					float[] thresholds = _thresholds;
					foreach (float num2 in thresholds)
					{
						flag = flag && num > num2;
						if (!(value <= num2) && !(num > num2))
						{
							Log.Debug("AutoEngineerFuelAlerter {locomotive}, {loadId}, {threshold}, {last}, {current}", _locomotive, requiredLoadIdentifier, num2, value, num);
							Load load = TrainController.Shared.carPrototypeLibrary.LoadForId(requiredLoadIdentifier);
							Say($"Running low on {load.description}, {num * 100f:N0}% remaining.");
							PostNotice(requiredLoadIdentifier, load, num2);
							break;
						}
					}
					if (flag)
					{
						UnpostNotice(requiredLoadIdentifier);
					}
				}
				_lastPercentByLoad[requiredLoadIdentifier] = num;
			}
		}
	}

	private void Say(string text)
	{
		GetComponent<AutoEngineerPlanner>().Say(text);
	}

	private static string NoticeKeyFor(LoadCategory category)
	{
		return category switch
		{
			LoadCategory.Fuel => "ai-fuel", 
			LoadCategory.Water => "ai-h2o", 
			_ => throw new ArgumentOutOfRangeException("category", category, null), 
		};
	}

	private static LoadCategory LoadCategoryForLoadId(string loadId)
	{
		return loadId switch
		{
			"water" => LoadCategory.Water, 
			"coal" => LoadCategory.Fuel, 
			"diesel-fuel" => LoadCategory.Fuel, 
			_ => throw new ArgumentOutOfRangeException("loadId", loadId, null), 
		};
	}

	private void PostNotice(string loadId, Load load, float threshold)
	{
		string content = $"{load.description} below {Mathf.RoundToInt(threshold * 100f)}%";
		LoadCategory loadCategory = LoadCategoryForLoadId(loadId);
		string key = NoticeKeyFor(loadCategory);
		_locomotive.PostNotice(key, content);
		_postedNotice[loadCategory] = true;
	}

	private void UnpostNotice(string loadId)
	{
		LoadCategory loadCategory = LoadCategoryForLoadId(loadId);
		if (_postedNotice.TryGetValue(loadCategory, out var value) && value)
		{
			string key = NoticeKeyFor(loadCategory);
			_locomotive.PostNotice(key, null);
			_postedNotice[loadCategory] = false;
		}
	}
}
