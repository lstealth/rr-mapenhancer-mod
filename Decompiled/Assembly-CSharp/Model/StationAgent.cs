using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core;
using Game.State;
using Model.Ops;
using UI.StationWindow;
using UI.SwitchList;
using UnityEngine;

namespace Model;

public class StationAgent : MonoBehaviour, IPickable
{
	[SerializeField]
	private Area area;

	[SerializeField]
	private List<Area> secondaryAreas;

	[SerializeField]
	private PassengerStop passengerStop;

	private readonly OpsCarList _freightCarList = new OpsCarList();

	private Coroutine _cacheUpdateCoroutine;

	public float MaxPickDistance => 50f;

	public int Priority => 0;

	public TooltipInfo TooltipInfo => new TooltipInfo(TooltipTitle(), TooltipText());

	public PickableActivationFilter ActivationFilter => PickableActivationFilter.PrimaryOnly;

	private void OnEnable()
	{
		if (area != null)
		{
			_cacheUpdateCoroutine = StartCoroutine(CacheUpdateCoroutine());
		}
	}

	private void OnDisable()
	{
		if (_cacheUpdateCoroutine != null)
		{
			StopCoroutine(_cacheUpdateCoroutine);
		}
		_cacheUpdateCoroutine = null;
	}

	public void Activate(PickableActivateEvent evt)
	{
		if (_freightCarList == null)
		{
			UpdateCachedFreightCarList();
		}
		StationWindow.Shared.Show(TooltipTitle(), ShownFreightAreas().ToList(), passengerStop, _freightCarList);
	}

	public void Deactivate()
	{
	}

	private string TooltipTitle()
	{
		if (area != null)
		{
			return area.name + " Station Agent";
		}
		if (passengerStop != null)
		{
			return passengerStop.DisplayName + " Agent";
		}
		return "Agent";
	}

	private string TooltipText()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("<sprite name=\"MouseLeft\"> Open Window");
		string value = PassengerSummary();
		if (!string.IsNullOrEmpty(value))
		{
			stringBuilder.AppendLine(value);
		}
		string value2 = FreightSummary();
		if (!string.IsNullOrEmpty(value2))
		{
			stringBuilder.AppendLine(value2);
		}
		return stringBuilder.ToString().TrimEnd();
	}

	private string FreightSummary()
	{
		if (area == null)
		{
			return null;
		}
		if (_freightCarList == null)
		{
			return "(Freight summary not available)";
		}
		return _freightCarList.Entries.Count.Pluralize("active freight waybill") + " in area";
	}

	private string PassengerSummary()
	{
		if (passengerStop == null)
		{
			return null;
		}
		int num = 0;
		foreach (KeyValuePair<string, PassengerStop.WaitingInfo> item in passengerStop.Waiting)
		{
			item.Deconstruct(out var _, out var value);
			PassengerStop.WaitingInfo waitingInfo = value;
			num += waitingInfo.Total;
		}
		return num.Pluralize("passenger") + " waiting";
	}

	private IEnumerator CacheUpdateCoroutine()
	{
		yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(1, 5));
		PlayersManager playersManager = StateManager.Shared.PlayersManager;
		while (true)
		{
			if (playersManager.IsPlayerCameraNear(base.transform, 500f))
			{
				UpdateCachedFreightCarList();
				yield return new WaitForSecondsRealtime(60f);
			}
			else
			{
				yield return new WaitForSecondsRealtime(5f);
			}
		}
	}

	private void UpdateCachedFreightCarList()
	{
		IEnumerable<IOpsCar> source = FreightCarsForArea();
		_freightCarList.Rebuild(source.Select((IOpsCar c) => c.Id));
		_freightCarList.SortByPositionDestination();
	}

	private IEnumerable<IOpsCar> FreightCarsForArea()
	{
		if (area == null)
		{
			return Array.Empty<IOpsCar>();
		}
		OpsController opsController = OpsController.Shared;
		return ShownFreightAreas().SelectMany((Area a) => opsController.CarsInArea(a)).Where(delegate(IOpsCar car)
		{
			Waybill? waybill = car.Waybill;
			return waybill.HasValue && !waybill.Value.Completed;
		});
	}

	private IEnumerable<Area> ShownFreightAreas()
	{
		yield return area;
		foreach (Area secondaryArea in secondaryAreas)
		{
			yield return secondaryArea;
		}
	}
}
