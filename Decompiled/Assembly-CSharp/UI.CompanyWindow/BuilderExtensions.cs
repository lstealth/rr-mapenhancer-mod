using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Game;
using Game.Events;
using Game.Messages;
using Game.State;
using Helpers;
using Model;
using Model.Definition;
using Model.Ops;
using Model.Ops.Timetable;
using UI.Builder;
using UnityEngine;

namespace UI.CompanyWindow;

public static class BuilderExtensions
{
	private static PlayersManager PlayersManager => StateManager.Shared.PlayersManager;

	public static void AddConditionField(this UIPanelBuilder builder, Car car)
	{
		StringBuilder sb = new StringBuilder(128);
		string message = ((car.RepairCap < 1f) ? "Overhaul needed; schedule overhaul service to fully repair\nby setting an Overhaul service destination." : "Damaged equipment may perform poorly and pay less.\nRepairs may be made at engine service facilities.");
		builder.AddField("Condition", ConditionText, UIPanelBuilder.Frequency.Periodic).Tooltip("Condition", message);
		string ConditionText()
		{
			sb.Clear();
			sb.Append($"{Mathf.RoundToInt(car.Condition * 100f)}%");
			float repairCap = car.RepairCap;
			if (repairCap < 1f)
			{
				sb.Append(string.Format(" ({0}% max repair {1})", (int)(repairCap * 100f), "<sprite name=Warning>"));
			}
			if (car.HasHotbox)
			{
				sb.Append(" <sprite name=\"Flame\"> Hotbox!");
			}
			if (car.IsDerailed)
			{
				sb.Append(" Derailed!");
			}
			return sb.ToString();
		}
	}

	public static void AddMileageField(this UIPanelBuilder builder, Car car)
	{
		if (car.IsOwnedByPlayer)
		{
			builder.AddField("Overhaul", delegate
			{
				float num = (float)Car.OverhaulMiles * 1.609344f - (car.OdometerService - car.LastOverhaulOdometer);
				return (num <= 0f) ? (DistanceString(Mathf.CeilToInt(0f - num)) + " Past Due <sprite name=Warning>") : ("Due in " + DistanceString(num));
			}, UIPanelBuilder.Frequency.Periodic).Tooltip(delegate
			{
				string text = ((car.LastOverhaulOdometer == 0f) ? "Never" : (DistanceString(car.OdometerService - car.LastOverhaulOdometer) + " ago"));
				string text2 = "Service Odometer: " + DistanceString(car.OdometerService) + "\nLast Overhaul: " + text + "\nService miles are miles adjusted for wear and tear and (for engines) heavy use.";
				return new TooltipInfo("Overhaul", text2);
			});
		}
		else
		{
			builder.AddField("Miles on Railroad", () => $"{Mathf.FloorToInt(car.OdometerActual * 0.6213712f):N0} mi", UIPanelBuilder.Frequency.Periodic);
		}
		static string DistanceString(float km)
		{
			return $"{Mathf.FloorToInt(km * 0.6213712f):N0} mi";
		}
	}

	public static void AddTrainCrewDropdown(this UIPanelBuilder builder, string tooltipMessage, string trainCrewId, bool canEdit, Action<string> onSelectTrainCrewId)
	{
		TimetableController timetableController = TimetableController.Shared;
		IReadOnlyList<TrainCrew> trainCrews = PlayersManager.TrainCrews;
		List<string> trainCrewChoices = new List<string> { "None" };
		trainCrewChoices.AddRange(trainCrews.Select((TrainCrew crew) => timetableController.TryGetTrainForTrainCrew(crew, out var timetableTrain) ? (crew.Name + " (Train " + timetableTrain.DisplayStringShort + ")") : crew.Name));
		int index = FindIndex(trainCrews, trainCrewId);
		builder.AddField("Train Crew", builder.HStack(delegate(UIPanelBuilder uIPanelBuilder)
		{
			RectTransform rectTransform = ((!canEdit) ? uIPanelBuilder.AddLabel(trainCrewChoices[index + 1]) : uIPanelBuilder.AddDropdown(trainCrewChoices, index + 1, delegate(int i)
			{
				string obj = ((i > 0 && i <= trainCrews.Count) ? trainCrews[i - 1].Id : null);
				onSelectTrainCrewId?.Invoke(obj);
			}));
			rectTransform.FlexibleWidth();
			uIPanelBuilder.AddButtonCompact("Show", delegate
			{
				LinkDispatcher.Open(EntityType.Crew, trainCrewId);
			}).Width(60f);
		})).Tooltip("Train Crew", tooltipMessage);
	}

