using Model.Ops.Definition;

namespace Model.Ops;

public struct ReturnFromBardoOrder : IOrder
{
	public readonly string CarId;

	public CarTypeFilter CarTypeFilter { get; }

	public Load Load { get; }

	public OpsCarPosition Destination { get; }

	public int CarCount { get; set; }

	public string Tag { get; }

	public bool NoPayment { get; }

	public ReturnFromBardoOrder(string carId)
	{
		CarId = carId;
		CarTypeFilter = new CarTypeFilter("");
		Load = null;
		Destination = default(OpsCarPosition);
		CarCount = 1;
		Tag = null;
		NoPayment = true;
	}
}
