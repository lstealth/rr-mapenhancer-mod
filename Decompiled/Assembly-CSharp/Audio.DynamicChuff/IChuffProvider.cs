using RollingStock.Steam;

namespace Audio.DynamicChuff;

public interface IChuffProvider : ISteamLocomotiveSubcomponent
{
	IDynamicChuffDelegate Delegate { get; set; }

	void Configure(float driverDiameter, float normalizedEngineSize);
}
