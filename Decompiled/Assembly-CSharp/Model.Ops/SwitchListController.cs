using System;
using System.Collections.Generic;
using System.Linq;
using Game;
using Game.Messages;
using Game.State;
using Network;
using Serilog;
using UnityEngine;

namespace Model.Ops;

public class SwitchListController : MonoBehaviour
{
	public OpsController opsController;

	private readonly Dictionary<string, List<IOpsCar>> _switchLists = new Dictionary<string, List<IOpsCar>>();

	public void RequestWaybillsForArea(IPlayer sender, string trainCrewId, string areaId)
	{
		Area area = opsController.Areas.First((Area a) => a.identifier == areaId);
		RequestWaybillsForArea(sender, area);
	}

	public void SetSwitchListCarIds(string trainCrewId, IEnumerable<string> carIds, bool send)
	{
		List<IOpsCar> list = new List<IOpsCar>();
		foreach (string carId in carIds)
		{
			IOpsCar opsCar = opsController.CarForId(carId);
			if (opsCar == null)
			{
				Log.Warning("{trainCrewId} Can't find ops car for {carId}, will be omitted from switch list", trainCrewId, carId);
			}
			else
			{
				list.Add(opsCar);
			}
		}
		_switchLists[trainCrewId] = list;
		if (send)
		{
			SendSwitchListUpdate(trainCrewId, list);
		}
	}

	public void ToggleSwitchListCarIds(string trainCrewId, List<string> carIds, bool on)
	{
		if (!_switchLists.TryGetValue(trainCrewId, out var value))
		{
			value = new List<IOpsCar>();
		}
		if (on)
		{
			foreach (string carId in carIds)
			{
				if (value.FindIndex((IOpsCar car) => car.Id == carId) < 0)
				{
					value.Add(opsController.CarForId(carId));
				}
			}
		}
		else
		{
			foreach (string carId2 in carIds)
			{
				int num = value.FindIndex((IOpsCar car) => car.Id == carId2);
				if (num >= 0)
				{
					value.RemoveAt(num);
				}
			}
		}
		_switchLists[trainCrewId] = value;
		SendSwitchListUpdate(trainCrewId, value);
	}

	private void RequestWaybillsForArea(IPlayer sender, Area area)
	{
		string text = StateManager.Shared.PlayersManager.TrainCrewIdFor(sender.PlayerId);
		if (!_switchLists.ContainsKey(text))
		{
			_switchLists[text] = new List<IOpsCar>();
		}
		List<IOpsCar> list = _switchLists[text];
		int count = list.Count;
		foreach (IOpsCar item in opsController.CarsInArea(area))
		{
			Waybill? waybill = item.Waybill;
			if (waybill.HasValue && !waybill.Value.Completed && !list.Contains(item))
			{
				list.Add(item);
			}
		}
		Log.Debug("RequestWaybillsForJob: {trainCrewId}, {before}, {after}", text, count, list.Count);
		SendSwitchListUpdate(text, list);
		int num = list.Count - count;
		string message = ((num > 0) ? $"Added {num} {area.name} cars to switch list." : ((!SwitchListContainsCarForArea(list, area)) ? ("No work in " + area.name + " today.") : ("No more work in " + area.name + " today.")));
		Multiplayer.SendError(sender, message);
	}

	private bool SwitchListContainsCarForArea(List<IOpsCar> switchList, Area area)
	{
		foreach (IOpsCar @switch in switchList)
		{
			OpsCarPosition? opsCarPosition = opsController.PositionForCar(@switch);
			if (opsCarPosition.HasValue && area.Contains(opsCarPosition.Value))
			{
				return true;
			}
		}
		return false;
	}

	private void SendSwitchListUpdate(string trainCrewId, List<IOpsCar> opsCars)
	{
		try
		{
			SwitchList switchList = new SwitchList(SwitchListEntriesFromOpsCars(opsCars));
			StateManager.ApplyLocal(new SwitchListUpdate(trainCrewId, switchList));
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Error SwitchListUpdate");
		}
	}

	private static List<SwitchList.Entry> SwitchListEntriesFromOpsCars(List<IOpsCar> opsCars)
	{
		return opsCars.Select((IOpsCar opsCar) => new SwitchList.Entry(opsCar.Id)).ToList();
	}

	private void RemoveCompletedFromSwitchLists()
	{
		foreach (string item in _switchLists.Keys.ToList())
		{
			List<IOpsCar> list = _switchLists[item];
			if (list.Exists((IOpsCar c) => c.Waybill?.Completed ?? false))
			{
				list = list.Where(delegate(IOpsCar c)
				{
					Waybill? waybill = c.Waybill;
					return waybill.HasValue && !waybill.GetValueOrDefault().Completed;
				}).ToList();
				_switchLists[item] = list;
				SendSwitchListUpdate(item, list);
			}
		}
	}

	public void RemoveCar(string carId)
	{
		foreach (KeyValuePair<string, List<IOpsCar>> switchList in _switchLists)
		{
			switchList.Deconstruct(out var key, out var value);
			string trainCrewId = key;
			List<IOpsCar> list = value;
			int num = list.FindIndex((IOpsCar c) => c.Id == carId);
			if (num >= 0)
			{
				list.RemoveAt(num);
				SendSwitchListUpdate(trainCrewId, list);
			}
		}
	}

	public void SendSwitchListUpdate(string trainCrewId)
	{
		if (!_switchLists.TryGetValue(trainCrewId, out var value))
		{
			Log.Debug("SendSwitchListUpdate: {trainCrewId} not found, sending empty switch list.", trainCrewId);
			SendSwitchListUpdate(trainCrewId, new List<IOpsCar>());
		}
		else
		{
			SendSwitchListUpdate(trainCrewId, value);
		}
	}

	public void PopulateSnapshot(ref Snapshot snapshot)
	{
		snapshot.SwitchLists = new Dictionary<string, SwitchList>();
		foreach (var (key, opsCars) in _switchLists)
		{
			snapshot.SwitchLists[key] = new SwitchList(SwitchListEntriesFromOpsCars(opsCars));
		}
	}

	public void RestoreSwitchLists(Dictionary<string, SwitchList> switchLists)
	{
		_switchLists.Clear();
		foreach (var (text2, switchList2) in switchLists)
		{
			try
			{
				SetSwitchListCarIds(text2, switchList2.Entries.Select((SwitchList.Entry entry) => entry.CarId), send: false);
			}
			catch (Exception exception)
			{
				Log.Error(exception, "Exception while restoring switch list for crew {trainCrewId}", text2);
			}
		}
	}
}
