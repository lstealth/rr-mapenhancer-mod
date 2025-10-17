using System.Linq;

namespace Track.Signals;

internal static class Extensions
{
	public static string KeyForSwitchKnob(this CTCPanelGroup group)
	{
		return CTCKeys.Knob(group.GetComponentsInChildren<CTCPanelKnob>().First((CTCPanelKnob k) => k.name == "Switch").knobId);
	}

	public static string KeyForSignalKnob(this CTCPanelGroup group)
	{
		return CTCKeys.Knob(group.GetComponentsInChildren<CTCPanelKnob>().First((CTCPanelKnob k) => k.name == "Signal").knobId);
	}

	public static string KeyForCodeButton(this CTCPanelGroup group)
	{
		return CTCKeys.Button(group.GetComponentsInChildren<CTCPanelButton>().First((CTCPanelButton k) => k.name == "CTC Button").id);
	}
}
