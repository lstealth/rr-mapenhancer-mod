using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Game;
using JetBrains.Annotations;
using KeyValue.Runtime;
using Model.Ops.Definition;
using Track;
using UnityEngine;

namespace Model.Ops;

public static class CarCounter
{
	private class DayPlan
	{
		public readonly Dictionary<string, int> LoadOrders = new Dictionary<string, int>();

		public readonly Dictionary<string, int> EmptyOrders = new Dictionary<string, int>();

		public readonly HashSet<MockCar> Outbounds = new HashSet<MockCar>();
	}

	private static readonly string[] CarTypes = new string[8] { "XM", "HM", "HMR", "FB", "FL", "FM", "GB", "TM" };

	public static void Count(Industry industry, int tier, int days)
	{
		TimeWeather.TimeMultiplier = 1f;
		TimeWeather.Now = default(GameDateTime);
		StringBuilder sb = new StringBuilder();
		KeyValueStorage keyValueObject = new KeyValueStorage();
		industry.MockSetKeyValueObject(keyValueObject);
		industry.SetContract(new Contract(tier));
		Dictionary<string, CarTypeFilter> carTypeFilterForLoad = new Dictionary<string, CarTypeFilter>();
		Dictionary<string, int> dwellCountByComponent = new Dictionary<string, int>();
		Dictionary<string, int> dwellMaxByComponent = new Dictionary<string, int>();
		int inboundCount = 0;
		int outboundCount = 0;
		Dictionary<string, CarCounterIndustryContext> contexts = new Dictionary<string, CarCounterIndustryContext>();
		List<IndustryComponent> industryComponents = industry.Components.Where(ShouldSimulateComponent).ToList();
		Dictionary<string, float> loadQuantities = new Dictionary<string, float>();
		foreach (IndustryComponent item in industryComponents)
		{
			contexts[item.Identifier] = new CarCounterIndustryContext(21600f, loadQuantities);
		}
		Dictionary<string, float> carCapacityPerComponent = industryComponents.ToDictionary((IndustryComponent ic) => ic.Identifier, (IndustryComponent ic) => ic.trackSpans.Sum((TrackSpan span) => span.Length * 0.0656168f));
		float num = industry.GetComponentsInChildren<TrackSpan>().Sum((TrackSpan span) => span.Length * 0.0656168f);
		Dictionary<string, Dictionary<int, DayPlan>> planner = new Dictionary<string, Dictionary<int, DayPlan>>();
		Dictionary<string, Load> loadLookup = LoadLookupForIndustry(industry);
		int carCount = 0;
		foreach (IndustryComponent item2 in industryComponents)
		{
			CarCounterIndustryContext ctx = contexts[item2.Identifier];
			item2.Initialize(ctx, GameVersion.Current);
		}
		for (int num2 = 0; num2 < days; num2++)
		{
			RunDay(num2);
		}
		float num3 = (float)(inboundCount + outboundCount) / (float)days;
		float num4 = (float)dwellCountByComponent.Values.Sum() / (float)days;
		int num5 = dwellMaxByComponent.Values.Sum();
		Debug.Log($"Tier {tier}: cars per day: {num3:F2}, dwell avg {num4:F1}, max {num5} ({num:F1} cap)\n{sb}");
		MockCar CarForFilter(CarTypeFilter filter, [CanBeNull] Load load, float loadQuantity)
		{
			string carType = CarTypeForFilter(filter);
			string id = carCount.ToString();
			carCount++;
			return new MockCar(id, carType, load, loadQuantity);
		}
		DayPlan Plan(string componentIdentifier, int day)
		{
			if (!planner.TryGetValue(componentIdentifier, out var value))
			{
				planner.Add(componentIdentifier, value = new Dictionary<int, DayPlan>());
			}
			if (!value.TryGetValue(day, out var value2))
			{
				value.Add(day, value2 = new DayPlan());
			}
			return value2;
		}
		void PrintSummary(int day)
		{
			float num6 = (float)inboundCount / (float)(day + 1);
			float num7 = (float)outboundCount / (float)(day + 1);
			string text = string.Join(", ", loadQuantities.Select((KeyValuePair<string, float> kv) => $"{kv.Key}: {kv.Value:N1}"));
			sb.AppendLine($"Day {day + 1}/{days}: in={num6:F1}, out={num7:F1} = total={num6 + num7:F1} | {text}");
			foreach (IndustryComponent item3 in industryComponents)
			{
				if (item3.trackSpans.Length != 0)
				{
					string identifier = item3.Identifier;
					CarCounterIndustryContext carCounterIndustryContext = contexts[identifier];
					float num8 = (float)dwellCountByComponent[identifier] / (float)(day + 1);
					sb.AppendLine($"    {item3.subIdentifier}: {carCounterIndustryContext.Cars.Count} cars ({num8:F1} avg, {dwellMaxByComponent[identifier]} max, {carCapacityPerComponent[identifier]:F1} cap)");
				}
			}
		}
		void RunDay(int day)
		{
			RunPlan(day);
			foreach (IndustryComponent component in industryComponents)
			{
				CarCounterIndustryContext carCounterIndustryContext = contexts[component.Identifier];
				if (!dwellCountByComponent.TryGetValue(component.Identifier, out var value))
				{
					value = 0;
				}
				value += carCounterIndustryContext.Cars.Count;
				dwellCountByComponent[component.Identifier] = value;
				if (!dwellMaxByComponent.TryGetValue(component.Identifier, out var value2))
				{
					value2 = 0;
				}
				value2 = Mathf.Max(value2, carCounterIndustryContext.Cars.Count);
				dwellMaxByComponent[component.Identifier] = value2;
				List<DayPlan> source = (from kv in planner
					where kv.Key == component.Identifier
					select kv.Value).SelectMany((Dictionary<int, DayPlan> k) => k.Values).ToList();
				carCounterIndustryContext.EmptyPending = (from kv in source.SelectMany((DayPlan plan) => plan.EmptyOrders)
					group kv by kv.Key).ToDictionary((IGrouping<string, KeyValuePair<string, int>> g) => g.Key, (IGrouping<string, KeyValuePair<string, int>> g) => g.Sum((KeyValuePair<string, int> kv) => kv.Value));
				carCounterIndustryContext.LoadPending = (from kv in source.SelectMany((DayPlan plan) => plan.LoadOrders)
					group kv by kv.Key).ToDictionary((IGrouping<string, KeyValuePair<string, int>> g) => g.Key, (IGrouping<string, KeyValuePair<string, int>> g) => g.Sum((KeyValuePair<string, int> kv) => kv.Value));
				component.OrderCars(carCounterIndustryContext);
				ScheduleOrders(day + 1, carCounterIndustryContext, component.Identifier);
			}
			RunDayPart(day);
			RunDayPart(day);
			RunPlan(day);
			RunDayPart(day);
			RunDayPart(day);
			PrintSummary(day);
		}
		void RunDayPart(int day)
		{
			foreach (IndustryComponent item4 in industryComponents)
			{
				CarCounterIndustryContext ctx2 = contexts[item4.Identifier];
				item4.Service(ctx2);
			}
		}
		void RunPlan(int day)
		{
			GameDateTime now = TimeWeather.Now;
			foreach (KeyValuePair<string, Dictionary<int, DayPlan>> item5 in planner)
			{
				item5.Deconstruct(out var key, out var value);
				string industryId = key;
				Dictionary<int, DayPlan> dictionary = value;
				CarCounterIndustryContext carCounterIndustryContext = contexts[industryId];
				IndustryComponent industryComponent = industryComponents.First((IndustryComponent ic) => ic.Identifier == industryId);
				if (!dictionary.TryGetValue(day, out var value2))
				{
					break;
				}
				int value3;
				foreach (MockCar outbound in value2.Outbounds)
				{
					if (carCounterIndustryContext.Cars.Contains(outbound))
					{
						sb.AppendLine($"<---- Out: {outbound}");
						carCounterIndustryContext.Cars.Remove(outbound);
						value3 = outboundCount++;
					}
				}
				foreach (KeyValuePair<string, int> emptyOrder in value2.EmptyOrders)
				{
					emptyOrder.Deconstruct(out key, out value3);
					string queryString = key;
					int num6 = value3;
					for (int num7 = 0; num7 < num6; num7++)
					{
						MockCar mockCar = CarForFilter(new CarTypeFilter(queryString), null, 0f);
						mockCar.SetWaybill(new Waybill(now, null, industryComponent, 1, completed: false, null, 0), industryComponent, "");
						sb.AppendLine($"----> In: {mockCar} (MT)");
						carCounterIndustryContext.Cars.Add(mockCar);
						value3 = inboundCount++;
					}
				}
				foreach (KeyValuePair<string, int> loadOrder in value2.LoadOrders)
				{
					loadOrder.Deconstruct(out key, out value3);
					string key2 = key;
					int num8 = value3;
					Load load = loadLookup[key2];
					CarTypeFilter filter = carTypeFilterForLoad[key2];
					for (int num9 = 0; num9 < num8; num9++)
					{
						MockCar mockCar2 = CarForFilter(filter, load, load.NominalQuantityPerCarLoad);
						mockCar2.SetWaybill(new Waybill(now, null, industryComponent, 1, completed: false, null, 0), industryComponent, "");
						sb.AppendLine($"----> In: {mockCar2}");
						carCounterIndustryContext.Cars.Add(mockCar2);
						value3 = inboundCount++;
					}
				}
				float num10 = carCapacityPerComponent[industryId];
				int count = carCounterIndustryContext.Cars.Count;
				if (count > Mathf.CeilToInt(num10))
				{
					sb.AppendLine($"************ OVER CAPACITY {industryId}: {count} > {num10:F1} *************");
				}
				dictionary.Remove(day);
			}
		}
		void ScheduleOrders(int day, CarCounterIndustryContext carCounterIndustryContext, string componentIdentifier)
		{
			DayPlan dayPlan = Plan(componentIdentifier, day);
			string key;
			int value;
			foreach (KeyValuePair<string, int> emptyOrder2 in carCounterIndustryContext.EmptyOrders)
			{
				emptyOrder2.Deconstruct(out key, out value);
				string key2 = key;
				int num6 = value;
				if (!dayPlan.EmptyOrders.TryGetValue(key2, out var value2))
				{
					value2 = 0;
				}
				value2 += num6;
				dayPlan.EmptyOrders[key2] = value2;
			}
			carCounterIndustryContext.EmptyOrders.Clear();
			foreach (KeyValuePair<string, int> loadOrder2 in carCounterIndustryContext.LoadOrders)
			{
				loadOrder2.Deconstruct(out key, out value);
				string key3 = key;
				int num7 = value;
				carTypeFilterForLoad[key3] = carCounterIndustryContext.LoadOrderCarTypeFilters[key3];
				if (!dayPlan.LoadOrders.TryGetValue(key3, out var value3))
				{
					value3 = 0;
				}
				value3 += num7;
				dayPlan.LoadOrders[key3] = value3;
			}
			carCounterIndustryContext.LoadOrders.Clear();
			foreach (MockCar item6 in carCounterIndustryContext.OrderedAway)
			{
				dayPlan.Outbounds.Add(item6);
			}
			carCounterIndustryContext.OrderedAway.Clear();
		}
	}

