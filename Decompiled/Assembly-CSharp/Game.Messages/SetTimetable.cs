using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[MinimumAccessLevel(AccessLevel.Officer)]
[MessagePackObject(false)]
public struct SetTimetable : IGameMessage
{
	[Key(0)]
	public string Source;

	public SetTimetable(string source)
	{
		Source = source;
	}
}
