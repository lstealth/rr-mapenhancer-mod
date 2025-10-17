using Game.Messages;
using Model.Definition;

namespace Model;

public static class CarPropertyChanges
{
	public static void SetHandbrake(this Car car, bool apply)
	{
		car.SendPropertyChange(PropertyChange.Control.Handbrake, apply);
	}

	public static bool SupportsBleed(this Car car)
	{
		return car.Archetype switch
		{
			CarArchetype.LocomotiveDiesel => false, 
			CarArchetype.LocomotiveSteam => false, 
			CarArchetype.Tender => false, 
			_ => true, 
		};
	}

	public static void SetBleed(this Car car)
	{
		car.SendPropertyChange(PropertyChange.Control.Bleed, value: true);
	}
}
