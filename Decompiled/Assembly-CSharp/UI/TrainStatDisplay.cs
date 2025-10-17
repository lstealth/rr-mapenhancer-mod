using Model;
using Model.Physics;
using UnityEngine;

namespace UI;

public abstract class TrainStatDisplay : MonoBehaviour
{
	protected abstract void SetGauges(float milesPerHour, float mainResPsi, float eqResPsi, float brakeCylPsi, float brakePipePsi);

	private void Update()
	{
		BaseLocomotive selectedLocomotive = TrainController.Shared.SelectedLocomotive;
		if (selectedLocomotive != null)
		{
			UpdateForLocomotive(selectedLocomotive);
		}
		else
		{
			ResetGauges();
		}
	}

	private void UpdateForLocomotive(BaseLocomotive locomotive)
	{
		if (locomotive != null && locomotive.air is LocomotiveAirSystem loco)
		{
			float velocityMphAbs = locomotive.VelocityMphAbs;
			UpdateForLocomotive(velocityMphAbs, loco);
		}
		else
		{
			ResetGauges();
		}
	}

	private void UpdateForLocomotive(float mph, LocomotiveAirSystem loco)
	{
		SetGauges(mph, loco.MainReservoir.Pressure, loco.trainBrakePressure, loco.BrakeCylinder.Pressure, loco.BrakeLine.Pressure);
	}

	private void ResetGauges()
	{
		SetGauges(0f, 0f, 0f, 0f, 0f);
	}
}