	public static bool AddTrainCrewDropdown(this UIPanelBuilder builder, Car car)
	{
		CarArchetype archetype = car.Archetype;
		if (!archetype.IsLocomotive() && archetype != CarArchetype.Caboose && !car.IsPassengerCar())
		{
			return false;
		}
		builder.RebuildOnEvent<CarTrainCrewChanged>();
		builder.RebuildOnEvent<TrainCrewsDidChange>();
		builder.AddTrainCrewDropdown("Set a train crew to associate a car with a specific job.", car.trainCrewId, StateManager.CheckAuthorizedToSendMessage(new SetCarTrainCrew(car.id, null)), delegate(string proposedTrainCrewId)
		{
			string trainCrewId = car.trainCrewId;
			if (!(proposedTrainCrewId == trainCrewId))
			{
				StateManager.ApplyLocal(new SetCarTrainCrew(car.id, proposedTrainCrewId));
			}
		});
		return true;
	}

	private static int FindIndex(IReadOnlyList<TrainCrew> trainCrews, string id)
	{
		for (int i = 0; i < trainCrews.Count; i++)
		{
			if (trainCrews[i].Id == id)
			{
				return i;
			}
		}
		return -1;
	}

	public static RectTransform AddDropdownIntPicker(this UIPanelBuilder builder, List<int> values, int selected, Func<int, string> toString, bool canWrite, Action<int> onSelect)
	{
		if (!values.Contains(selected))
		{
			values.Add(selected);
		}
		int num = values.IndexOf(selected);
		List<string> list = values.Select(toString).ToList();
		if (!canWrite)
		{
			return builder.AddLabel(list[num]);
		}
		return builder.AddDropdown(list, num, delegate(int newIndex)
		{
			int obj = values[newIndex];
			onSelect(obj);
		});
	}

	private static IEnumerable<IndustryComponent> EnumerateAvailableIndustryComponents(OpsController opsController, Car car)
	{
		foreach (Area area in opsController.Areas)
		{
			foreach (Industry item in area.Industries.Where((Industry i) => !i.ProgressionDisabled))
			{
				IEnumerable<IndustryComponent> enumerable = item.UniqueVisibleComponents.Where((IndustryComponent ic) => ic.carTypeFilter.Matches(car.CarType));
				foreach (IndustryComponent item2 in enumerable)
				{
					yield return item2;
				}
			}
		}
	}

	public static void AddRepairDestination(this UIPanelBuilder builder, Car car)
	{
		OpsController shared = OpsController.Shared;
		List<(IndustryComponent, string)> industryComponentOptions = (from ic in EnumerateAvailableIndustryComponents(shared, car)
			where ic is RepairTrack
			select ic).Cast<RepairTrack>().SelectMany(delegate(RepairTrack rt)
		{
			List<(IndustryComponent, string)> list = new List<(IndustryComponent, string)> { (rt, null) };
			if (rt.canOverhaul && Car.WearFeature)
			{
				list.Add((rt, "overhaul"));
			}
			return list;
		}).ToList();
		builder.PopulateOverrideDestination(OverrideDestination.Repair, industryComponentOptions, shared, car);
	}

	public static void AddSellDestination(this UIPanelBuilder builder, Car car)
	{
		builder.AddField("Sell Value", EquipmentPurchase.TradeInValueForCar(car).ToString("C0"));
		OpsController shared = OpsController.Shared;
		List<IndustryComponent> industryComponentOptions = (from ic in EnumerateAvailableIndustryComponents(shared, car)
			where ic is Interchange
			select ic).ToList();
		builder.AddSellDestination(industryComponentOptions, shared, car);
	}

