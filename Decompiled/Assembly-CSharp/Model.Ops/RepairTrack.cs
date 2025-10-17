using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core;
using Game;
using Game.Messages;
using Game.Reputation;
using Game.State;
using KeyValue.Runtime;
using Model.Definition;
using Model.Ops.Definition;
using Network;
using TMPro;
using UI.Builder;
using UI.CompanyWindow;
using UnityEngine;

namespace Model.Ops;

public class RepairTrack : IndustryComponent
{
	private enum RepairGroup
	{
		Overhaul,
		NeedsRepair,
		None
	}

	private struct RepairRateState
	{
		public float PayRateMultiplier;

		public bool PaidCurrent;

		public float PayDue;

		public RepairRateState(float payRateMultiplier, bool paidCurrent, float payDue)
		{
			PayRateMultiplier = payRateMultiplier;
			PaidCurrent = paidCurrent;
			PayDue = payDue;
		}

		public static RepairRateState From(Value value)
		{
			return new RepairRateState(value["payRate"].FloatValueOrDefault(1f), value["paidCurr"].BoolValueOrDefault(defaultValue: true), value["payDue"].FloatValue);
		}

		public Value ToValue()
		{
			return Value.Dictionary(new Dictionary<string, Value>
			{
				{ "payRate", PayRateMultiplier },
				{ "paidCurr", PaidCurrent },
				{ "payDue", PayDue }
			});
		}
	}

	public const string TagOverhaul = "overhaul";

	private const float RepairPartLbsPerRepairUnit = 12000f;

	[SerializeField]
	private Load repairPartsLoad;

	public bool canOverhaul;

	private const float BasePayPerRepairUnit = 50f;

	private const float VerySmallFloat = 1E-06f;

	private const int RepairCapLevels = 5;

	private const float PercentPerCapLevel = 0.1f;

	private const float MaxPercentDropForCaps = 0.5f;

	private string RateStateKey => subIdentifier + "-rate";

	private RepairRateState RateState
	{
		get
		{
			return RepairRateState.From(base.Industry.KeyValueObject[RateStateKey]);
		}
		set
		{
			base.Industry.KeyValueObject[RateStateKey] = value.ToValue();
		}
	}

	private static Config Config => Config.Shared;

	public override void DailyPayables(GameDateTime now, IIndustryContext ctx)
	{
		RepairRateState rateState = RateState;
		StateManager shared = StateManager.Shared;
		if (rateState.PayDue < 1E-06f)
		{
			rateState.PayDue = 0f;
			rateState.PaidCurrent = true;
		}
		else
		{
			int num = Mathf.CeilToInt(rateState.PayDue);
			rateState.PaidCurrent = shared.CanAfford(num);
			if (rateState.PaidCurrent)
			{
				Multiplayer.Broadcast($"{Hyperlink.To(base.Industry)}: Paid {num:C0} wages for shop crew.");
				base.Industry.ApplyToBalance(-num, Ledger.Category.WagesRepair, null, 0, quiet: true);
				rateState.PayDue = 0f;
			}
			else
			{
				Multiplayer.Broadcast($"{Hyperlink.To(base.Industry)}: Unable to pay wages of {num:C0}; shop is closed for the day.");
			}
		}
		RateState = rateState;
	}

	private IEnumerable<Car> EnumerateCarsActual(IIndustryContext ctx)
	{
		TrainController trainController = TrainController.Shared;
		return from car in EnumerateCars(ctx)
			select trainController.CarForId(car.Id);
	}

	public override void Service(IIndustryContext ctx)
	{
		float payRate;
		float num = IndustryComponent.RateToValue(EffectiveRepairPerDayPerCar(out payRate), ctx.DeltaTime);
		if (num < 1E-06f)
		{
			return;
		}
		float num2 = ctx.QuantityInStorage(repairPartsLoad) / 12000f;
		float num3 = 0f;
		float num4 = 0f;
		foreach (Car item in (from car in EnumerateCarsActual(ctx).Where(NeedsRepair)
			orderby car.id
			select car).ToList())
		{
			float repairAvailable = Mathf.Min(num2, num);
			if (TickCar(ctx, item, repairAvailable, out var repairUsed))
			{
				num2 -= repairUsed;
				num4 += repairUsed;
				num3 += repairUsed * 12000f;
				if (num2 < 1E-06f)
				{
					break;
				}
			}
		}
		ctx.RemoveFromStorage(repairPartsLoad, num3);
		if (num4 > 1E-06f)
		{
			RepairRateState rateState = RateState;
			rateState.PayDue += CalculatePayForRepair(num4, payRate);
			RateState = rateState;
		}
		CheckForCompletelyRepairedCars(ctx);
	}

