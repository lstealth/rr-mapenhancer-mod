using System.Collections.Generic;
using System.Linq;
using Game.Messages;

namespace Game.State;

public class TrainCrew
{
	public string Id;

	public string Name;

	public string Description;

	public HashSet<PlayerId> MemberPlayerIds;

	public string TimetableSymbol;

	public TrainCrew(Snapshot.TrainCrew snapshot)
	{
		Id = snapshot.Id;
		Name = snapshot.Name;
		Description = snapshot.Description;
		MemberPlayerIds = snapshot.MemberPlayerIds.Select((string id) => new PlayerId(id)).ToHashSet();
		TimetableSymbol = snapshot.TimetableSymbol;
	}

	public Snapshot.TrainCrew ToSnapshot()
	{
		return new Snapshot.TrainCrew(Id, Name, MemberPlayerIds.Select((PlayerId id) => id.String).ToHashSet(), Description, TimetableSymbol);
	}
}
