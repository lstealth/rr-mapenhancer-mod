using System.Linq;
using JetBrains.Annotations;
using Model.Ops.Definition;
using UnityEngine;

namespace Model.Ops;

public class LoadImporter : IndustryComponent
{
	[CanBeNull]
	public Load load;

	[Range(0f, 100f)]
	public int desiredCarCount = 1;

	[Range(0f, 100f)]
	public int maxOrderCount = 1;

	public IndustryComponent forwardToOnArrival;

	public override void OrderCars(IIndustryContext ctx)
	{
		if (!base.Industry.ShouldOrderCars())
		{
			return;
		}
		int num = EnumerateCars(ctx).Count();
		int i = ctx.NumberOfCarsOnOrder(load);
		for (float num2 = (float)desiredCarCount * base.Industry.GetContractMultiplier(); (float)(num + i) < num2; i++)
		{
			if (!ctx.OrderLoad(carTypeFilter, load, null, noPayment: false, out var _))
			{
				break;
			}
		}
	}

	public override void Service(IIndustryContext ctx)
	{
	}

	protected override void OnCompleteWaybill(IIndustryContext ctx, IOpsCar car, Waybill waybill)
	{
		base.OnCompleteWaybill(ctx, car, waybill);
		if (forwardToOnArrival != null)
		{
			int graceDays = OpsController.Shared.CalculateGraceDays(this, forwardToOnArrival);
			Waybill value = new Waybill(ctx.Now, this, forwardToOnArrival, 0, completed: false, null, graceDays);
			car.SetWaybill(value, this, "OnComplete forwardToOnArrival");
		}
	}
}
