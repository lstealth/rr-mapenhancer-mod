using Track.Signals;
using Track.Signals.Panel;
using UnityEngine;

namespace UI.Console.Commands;

[ConsoleCommandHandler("ctc", null)]
public class CTCCommand
{
	private static CTCPanelController Panel => Object.FindObjectOfType<CTCPanelController>();

	[ConsoleSubcommand(null, "Resets the system, clearing blocks and routes.")]
	private static string Reset()
	{
		CTCPanelController panel = Panel;
		panel.ClearAllRoutes();
		panel.ClearAllBlocks();
		return "Cleared all routes.";
	}

	[ConsoleSubcommand(null, "Removes all markers from the CTC panel.")]
	private static string ClearMarkers()
	{
		CTCPanelMarkerManager cTCPanelMarkerManager = Object.FindObjectOfType<CTCPanelMarkerManager>();
		if (cTCPanelMarkerManager == null)
		{
			return "Couldn't find marker manager.";
		}
		cTCPanelMarkerManager.RemoveAllMarkers();
		return "Done.";
	}
}
