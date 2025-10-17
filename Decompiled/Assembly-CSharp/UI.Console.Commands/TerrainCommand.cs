using Cameras;
using Game;
using Map.Runtime;
using Track;

namespace UI.Console.Commands;

[ConsoleCommand("/terrain", null)]
public class TerrainCommand : IConsoleCommand
{
	private static MapManager MapManager => MapManager.Instance;

	public string Execute(string[] comps)
	{
		if (comps.Length < 2)
		{
			return "Usage: /terrain <rebuild|density>";
		}
		switch (comps[1])
		{
		case "rebuild":
			MapManager.RebuildAll();
			break;
		case "density":
			if (comps.Length == 4)
			{
				float treeDensity = float.Parse(comps[2]);
				float detailDensity = float.Parse(comps[3]);
				MapCameraUpdater.SetTerrainDensityValues(treeDensity, detailDensity);
			}
			return $"Tree: {Preferences.GraphicsTreeDensity:F1}, Detail: {Preferences.GraphicsDetailDensity:F1}";
		case "stresstest":
		{
			TrainController shared = TrainController.Shared;
			MapManager.Instance.logStats = true;
			int speed = int.Parse(comps[2]);
			Location location = new Location(shared.graph.GetSegment("S8rj"), 0f, TrackSegment.End.A);
			CameraSelector.shared.FollowTrack(location, speed);
			break;
		}
		}
		return null;
	}
}