	private static void PopulateOverrideDestination(this UIPanelBuilder builder, OverrideDestination overrideDestination, List<(IndustryComponent ic, string tag)> industryComponentOptions, OpsController opsController, Car car)
	{
		builder.AddObserver(car.KeyValueObject.Observe(overrideDestination.Key(), delegate
		{
			builder.Rebuild();
		}, callInitial: false));
		List<DropdownLocationPickerRowData> values = new List<DropdownLocationPickerRowData>();
		List<(IndustryComponent ic, string tag)> dropdownIndustryComponents = new List<(IndustryComponent, string)>();
		AddDropdownItem("Not Set", "Clear Selection", null, null);
		foreach (var industryComponentOption in industryComponentOptions)
		{
			string text = ((overrideDestination != OverrideDestination.Repair) ? industryComponentOption.ic.DisplayName : (industryComponentOption.ic.DisplayName + DisplaySuffixForRepairTag(industryComponentOption.tag)));
			string title = text;
			AddDropdownItem(title, industryComponentOption.ic.GetComponentInParent<Area>().name, industryComponentOption.ic, industryComponentOption.tag);
		}
		int num;
		try
		{
			num = (car.TryGetOverrideDestination(overrideDestination, opsController, out var overrideInfo) ? dropdownIndustryComponents.FindIndex(delegate((IndustryComponent ic, string tag) tuple)
			{
				var (industryComponent, text2) = tuple;
				if (industryComponent == null)
				{
					return false;
				}
				(OpsCarPosition, string) value = overrideInfo.Value;
				return industryComponent.Identifier == value.Item1.Identifier && text2 == value.Item2;
			}) : 0);
		}
		catch
		{
			num = 0;
		}
		if (overrideDestination == OverrideDestination.Repair)
		{
			string text = "Repair Destination";
			string label = text;
			if (EquipmentPurchase.CarCanBeSold(car) && !overrideDestination.IsWriteAuthorized(car))
			{
				builder.AddField(label, values[num].Title);
				return;
			}
			if (overrideDestination == OverrideDestination.Repair)
			{
				text = "Select a repair facility to send this equipment to. <b>Overhaul</b> repairs the equipment to its original condition.";
				string prompt = text;
				RectTransform control = builder.AddLocationPicker(prompt, values, num, delegate(int index)
				{
					(IndustryComponent, string) tuple = dropdownIndustryComponents[index];
					Car car2 = car;
					OverrideDestination type = overrideDestination;
					(OpsCarPosition, string)? tuple3;
					if (!(tuple.Item1 == null))
					{
						(IndustryComponent, string) tuple2 = tuple;
						tuple3 = (tuple2.Item1, tuple2.Item2);
					}
					else
					{
						tuple3 = null;
					}
					car2.SetOverrideDestination(type, tuple3);
					builder.Rebuild();
				});
				builder.AddField(label, control);
				return;
			}
			throw new ArgumentOutOfRangeException("overrideDestination", overrideDestination, null);
		}
		throw new ArgumentOutOfRangeException("overrideDestination", overrideDestination, null);
		void AddDropdownItem(string title2, string subtitle, IndustryComponent ic, string tag)
		{
			values.Add(new DropdownLocationPickerRowData(title2, subtitle));
			dropdownIndustryComponents.Add((ic, tag));
		}
		static string DisplaySuffixForRepairTag(string tag)
		{
			if (tag == "overhaul")
			{
				return " (Overhaul)";
			}
			return "";
		}
	}

	private static void AddSellDestination(this UIPanelBuilder builder, List<IndustryComponent> industryComponentOptions, OpsController opsController, Car car)
	{
		builder.AddObserver(car.KeyValueObject.Observe("ops.waybill", delegate
		{
			builder.Rebuild();
		}, callInitial: false));
		List<DropdownLocationPickerRowData> values = new List<DropdownLocationPickerRowData>();
		List<(IndustryComponent ic, string tag)> dropdownIndustryComponents = new List<(IndustryComponent, string)>();
		AddDropdownItem("Not Set", "Clear Selection", null, null);
		foreach (IndustryComponent industryComponentOption in industryComponentOptions)
		{
			string displayName = industryComponentOption.DisplayName;
			string name = industryComponentOption.GetComponentInParent<Area>().name;
			AddDropdownItem(displayName, name, industryComponentOption, "sell");
		}
		int num = 0;
		Waybill? waybill = car.GetWaybill(opsController);
		if (waybill.HasValue)
		{
			Waybill waybill2 = waybill.GetValueOrDefault();
			num = dropdownIndustryComponents.FindIndex(delegate((IndustryComponent ic, string tag) tuple)
			{
				var (industryComponent, text) = tuple;
				if (industryComponent == null)
				{
					return false;
				}
				return industryComponent.Identifier == waybill2.Destination.Identifier && text == waybill2.Tag;
			});
			if (num < 0)
			{
				num = 0;
			}
		}
		if (!StateManager.CheckAuthorizedToChangeProperty(car.id, "ops.waybill"))
		{
			builder.AddField("Sell Destination", values[num].Title);
			return;
		}
		RectTransform control = builder.AddLocationPicker("Select an interchange to send this equipment to for sale. The equipment will be sold when the interchange is served.", values, num, delegate(int index)
		{
			IndustryComponent item = dropdownIndustryComponents[index].ic;
			if (item == null)
			{
				car.SetWaybillAuto(null, opsController);
			}
			else
			{
				car.SetWaybill(new Waybill(TimeWeather.Now, null, item, 0, completed: false, "sell", 0));
			}
			builder.Rebuild();
		});
		builder.AddField("Sell Destination", control);
		void AddDropdownItem(string title, string subtitle, IndustryComponent ic, string tag)
		{
			values.Add(new DropdownLocationPickerRowData(title, subtitle));
			dropdownIndustryComponents.Add((ic, tag));
		}
	}
}
