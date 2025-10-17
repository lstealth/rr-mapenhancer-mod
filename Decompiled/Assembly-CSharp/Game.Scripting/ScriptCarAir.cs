using Model;

namespace Game.Scripting;

public class ScriptCarAir
{
	private readonly Car _car;

	public float brake_cylinder => _car.air.BrakeCylinder.Pressure;

	public float brake_line => _car.air.BrakeLine.Pressure;

	internal ScriptCarAir(Car car)
	{
		_car = car;
	}
}
