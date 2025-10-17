using System;

public static class PickableActivationFilterExtensions
{
	public static bool Accepts(this PickableActivationFilter filter, PickableActivation activation)
	{
		return filter switch
		{
			PickableActivationFilter.Any => true, 
			PickableActivationFilter.PrimaryOnly => activation == PickableActivation.Primary, 
			PickableActivationFilter.SecondaryOnly => activation == PickableActivation.Secondary, 
			_ => throw new ArgumentOutOfRangeException("filter", filter, null), 
		};
	}
}
