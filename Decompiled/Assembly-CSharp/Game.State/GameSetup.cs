namespace Game.State;

public struct GameSetup
{
	public string SaveName;

	public NewGameSetup? NewGameSetup;

	public GameSetup(string saveName)
	{
		SaveName = saveName;
		NewGameSetup = null;
	}

	public GameSetup(string saveName, NewGameSetup setup)
	{
		SaveName = saveName;
		NewGameSetup = setup;
	}
}