	private static bool ShouldSimulateComponent(IndustryComponent ic)
	{
		if (!(ic is LoadImporter))
		{
			if (!(ic is LoadExporter))
			{
				if (!(ic is IndustryLoader { orderAwayLoaded: var orderAwayLoaded }))
				{
					if (!(ic is IndustryUnloader { orderAwayEmpties: var orderAwayEmpties }))
					{
						if (ic is FormulaicIndustryComponent)
						{
							return true;
						}
						throw new ArgumentException($"Unsupported industry type: {ic.GetType()}");
					}
					return orderAwayEmpties;
				}
				return orderAwayLoaded;
			}
			return false;
		}
		return false;
	}

	private static string CarTypeForFilter(CarTypeFilter carTypeFilter)
	{
		return CarTypes.FirstOrDefault(carTypeFilter.Matches) ?? throw new ArgumentException($"No car type for filter {carTypeFilter}");
	}

	private static Dictionary<string, Load> LoadLookupForIndustry(Industry industry)
	{
		HashSet<Load> hashSet = new HashSet<Load>();
		foreach (IndustryComponent component in industry.Components)
		{
			if (!(component is LoadImporter loadImporter))
			{
				if (!(component is LoadExporter loadExporter))
				{
					if (!(component is IndustryLoader industryLoader))
					{
						if (!(component is IndustryUnloader industryUnloader))
						{
							if (!(component is FormulaicIndustryComponent))
							{
								throw new ArgumentException($"Unsupported industry type: {industry.GetType()}");
							}
						}
						else
						{
							hashSet.Add(industryUnloader.load);
						}
					}
					else
					{
						hashSet.Add(industryLoader.load);
					}
				}
				else
				{
					hashSet.Add(loadExporter.load);
				}
			}
			else
			{
				hashSet.Add(loadImporter.load);
			}
		}
		return hashSet.ToDictionary((Load o) => o.id, (Load o) => o);
	}
}
