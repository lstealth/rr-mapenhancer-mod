using Game.State;

namespace Game.Events;

public struct GameModeDidChange
{
	public GameMode GameMode { get; }

	public GameModeDidChange(GameMode gameMode)
	{
		GameMode = gameMode;
	}
}
