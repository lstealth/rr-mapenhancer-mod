namespace RollingStock.Steam;

public interface ISteamLocomotiveSubcomponent
{
	void ApplyDistanceMoved(MovementInfo info, float driverVelocity, float absReverser, float absThrottle, float driverPhase);
}
