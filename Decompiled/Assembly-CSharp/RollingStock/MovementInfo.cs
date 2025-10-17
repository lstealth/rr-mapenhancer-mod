namespace RollingStock;

public readonly struct MovementInfo
{
	public readonly float DeltaTime;

	public readonly float Distance;

	public readonly float TractiveEffort;

	public static readonly MovementInfo Zero;

	public MovementInfo(float deltaTime, float distance, float tractiveEffort)
	{
		DeltaTime = deltaTime;
		Distance = distance;
		TractiveEffort = tractiveEffort;
	}
}
