using Model.Ops.Definition;

namespace Model.Ops;

public class LoadExporter : IndustryComponent
{
	public Load load;

	public float minimumLoad = 1f;

	public override void Service(IIndustryContext ctx)
	{
		foreach (IOpsCar item in EnumerateCars(ctx))
		{
			if (item.QuantityOfLoad(load).quantity > minimumLoad && (!item.Waybill.HasValue || item.Waybill.Value.Destination.Equals(this)))
			{
				ctx.OrderAwayLoaded(item);
			}
		}
	}

	public override void OrderCars(IIndustryContext ctx)
	{
	}
}
