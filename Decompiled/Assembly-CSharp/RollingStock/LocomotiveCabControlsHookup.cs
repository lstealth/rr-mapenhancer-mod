using RollingStock.ContinuousControls;
using UnityEngine;

namespace RollingStock;

public class LocomotiveCabControlsHookup : MonoBehaviour
{
	public IGauge speedometer;

	public IGauge mainReservoir;

	public IGauge brakeCylinder;

	public IGauge brakePipe;

	public IGauge equalizingReservoir;

	public ContinuousControl locomotiveBrake;

	public ContinuousControl trainBrake;

	public ContinuousControl cutout;

	public ContinuousControl bell;

	public ContinuousControl regulator;

	public ContinuousControl johnsonBar;

	public IGauge boilerPressure;

	public ContinuousControl throttle;

	public ContinuousControl reverser;

	public ContinuousControl horn;
}
