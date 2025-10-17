using RollingStock;
using UnityEngine;

namespace Model.AI;

public abstract class AutoEngineerComponentBase : MonoBehaviour
{
	protected BaseLocomotive _locomotive;

	private AutoEngineerPlanner _planner;

	protected AutoEngineerConfig _config;

	protected AutoEngineerPlanner Planner
	{
		get
		{
			if (_planner == null)
			{
				_planner = GetComponent<AutoEngineerPlanner>();
			}
			return _planner;
		}
	}

	private void Awake()
	{
		_locomotive = GetComponent<BaseLocomotive>();
		_config = TrainController.Shared.autoEngineerConfig;
	}

	protected void Say(string text)
	{
		Planner.Say(text);
	}

	public abstract void ApplyMovement(MovementInfo info);

	public abstract void WillMove();
}
