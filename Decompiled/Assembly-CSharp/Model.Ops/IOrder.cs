using JetBrains.Annotations;
using Model.Ops.Definition;

namespace Model.Ops;

public interface IOrder
{
	CarTypeFilter CarTypeFilter { get; }

	[CanBeNull]
	Load Load { get; }

	OpsCarPosition Destination { get; }

	int CarCount { get; set; }

	string Tag { get; }

	bool NoPayment { get; }
}