	private float CalculatePayForRepair(float repairUnits, float payRatePerRepairUnit)
	{
		float num = payRatePerRepairUnit * repairUnits;
		if (num < 1E-06f)
		{
			return 0f;
		}
		return num;
	}

	public void HandleSetMultiplier(float multiplier)
	{
		StateManager.AssertIsHost();
		RepairRateState rateState = RateState;
		rateState.PayRateMultiplier = multiplier;
		RateState = rateState;
	}

	private void CheckForCompletelyRepairedCars(IIndustryContext ctx)
	{
		foreach (Car item in EnumerateCarsActual(ctx))
		{
			if (!NeedsRepair(item))
			{
				float repairCap = item.RepairCap;
				if (item.Condition < repairCap)
				{
					item.SetCondition(repairCap);
				}
				if (TryGetRepairDestination(item, out var overrideTag))
				{
					item.SetOverrideDestination(OverrideDestination.Repair, null);
					string arg = ((overrideTag == "overhaul") ? "Overhaul" : "Repairs");
					Multiplayer.Broadcast($"{Hyperlink.To(base.Industry)}: {arg} completed on {Hyperlink.To(item)}.");
				}
			}
		}
	}

	private static bool TryGetRepairDestination(Car car, out string overrideTag)
	{
		if (!car.TryGetOverrideDestination(OverrideDestination.Repair, OpsController.Shared, out (OpsCarPosition, string)? result))
		{
			overrideTag = null;
			return false;
		}
		overrideTag = result.Value.Item2;
		return true;
	}

	private float EffectiveRepairPerDayPerCar(out float payRate)
	{
		float repairBonus = ReputationTracker.Shared.RepairBonus();
		RepairRateState rateState = RateState;
		if (!rateState.PaidCurrent)
		{
			payRate = 0f;
			return 0f;
		}
		GetRepairStats(repairBonus, rateState.PayRateMultiplier, out var repairPerDay, out payRate);
		return repairPerDay;
	}

	private static void GetRepairStats(float repairBonus, float payRateMultiplier, out float repairPerDay, out float payPerRepairUnit)
	{
		payPerRepairUnit = 1f / (1f + repairBonus) * 50f;
		repairPerDay = (1f + repairBonus) * payRateMultiplier;
	}

	public override void OrderCars(IIndustryContext ctx)
	{
	}

	private static bool NeedsRepair(Car car)
	{
		if (car.Condition < car.RepairCap - 0.001f)
		{
			return true;
		}
		if (car.NeedsOiling || car.HasHotbox)
		{
			return true;
		}
		if (!InForOverhaul(car))
		{
			return false;
		}
		if (OverhaulWorkRemaining(car, out var _) > 0.001f)
		{
			return true;
		}
		return car.OverhaulProgress > 0.001f;
	}

	private static bool InForOverhaul(Car car)
	{
		if (TryGetRepairDestination(car, out var overrideTag))
		{
			return overrideTag == "overhaul";
		}
		if (car.Archetype == CarArchetype.Tender && car.TryGetAdjacentCar(car.EndToLogical(Car.End.F), out var adjacent))
		{
			return InForOverhaul(adjacent);
		}
		return false;
	}

	private bool TickCar(IIndustryContext ctx, Car car, float repairAvailable, out float repairUsed)
	{
		car.OffsetOiled(1f);
		car.ControlProperties[PropertyChange.Control.Hotbox] = null;
		float carRepairSpeed = EquipmentRepairSpeed(car);
		float repairCap = car.RepairCap;
		float resultingCondition = car.Condition;
		if (InForOverhaul(car))
		{
			if (NeedsMinimalRepairBeforeOverhaul(car, out var minRepairTarget))
			{
				repairCap = minRepairTarget;
			}
			else
			{
				float shareOfFullOverhaulWork;
				float num = OverhaulWorkRemaining(car, out shareOfFullOverhaulWork);
				if (num > 0f)
				{
					repairUsed = Mathf.Min(num, repairAvailable);
					float num2 = repairUsed / num * shareOfFullOverhaulWork;
					car.OverhaulProgress += num2;
					return repairUsed > 1E-06f;
				}
				car.LastOverhaulOdometer = car.OdometerService;
				car.OverhaulProgress = 0f;
			}
		}
		CalculateRepairStep(resultingCondition, repairAvailable, repairCap, carRepairSpeed, car.Archetype, out resultingCondition, out repairUsed);
		if (repairUsed < 1E-06f)
		{
			return false;
		}
		car.SetCondition(resultingCondition);
		return true;
	}

