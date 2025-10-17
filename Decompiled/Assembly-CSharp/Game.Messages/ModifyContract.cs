using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[MinimumAccessLevel(AccessLevel.Officer)]
[MessagePackObject(false)]
public struct ModifyContract : IGameMessage
{
	[Key(0)]
	public string Id;

	[Key(1)]
	public int Tier;

	public ModifyContract(string id, int tier)
	{
		Id = id;
		Tier = tier;
	}
}
