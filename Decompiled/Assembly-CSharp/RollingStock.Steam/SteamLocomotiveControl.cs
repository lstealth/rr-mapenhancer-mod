using Model;
using Model.Physics;

namespace RollingStock.Steam;

public class SteamLocomotiveControl : LocomotiveControlAdapter
{
	public SteamLocomotive locomotive;

	private SteamEngine engine => locomotive.engine;

	public override int ThrottleInputNotches => 0;

	public override int ThrottleValueSteps => 100;

	public override float NormalizedTractiveEffort => engine.NormalizedTractiveEffort;

	public override float AbstractReverser
	{
		get
		{
			return engine.reverser;
		}
		set
		{
			engine.reverser = value;
		}
	}

	public override float AbstractThrottle
	{
		get
		{
			return engine.regulator;
		}
		set
		{
			engine.regulator = value;
		}
	}

	public float Reverser
	{
		get
		{
			return engine.reverser;
		}
		set
		{
			engine.reverser = value;
		}
	}

	public float Regulator
	{
		get
		{
			return engine.regulator;
		}
		set
		{
			engine.regulator = value;
		}
	}
}
