namespace Track.Signals;

internal static class CTCExtensions
{
	public static SwitchSetting CTCSwitchSetting(this TrackNode node)
	{
		if (!node.isThrown)
		{
			return SwitchSetting.Normal;
		}
		return SwitchSetting.Reversed;
	}
}
