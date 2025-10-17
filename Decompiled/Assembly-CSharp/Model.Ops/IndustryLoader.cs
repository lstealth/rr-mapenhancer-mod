using System.Collections.Generic;
using System.Linq;
using Track;
using UnityEngine;

namespace Model.Ops;

public class IndustryLoader : IndustryLoaderBase
{
	[Tooltip("Rate at which each car is loaded, in units per day. Overall loading is only limited by storage.")]
	public float carLoadRate = 1f;

	public bool orderAwayLoaded = true;

	public override bool WantsAutoDestination(AutoDestinationType type)
	{
		if (type == AutoDestinationType.Empty)
		{
			return !orderEmpties;
		}
		return false;
	}

	public override void Service(IIndustryContext ctx)
	{
		float contractMultiplier = base.Industry.GetContractMultiplier();
		float rate = productionRate * contractMultiplier;
		float rate2 = carLoadRate * contractMultiplier;
		float num = 0f;
		ctx.AddToStorage(load, IndustryComponent.RateToValue(rate, ctx.DeltaTime), maxStorage);
		float num2 = ctx.QuantityInStorage(load);
		foreach (IOpsCar item in (from car in EnumerateCars(ctx, requireWaybill: true)
			where car.IsEmptyOrContains(load)
			orderby car.QuantityOfLoad(load).quantity descending
			select car).ToList())
		{
			float quantityToLoad = Mathf.Min(num2, IndustryComponent.RateToValue(rate2, ctx.DeltaTime));
			float num3 = item.Load(load, quantityToLoad);
			if (item.IsFull(load))
			{
				if (orderAwayLoaded)
				{
					ctx.OrderAwayLoaded(item);
				}
				else
				{
					item.SetWaybill(null, this, "Full");
				}
			}
			ctx.RemoveFromStorage(load, num3);
			num2 -= num3;
			num += num3;
		}
	}

	public override IEnumerable<PanelField> PanelFields(IndustryContext ctx)
	{
		if (base.Industry.GetContractMultiplier() < 0.001f)
		{
			yield break;
		}
		if (!load.importable)
		{
			int num = base.TrackSpans.Sum((TrackSpan ts) => Mathf.FloorToInt(ts.Length * 3.28084f / 50f));
			float f = Mathf.Min(productionRate, carLoadRate * (float)num) / load.NominalQuantityPerCarLoad;
			yield return new PanelField("Production", $"{load.description} @ {Mathf.FloorToInt(f)} cars/day", "Car loads produced");
		}
		if (!orderEmpties)
		{
			float quantityInStorage = ctx.QuantityInStorage(load);
			yield return PanelField.InStorage(load, quantityInStorage, maxStorage);
		}
	}
}
