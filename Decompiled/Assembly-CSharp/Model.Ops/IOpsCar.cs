using Model.Ops.Definition;

namespace Model.Ops;

public interface IOpsCar
{
	string Id { get; }

	string CarType { get; }

	string DisplayName { get; }

	bool IsOwnedByPlayer { get; }

	int WeightInTons { get; }

	Waybill? Waybill { get; }

	PassengerMarker? PassengerMarker { get; set; }

	float Condition { get; }

	bool IsEmptyOrContains(Load load);

	(float quantity, float capacity) QuantityOfLoad(Load load);

	float Unload(Load load, float quantityToConsume);

	float Load(Load load, float quantityToLoad);

	bool IsFull(Load load);

	void SetWaybill(Waybill? waybill, IndustryComponent setter, string reason);

	bool GetOverrideDestination(OverrideDestination overrideDestination, out OpsCarPosition destination, out string tag);
}
