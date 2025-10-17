using Game.AccessControl;

namespace Game.Events;

public struct AccessLevelDidChange
{
	public AccessLevel OldAccessLevel { get; private set; }

	public AccessLevel NewAccessLevel { get; private set; }

	public AccessLevelDidChange(AccessLevel oldAccessLevel, AccessLevel accessLevel)
	{
		OldAccessLevel = oldAccessLevel;
		NewAccessLevel = accessLevel;
	}
}
