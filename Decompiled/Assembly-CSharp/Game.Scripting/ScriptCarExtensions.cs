using Model;

namespace Game.Scripting;

public static class ScriptCarExtensions
{
	public static ScriptCar ScriptCar(this Car car)
	{
		if (car is BaseLocomotive locomotive)
		{
			return new ScriptBaseLocomotive(locomotive);
		}
		return new ScriptCar(car);
	}
}