	internal static void CalculateRepairStep(float condition, float repairAvailable, float repairCap, float carRepairSpeed, CarArchetype archetype, out float resultingCondition, out float repairUsed)
	{
		Config config = Config;
		AnimationCurve animationCurve = ((archetype != CarArchetype.LocomotiveSteam) ? config.workPerPercentForCondition : config.workPerPercentForConditionSteam);
		AnimationCurve animationCurve2 = animationCurve;
		float num = Mathf.Clamp01(repairCap - condition);
		repairUsed = 0f;
		while (num > 1E-06f && repairAvailable > 1E-06f)
		{
			float num2 = animationCurve2.Evaluate(condition) / (100f * carRepairSpeed);
			float num3 = 0.01f;
			if (num2 > repairAvailable)
			{
				num3 *= repairAvailable / num2;
				num2 = repairAvailable;
			}
			if (num3 > num)
			{
				num2 *= num / num3;
				num3 = num;
			}
			condition += num3;
			num -= num3;
			repairAvailable -= num2;
			repairUsed += num2;
		}
		resultingCondition = condition;
	}

	internal static float CalculateRepairWorkOverall(Car car)
	{
		float repairCap = car.RepairCap;
		if (!NeedsRepair(car))
		{
			return 0f;
		}
		float num = 0f;
		float repairCapOverride = repairCap;
		float value = car.Condition;
		if (InForOverhaul(car))
		{
			if (NeedsMinimalRepairBeforeOverhaul(car, out var minRepairTarget))
			{
				num += CalculateRepairWork(car, minRepairTarget);
				value = minRepairTarget;
			}
			num += OverhaulWorkRemaining(car, out var _);
			repairCapOverride = 1f;
		}
		return num + CalculateRepairWork(car, repairCapOverride, value);
	}

	private static float CalculateRepairWork(Car car, float repairCapOverride, float? startCondition = null)
	{
		CalculateRepairStep(startCondition ?? car.Condition, float.PositiveInfinity, repairCapOverride, EquipmentRepairSpeed(car), car.Archetype, out var _, out var repairUsed);
		return repairUsed;
	}

	private static float EquipmentRepairSpeed(Car car)
	{
		float time = NormalizedCostValue(car);
		return Config.repairSpeedForNormalizedCost.Evaluate(time);
	}

	private static float OverhaulWorkRemaining(Car car, out float shareOfFullOverhaulWork)
	{
		float overhaulProgress = car.OverhaulProgress;
		float num = Mathf.Max(0f, car.OdometerService - car.LastOverhaulOdometer);
		float num2 = Mathf.Min(1f, num * 0.6213712f / (float)Car.OverhaulMiles);
		if (num2 < 0.001f)
		{
			shareOfFullOverhaulWork = 0f;
			return 0f;
		}
		float num3 = WorkForFullOverhaul(car);
		shareOfFullOverhaulWork = 1f - overhaulProgress;
		return num2 * shareOfFullOverhaulWork * num3;
	}

	private static float WorkForFullOverhaul(Car car)
	{
		CalculateRepairStep(0f, float.PositiveInfinity, 1f, EquipmentRepairSpeed(car), car.Archetype, out var _, out var repairUsed);
		return repairUsed;
	}

	private static float NormalizedCostValue(Car car)
	{
		return Mathf.Max(0f, ((float)car.Definition.BasePrice - 1000f) / 34000f);
	}

	private static bool NeedsMinimalRepairBeforeOverhaul(Car car, out float minRepairTarget)
	{
		float condition = car.Condition;
		float num = 0.5f;
		if (condition >= num)
		{
			minRepairTarget = 0f;
			return false;
		}
		minRepairTarget = num;
		return true;
	}

	public static float RepairCapForKilometersSinceOverhaul(float kilometers)
	{
		int num = Mathf.FloorToInt(kilometers * 0.6213712f / (float)Car.OverhaulMiles);
		float num2 = Mathf.Min(0.5f, (float)num * 0.1f);
		return Mathf.Clamp01(1f - num2);
	}

	public override void BuildPanel(UIPanelBuilder builder)
	{
		builder.AddSection(this.ShortName(base.Industry), delegate(UIPanelBuilder builder2)
		{
			RepairRateState rateState = RateState;
			float payDue = rateState.PayDue;
			builder2.AddObserver(base.Industry.KeyValueObject.Observe(RateStateKey, delegate
			{
				builder2.Rebuild();
			}, callInitial: false));
			builder2.AddField("Wages Due", $"{payDue:C0}");
			if (!rateState.PaidCurrent)
			{
				builder2.AddField("Status", "<sprite name=Warning> Work Stopped Today: Unpaid Wages");
			}
			RectTransform control = builder2.AddDropdownIntPicker(new List<int> { 0, 1, 2, 3, 5, 10, 20 }, Mathf.RoundToInt(RateState.PayRateMultiplier), delegate(int intValue)
			{
				if (intValue <= 0)
				{
					return "Stop Work";
				}
				float num2 = (float)intValue * 50f;
				return $"{intValue}X\t{num2:C0}/day/car";
			}, StateManager.HasTrainmasterAccess, delegate(int newValue)
			{
				StateManager.ApplyLocal(new SetRepairMultiplier(base.Identifier, newValue));
			});
			builder2.AddField("Repair Speed", control).Tooltip("Repair Speed", "Higher pay yields faster repairs.");
			float num = ReputationTracker.Shared.RepairBonus();
			builder2.AddField("Reputation Bonus", $"{num * 100f:F0}%").Tooltip("Reputation Bonus", "Repair crews work faster when your railroad has a higher reputation.");
			BuildCars(builder2);
		});
	}

