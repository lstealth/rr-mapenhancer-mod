public interface IPickable
{
	float MaxPickDistance { get; }

	int Priority { get; }

	TooltipInfo TooltipInfo { get; }

	PickableActivationFilter ActivationFilter { get; }

	void Activate(PickableActivateEvent evt);

	void Deactivate();
}
