using System.Collections.Generic;
using System.Linq;
using Game;
using Model.Ops.Definition;
using Serilog;
using UnityEngine;

namespace Model.Ops;

public class IndustryUnloader : IndustryComponent
{
	public Load load;

	[Tooltip("Units of load unloaded per game day.")]
	public float carUnloadRate = 1f;

	[Tooltip("Units of storage consumed per game day.")]
	public float storageConsumptionRate = 1f;

	public float maxStorage = 1f;

	public bool orderAwayEmpties = true;

	public bool orderLoads = true;

	private bool _warnedConsumption;

	private string KeyUnloadedTotal => "unloaded-total-" + load.id;

	public override bool WantsAutoDestination(AutoDestinationType type)
	{
		if (type == AutoDestinationType.Load)
		{
			return !orderLoads;
		}
		return false;
	}

	public override void Initialize(IIndustryContext ctx, GameVersion fromVersion)
	{
		if (fromVersion.IsZero || (load.id == "repair-parts" && fromVersion < GameVersion.V2024_4_0))
		{
			float contractMultiplier = base.Industry.GetContractMultiplier();
			float num = maxStorage * contractMultiplier;
			ctx.AddToStorage(load, num * 0.25f, num);
		}
	}

	public override void Service(IIndustryContext ctx)
	{
		float contractMultiplier = base.Industry.GetContractMultiplier();
		float num = maxStorage * contractMultiplier;
		float zeroThreshold = load.ZeroThreshold;
		float num2 = IndustryComponent.RateToValue(contractMultiplier * carUnloadRate, ctx.DeltaTime);
		if (num2 < zeroThreshold)
		{
			if (!_warnedConsumption && contractMultiplier > 0f)
			{
				Log.Warning("Industry {industry} {qtyRemainingToConsume} is less than {zeroThreshold}, this will be corrected each tick", base.Identifier, num2, zeroThreshold);
				_warnedConsumption = true;
			}
			num2 = zeroThreshold * 2f;
		}
		float num3 = 0f;
		foreach (IOpsCar item in (from car in EnumerateCars(ctx, requireWaybill: true)
			where car.IsEmptyOrContains(load)
			orderby car.QuantityOfLoad(load).quantity
			select car).ToList())
		{
			float num4 = Mathf.Min(num2, Mathf.Max(0f, num - ctx.QuantityInStorage(load)));
			if (num4 < zeroThreshold)
			{
				break;
			}
			float num5 = item.Unload(load, num4);
			if (num5 < zeroThreshold)
			{
				if (orderAwayEmpties)
				{
					ctx.OrderAwayEmpty(item);
				}
				else if (item.Waybill.Value.Completed)
				{
					item.SetWaybill(null, this, "Empty completed");
				}
			}
			ctx.AddToStorage(load, num5, num);
			num2 -= num5;
			num3 += num5;
		}
		if (load.payPerQuantity > 0f)
		{
			ctx.CounterIncrement(KeyUnloadedTotal, num3);
		}
		ctx.RemoveFromStorage(load, IndustryComponent.RateToValue(contractMultiplier * storageConsumptionRate, ctx.DeltaTime));
	}

	public override void DailyReceivables(GameDateTime now, IIndustryContext ctx)
	{
		float num = ctx.CounterIncrement(KeyUnloadedTotal, 0f);
		if (!(num < 1f))
		{
			ctx.PayLoad(load, num);
			ctx.CounterClear(KeyUnloadedTotal);
		}
	}

	public override bool AcceptsCarsWithLoad(Load checkLoad)
	{
		return checkLoad == load;
	}

	public override void OrderCars(IIndustryContext ctx)
	{
		if (!orderLoads || !base.Industry.ShouldOrderCars())
		{
			return;
		}
		float contractMultiplier = base.Industry.GetContractMultiplier();
		float num = maxStorage * contractMultiplier;
		float num2 = ctx.QuantityOnOrder(load);
		float num3 = ctx.QuantityInStorage(load) + num2;
		float num4 = (num - num3) / 2f;
		float quantity;
		for (float num5 = 0f; num5 < num4; num5 += quantity)
		{
			if (!ctx.OrderLoad(carTypeFilter, load, null, noPayment: false, out quantity))
			{
				break;
			}
		}
	}

	public override IEnumerable<PanelField> PanelFields(IndustryContext ctx)
	{
		float contractMultiplier = base.Industry.GetContractMultiplier();
		if (!(contractMultiplier < 0.001f))
		{
			if (!load.importable)
			{
				float f = GetUnitsPerDay() / load.NominalQuantityPerCarLoad * contractMultiplier;
				yield return new PanelField("Consumes", $"{load.description} @ {Mathf.CeilToInt(f)} cars/day", "Car consumption rate");
			}
			if (!orderLoads)
			{
				float effectiveStorage = contractMultiplier * maxStorage;
				float quantityInStorage = ctx.QuantityInStorage(load);
				yield return PanelField.InStorage(load, quantityInStorage, effectiveStorage);
			}
		}
	}

	private float GetUnitsPerDay()
	{
		float result;
		if (storageConsumptionRate > 0f)
		{
			result = carUnloadRate;
		}
		else
		{
			FormulaicIndustryComponent component = base.Industry.GetComponent<FormulaicIndustryComponent>();
			if (component != null)
			{
				result = 0f;
				foreach (FormulaicIndustryComponent.Term inputTerm in component.inputTerms)
				{
					if (inputTerm.load == load)
					{
						result = inputTerm.unitsPerDay;
						break;
					}
				}
			}
			else
			{
				Log.Warning("Couldn't find formulaic for industry {ic}", this);
				result = 0f;
			}
		}
		return result;
	}
}
