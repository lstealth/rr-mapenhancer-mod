namespace Game.AccessControl;

public enum AccessLevel
{
	Banned = -10,
	Undetermined = 0,
	Passenger = 10,
	Crew = 20,
	Dispatcher = 30,
	Trainmaster = 40,
	Officer = 50,
	President = 60
}
