using System.Collections.Generic;
using Game.Persistence;

namespace Game.State;

public class PlayerRecordsClientManager
{
	public Dictionary<PlayerId, PlayerRecord> PlayerRecords { get; set; }
}