	private void BuildCars(UIPanelBuilder builder)
	{
		IndustryContext industryContext = this.CreateContext(TimeWeather.Now, 0f);
		IOrderedEnumerable<IGrouping<RepairGroup, Car>> carGroups = from @group in EnumerateCarsActual(industryContext).GroupBy(delegate(Car car)
			{
				if (InForOverhaul(car))
				{
					return RepairGroup.Overhaul;
				}
				return NeedsRepair(car) ? RepairGroup.NeedsRepair : RepairGroup.None;
			})
			orderby @group.Key
			select @group;
		builder.VStack(delegate(UIPanelBuilder uIPanelBuilder)
		{
			float repairRateMultiplier = RateState.PayRateMultiplier;
			foreach (IGrouping<RepairGroup, Car> item in carGroups)
			{
				RepairGroup repairGroup = item.Key;
				uIPanelBuilder.AddSection(repairGroup switch
				{
					RepairGroup.Overhaul => "Overhauling", 
					RepairGroup.NeedsRepair => "Repairing", 
					RepairGroup.None => "No Work", 
					_ => throw new ArgumentOutOfRangeException(), 
				});
				foreach (Car car in item.OrderBy((Car car2) => car2.SortName))
				{
					uIPanelBuilder.HStack(delegate(UIPanelBuilder uIPanelBuilder2)
					{
						uIPanelBuilder2.AddLabel(Hyperlink.To(car)).Width(130f);
						uIPanelBuilder2.AddLabel($"{Mathf.RoundToInt(car.Condition * 100f)}%").Width(60f).HorizontalTextAlignment(HorizontalAlignmentOptions.Right);
						string text = "";
						string title = "";
						if (repairGroup == RepairGroup.Overhaul)
						{
							float overhaulProgress = car.OverhaulProgress;
							if (overhaulProgress > 0f)
							{
								text = $"{(int)(overhaulProgress * 100f)}%";
								title = "Overhaul Progress";
							}
						}
						uIPanelBuilder2.AddLabel(text).Width(60f).HorizontalTextAlignment(HorizontalAlignmentOptions.Right)
							.Tooltip(title, null);
						if (repairGroup != RepairGroup.None)
						{
							float num = CalculateRepairWorkOverall(car);
							float num2 = num * 12000f * 0.0005f;
							string text2 = ((!(repairRateMultiplier > 0f)) ? "Never" : GameDateTimeInterval.DeltaStringMinutes((int)(num / repairRateMultiplier * 60f * 24f), GameDateTimeInterval.Style.Short));
							uIPanelBuilder2.AddLabel(text2).Width(80f).HorizontalTextAlignment(HorizontalAlignmentOptions.Right)
								.Tooltip("Time Remaining", "D:HH:MM or H:MM");
							uIPanelBuilder2.AddLabel($"{num2:F1}T").Width(80f).HorizontalTextAlignment(HorizontalAlignmentOptions.Right)
								.Tooltip("Repair Parts Needed", null);
						}
					});
				}
			}
		}).Padding(new RectOffset(20, 0, 0, 0));
	}

	public string DailyReportSummary(GameDateTime now)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(DisplayName + ": ");
		IndustryContext industryContext = this.CreateContext(TimeWeather.Now, 0f);
		List<Car> list = EnumerateCarsActual(industryContext).Where(NeedsRepair).ToList();
		if (list.Count > 0)
		{
			int num = Mathf.FloorToInt(list.Average((Car c) => c.Condition) * 100f);
			stringBuilder.Append(string.Format("{0} awaiting repair, average {1}%", list.Count.Pluralize("car"), num));
		}
		else
		{
			stringBuilder.Append("No cars awaiting repair");
		}
		stringBuilder.Append(".");
		float quantity = industryContext.QuantityInStorage(repairPartsLoad);
		stringBuilder.Append(" " + repairPartsLoad.QuantityString(quantity) + ".");
		return stringBuilder.ToString();
	}
}
