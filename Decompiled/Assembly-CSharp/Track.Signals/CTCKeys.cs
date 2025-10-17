namespace Track.Signals;

public static class CTCKeys
{
	public static string Knob(string knobId)
	{
		return "knob:" + knobId + ":position";
	}

	public static string BlockOccupancy(string blockId)
	{
		return "block:" + blockId + ":occupancy";
	}

	public static string BlockTrafficFilter(string blockId)
	{
		return "block:" + blockId + ":direction";
	}

	public static string SignalAspect(string signalId)
	{
		return "signal:" + signalId + ":aspect";
	}

	public static string SwitchPosition(string nodeId)
	{
		return "switch:" + nodeId + ":position";
	}

	public static string Button(string buttonId)
	{
		return "button:" + buttonId + ":active";
	}

	public static string InterlockingDirection(string interlockingId)
	{
		return "il:" + interlockingId + ":direction";
	}
}
