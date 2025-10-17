using System.Collections.Generic;
using Game.AccessControl;
using Game.State;
using MessagePack;

namespace Game.Messages;

[HostOnlyAuthorizationRule]
[MessagePackObject(false)]
public struct LedgerResponse : IGameMessage
{
	[Key(0)]
	public List<SerializableLedgerEntry> Entries;

	[Key(1)]
	public int StartBalance;

	[Key(2)]
	public int EndBalance;

	public LedgerResponse(List<SerializableLedgerEntry> entries, int startBalance, int endBalance)
	{
		Entries = entries;
		StartBalance = startBalance;
		EndBalance = endBalance;
	}
}
