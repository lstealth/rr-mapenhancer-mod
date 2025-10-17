using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[MinimumAccessLevel(AccessLevel.Trainmaster)]
[MessagePackObject(false)]
public struct RequestOps : IGameMessage
{
	public enum Command
	{
		Sweep,
		Step
	}

	[Key(0)]
	public Command command;

	[Key(1)]
	public string query;

	public RequestOps(Command command, string query)
	{
		this.command = command;
		this.query = query;
	}
}
