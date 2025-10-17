using Game.AccessControl;
using MessagePack;

namespace Game.Messages;

[MinimumAccessLevel(AccessLevel.Officer)]
[MessagePackObject(false)]
public struct ProgressionStartPhase : IGameMessage
{
	[Key(0)]
	public string SectionIdentifier { get; set; }

	[Key(1)]
	public int PhaseIndex { get; set; }

	public ProgressionStartPhase(string sectionIdentifier, int phaseIndex)
	{
		SectionIdentifier = sectionIdentifier;
		PhaseIndex = phaseIndex;
	}
}
