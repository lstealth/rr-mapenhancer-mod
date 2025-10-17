using Game.Messages;
using Model;
using Model.AI;
using Track;
using UI.EngineControls;

namespace Game.Scripting;

public class ScriptBaseLocomotive : ScriptCar
{
	private readonly BaseLocomotive _locomotive;

	private readonly LocomotiveControlHelper _controls;

	public ScriptCar fuel_car
	{
		get
		{
			if (!(_locomotive is SteamLocomotive steamLocomotive))
			{
				return this;
			}
			Car car = steamLocomotive.FuelCar();
			if (!(car == _locomotive))
			{
				return car.ScriptCar();
			}
			return this;
		}
	}

	public float independent_brake
	{
		get
		{
			return _controls.LocomotiveBrake;
		}
		set
		{
			_controls.LocomotiveBrake = value;
		}
	}

	public float train_brake
	{
		get
		{
			return _controls.TrainBrake;
		}
		set
		{
			_controls.TrainBrake = value;
		}
	}

	public float reverser
	{
		get
		{
			return _controls.Reverser;
		}
		set
		{
			_controls.Reverser = value;
		}
	}

	public float throttle
	{
		get
		{
			return _controls.Throttle;
		}
		set
		{
			_controls.Throttle = value;
		}
	}

	public bool bell
	{
		get
		{
			return _controls.Bell;
		}
		set
		{
			_controls.Bell = value;
		}
	}

	public float horn
	{
		get
		{
			return _controls.Horn;
		}
		set
		{
			_controls.Horn = value;
		}
	}

	private AutoEngineerOrdersHelper AutoEngineerOrdersHelper => new AutoEngineerOrdersHelper(persistence: new AutoEngineerPersistence(base.Car.KeyValueObject), locomotive: base.Car);

	private AutoEngineer AutoEngineer => base.Car.GetComponent<AutoEngineer>();

	internal ScriptBaseLocomotive(BaseLocomotive locomotive)
		: base(locomotive)
	{
		_locomotive = locomotive;
		_controls = new LocomotiveControlHelper(_locomotive);
	}

	public void set_control_manual()
	{
		AutoEngineerOrdersHelper.SetOrdersValue(AutoEngineerMode.Off);
	}

	public void set_control_ae_road(int direction, int speed)
	{
		AutoEngineerOrdersHelper.SetOrdersValue(AutoEngineerMode.Road, direction >= 0, speed);
	}

	public void set_control_ae_yard(int direction, int speed, float distance)
	{
		AutoEngineerOrdersHelper.SetOrdersValue(AutoEngineerMode.Yard, direction >= 0, speed, distance);
	}

	public void set_control_ae_waypoint(ScriptLocation location, int speed, string coupleCarId = null)
	{
		AutoEngineerOrdersHelper autoEngineerOrdersHelper = AutoEngineerOrdersHelper;
		AutoEngineerMode? mode = AutoEngineerMode.Waypoint;
		int? maxSpeedMph = speed;
		(Location, string)? maybeWaypoint = (location.Location, coupleCarId);
		autoEngineerOrdersHelper.SetOrdersValue(mode, null, maxSpeedMph, null, maybeWaypoint);
	}

	public float get_ae_target_speed_mph()
	{
		return AutoEngineer.ContextualTargetVelocity() * 2.23694f;
	}
}
