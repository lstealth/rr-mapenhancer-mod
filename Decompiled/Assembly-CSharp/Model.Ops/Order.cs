using JetBrains.Annotations;
using Model.Ops.Definition;

namespace Model.Ops;

public struct Order : IOrder
{
	public CarTypeFilter CarTypeFilter { get; }

	[CanBeNull]
	public Load Load { get; set; }

	public OpsCarPosition Destination { get; }

	public int CarCount { get; set; }

	public string Tag { get; }

	public bool NoPayment { get; }

	public Order(CarTypeFilter carTypeFilter, [CanBeNull] Load load, OpsCarPosition destination, int carCount, string tag, bool noPayment)
	{
		CarTypeFilter = carTypeFilter;
		Load = load;
		Destination = destination;
		CarCount = carCount;
		Tag = tag;
		NoPayment = noPayment;
	}
}
