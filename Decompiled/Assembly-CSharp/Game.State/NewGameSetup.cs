namespace Game.State;

public struct NewGameSetup
{
	public readonly string RailroadName;

	public readonly string ReportingMark;

	public readonly GameMode Mode;

	public readonly string ProgressionId;

	public readonly string SetupId;

	public NewGameSetup(string railroadName, string reportingMark, GameMode mode, string progressionId, string setupId)
	{
		RailroadName = railroadName;
		ReportingMark = reportingMark;
		Mode = mode;
		ProgressionId = progressionId;
		SetupId = setupId;
	}
}
