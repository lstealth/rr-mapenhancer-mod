namespace Track.Search;

public struct HeuristicCosts
{
	public int DivergingRoute;

	public int ThrowSwitch;

	public int ThrowSwitchCTCLocked;

	public int CarBlockingRoute;

	public static HeuristicCosts Zero => default(HeuristicCosts);

	public static HeuristicCosts AutoEngineer => new HeuristicCosts
	{
		DivergingRoute = 20,
		ThrowSwitch = 10,
		ThrowSwitchCTCLocked = 1000,
		CarBlockingRoute = 5000
	};
}
