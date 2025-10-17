namespace Game.AccessControl;

public enum AuthorizationRequirement
{
	HostOnly = 0,
	PlayerIdKey = 1,
	MinimumLevelPassenger = 10,
	MinimumLevelCrew = 11,
	MinimumLevelDispatcher = 12,
	MinimumLevelTrainmaster = 13,
	MinimumLevelOfficer = 14,
	MinimumLevelPresident = 15
}
