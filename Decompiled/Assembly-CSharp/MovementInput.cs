using UI;

public static class MovementInput
{
	public static float CalculateSpeedFromInput(float normalSpeed, float fastSpeed, float superSpeed)
	{
		GameInput shared = GameInput.shared;
		if (shared.ModifierVeryFast)
		{
			return superSpeed;
		}
		if (!shared.ModifierRun)
		{
			return normalSpeed;
		}
		return fastSpeed;
	}
}
