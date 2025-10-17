namespace Effects;

public static class HeadlightStateLogic
{
	private static HeadlightController.State StateFromBits(int value)
	{
		if (value >= 2)
		{
			return HeadlightController.State.On;
		}
		if (value != 1)
		{
			return HeadlightController.State.Off;
		}
		return HeadlightController.State.Dim;
	}

	public static (HeadlightController.State, HeadlightController.State) StatesFromInt(int intValue)
	{
		HeadlightController.State item = StateFromBits((intValue >> 2) & 3);
		return (StateFromBits(intValue & 3), item);
	}

	public static int IntFromStates(HeadlightController.State forward, HeadlightController.State rear)
	{
		return (BitsForState(rear) << 2) | BitsForState(forward);
		static int BitsForState(HeadlightController.State state)
		{
			return state switch
			{
				HeadlightController.State.Off => 0, 
				HeadlightController.State.Dim => 1, 
				HeadlightController.State.On => 2, 
				_ => 0, 
			};
		}
	}
}
