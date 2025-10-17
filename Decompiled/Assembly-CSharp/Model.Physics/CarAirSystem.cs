using System;
using Model.Definition;
using UnityEngine;

namespace Model.Physics;

public class CarAirSystem : MonoBehaviour
{
	public readonly Reservoir BrakeLine = new Reservoir("Brake Line", 0.6818f, 0f);

	public readonly Reservoir BrakeReservoir = new Reservoir("Brake Reservoir", 2.5f, 0f);

	public readonly Reservoir BrakeCylinder = new Reservoir("Brake Cylinder", 1f, 0f);

	public readonly AirConnection BrakeLineToReservoir = new AirConnection(Reservoir.Pipe.Feed);

	public readonly AirConnection ReservoirToCylinder = new AirConnection(Reservoir.Pipe.HalfInch);

	private readonly AirConnection CylinderToOutside = new AirConnection(Reservoir.Pipe.Feed);

	private readonly AirConnection BrakeLineConnectionA = new AirConnection(Reservoir.Pipe.Line);

	private readonly AirConnection BrakeLineConnectionB = new AirConnection(Reservoir.Pipe.Line);

	private readonly VentedValve TenderMainResToBrakeCylinder = new VentedValve(Reservoir.Pipe.HalfInch);

	private readonly VentedValve TenderMainResToMainRes = new VentedValve(Reservoir.Pipe.Line);

	[NonSerialized]
	public float brakePercent;

	[NonSerialized]
	public bool handbrakeApplied;

	[NonSerialized]
	public float exhaustFlow;

	[NonSerialized]
	private float airFlow;

	[NonSerialized]
	public float anglecockFlowA;

	[NonSerialized]
	public float anglecockFlowB;

	public Car car;

	public bool NeedsSend;

	public long LastSentTick;

	public const float AnglecockClosedThreshold = 0.01f;

	public bool bleedBrakeCylinder { get; private set; }

	private float anglecockA => car.EndGearA.AnglecockSetting;

	private float anglecockB => car.EndGearB.AnglecockSetting;

	public bool DefersToLocomotiveAir
	{
		get
		{
			LocomotiveAirSystem locomotiveAirSystem;
			return ShouldDeferToLocomotiveAir(out locomotiveAirSystem);
		}
	}

	private void Awake()
	{
		SetupReservoirs();
	}

	protected virtual void SetupReservoirs()
	{
	}

	public void FixedUpdateAir(float dt)
	{
		UpdateBrakeLine(dt);
		airFlow = 0f;
		exhaustFlow = 0f;
		for (int i = 0; i < 2; i++)
		{
			UpdateAir(dt / 2f);
		}
		UpdateBrakingForce();
		UpdateNeedsSend();
	}

	protected virtual void UpdateAir(float dt)
	{
		if (ShouldDeferToLocomotiveAir(out var locomotiveAirSystem))
		{
			if (this is LocomotiveAirSystem locomotiveAirSystem2)
			{
				Reservoir.Equalize(locomotiveAirSystem.MainReservoir, locomotiveAirSystem2.MainReservoir);
			}
			exhaustFlow += TenderMainResToBrakeCylinder.ValveVent(locomotiveAirSystem.MainReservoir, BrakeCylinder, locomotiveAirSystem.locomotiveBrakeControlLine, canValve: true, dt);
			return;
		}
		float pressure = BrakeReservoir.Pressure;
		float pressure2 = BrakeLine.Pressure;
		int num = ((pressure2 < pressure - 0.5f) ? 1 : 0);
		int num2 = ((pressure2 > pressure + 0.5f) ? 1 : 0);
		if (bleedBrakeCylinder)
		{
			bool flag = BrakeCylinder.Pressure > 0.1f;
			bleedBrakeCylinder = flag;
			if (bleedBrakeCylinder)
			{
				num = 1;
				num2 = 1;
			}
		}
		airFlow += ReservoirToCylinder.Equalize(BrakeReservoir, BrakeCylinder, num, dt);
		exhaustFlow += CylinderToOutside.Equalize(BrakeCylinder, null, num2, dt);
		airFlow += BrakeLineToReservoir.Valve(BrakeLine, BrakeReservoir, num2, dt);
	}

	protected virtual bool ShouldDeferToLocomotiveAir(out LocomotiveAirSystem locomotiveAirSystem)
	{
		locomotiveAirSystem = null;
		if (car.set == null)
		{
			return false;
		}
		if (car.Archetype != CarArchetype.Tender)
		{
			return false;
		}
		if (!car.TryGetAdjacentCar(car.EndToLogical(Car.End.F), out var adjacent) || !adjacent.IsLocomotive)
		{
			return false;
		}
		if (!(adjacent.air is LocomotiveAirSystem locomotiveAirSystem2))
		{
			return false;
		}
		locomotiveAirSystem = locomotiveAirSystem2;
		if (!locomotiveAirSystem.IsCutOut)
		{
			return true;
		}
		if (!locomotiveAirSystem.IsMuEnabled)
		{
			return false;
		}
		return locomotiveAirSystem.ShouldDeferToLocomotiveAir(out locomotiveAirSystem);
	}

	private void UpdateNeedsSend()
	{
		if (exhaustFlow > 0.1f || airFlow > 0.1f || anglecockFlowA > 0.1f || anglecockFlowB > 0.1f)
		{
			NeedsSend = true;
		}
	}

	private void UpdateBrakeLine(float dt)
	{
		if (this.car.EndGearA.IsAirConnected || this.car.set == null)
		{
			return;
		}
		IntegrationSet set = this.car.set;
		ResetFlowValues();
		int num = UnityEngine.Random.Range(0, 2);
		for (int i = 0; i < 2; i++)
		{
			Car.LogicalEnd logicalEnd = (((i + num) % 2 != 0) ? Car.LogicalEnd.B : Car.LogicalEnd.A);
			int carIndex = set.StartIndexForConnected(this.car, logicalEnd, IntegrationSet.EnumerationCondition.AirConnected);
			bool stop = false;
			while (!stop)
			{
				Car car = set.NextCarConnected(ref carIndex, logicalEnd, IntegrationSet.EnumerationCondition.AirConnected, out stop);
				if (car == null)
				{
					break;
				}
				if (logicalEnd == Car.LogicalEnd.A)
				{
					car.air.UpdateBrakeLineIndividualB(dt / 2f);
				}
				else
				{
					car.air.UpdateBrakeLineIndividualA(dt / 2f);
				}
			}
		}
	}

	private void ResetFlowValues()
	{
		bool stop = false;
		int carIndex = this.car.set.StartIndexForConnected(this.car, Car.LogicalEnd.A, IntegrationSet.EnumerationCondition.Coupled);
		Car car;
		while (!stop && (car = this.car.set.NextCarConnected(ref carIndex, Car.LogicalEnd.A, IntegrationSet.EnumerationCondition.Coupled, out stop)) != null)
		{
			car.air.anglecockFlowA = 0f;
			car.air.anglecockFlowB = 0f;
		}
	}

	private static float ValveValueForAnglecock(float anglecock)
	{
		if (!(anglecock < 0.01f))
		{
			return anglecock;
		}
		return 0f;
	}

	private void UpdateBrakeLineIndividualB(float dt)
	{
		CarAirSystem carAirSystem = car.set.GetAirConnection(car, Car.LogicalEnd.B)?.air;
		float num = ValveValueForAnglecock(anglecockB);
		AirConnection brakeLineConnectionB;
		if (carAirSystem == null)
		{
			brakeLineConnectionB = BrakeLineConnectionB;
			anglecockFlowB += brakeLineConnectionB.Equalize(BrakeLine, null, num, dt);
			return;
		}
		brakeLineConnectionB = BrakeLineConnectionB;
		float valve = Mathf.Min(carAirSystem.anglecockA, num);
		float num2 = brakeLineConnectionB.Equalize(BrakeLine, carAirSystem.BrakeLine, valve, dt);
		carAirSystem.anglecockFlowA += num2;
		anglecockFlowB += num2;
	}

	private void UpdateBrakeLineIndividualA(float dt)
	{
		CarAirSystem carAirSystem = car.set.GetAirConnection(car, Car.LogicalEnd.A)?.air;
		float num = ValveValueForAnglecock(anglecockA);
		AirConnection brakeLineConnectionA;
		if (carAirSystem == null)
		{
			brakeLineConnectionA = BrakeLineConnectionA;
			anglecockFlowA += brakeLineConnectionA.Equalize(BrakeLine, null, num, dt);
			return;
		}
		brakeLineConnectionA = carAirSystem.BrakeLineConnectionB;
		float valve = Mathf.Min(carAirSystem.anglecockB, num);
		float num2 = brakeLineConnectionA.Equalize(carAirSystem.BrakeLine, BrakeLine, valve, dt);
		carAirSystem.anglecockFlowB += num2;
		anglecockFlowA += num2;
	}

	protected void UpdateBrakingForce()
	{
		float b = CalculateTargetBrakePercent();
		brakePercent = Mathf.Lerp(brakePercent, b, Time.deltaTime);
	}

	private float CalculateTargetBrakePercent()
	{
		if (handbrakeApplied)
		{
			return Car.BrakeForceMultiplierHandbrake;
		}
		if (BrakeCylinder.Pressure < 2f)
		{
			return 0f;
		}
		return BrakeCylinder.Pressure / 64f;
	}

	public void SetAir(float brakeLinePressure, float brakeRes, float brakeCyl)
	{
		BrakeLine.Pressure = brakeLinePressure;
		BrakeReservoir.Pressure = brakeRes;
		BrakeCylinder.Pressure = brakeCyl;
	}

	public void PostRestoreProperties()
	{
		brakePercent = CalculateTargetBrakePercent();
	}

	public void BleedBrakeCylinder()
	{
		bleedBrakeCylinder = true;
	}

	internal static void GUIDrawDebugBrakeDisplay(Car car)
	{
		IntegrationSet set = car.set;
		Texture2D whiteTexture = Texture2D.whiteTexture;
		int carIndex = set.StartIndexForConnected(car, Car.LogicalEnd.A, IntegrationSet.EnumerationCondition.AirConnected);
		bool stop = false;
		int num = 20;
		int num2 = Screen.height - 250;
		while (!stop)
		{
			Car car2 = set.NextCarConnected(ref carIndex, Car.LogicalEnd.A, IntegrationSet.EnumerationCondition.AirConnected, out stop);
			if (car2 == null)
			{
				break;
			}
			float value = ((car2.IsLocomotive && car2.air is LocomotiveAirSystem locomotiveAirSystem) ? locomotiveAirSystem.MainReservoir.Pressure : ((car2.Archetype != CarArchetype.Tender) ? car2.air.BrakeReservoir.Pressure : 0f));
			GUI.color = Color.green;
			GUI.DrawTexture(new Rect(num, num2, 2f, -64f * Mathf.InverseLerp(0f, 90f, car2.air.BrakeLine.Pressure)), whiteTexture);
			num += 3;
			GUI.color = Color.yellow;
			GUI.DrawTexture(new Rect(num, num2, 2f, -64f * Mathf.InverseLerp(0f, 90f, value)), whiteTexture);
			num += 3;
			GUI.color = Color.red;
			GUI.DrawTexture(new Rect(num, num2, 2f, -64f * Mathf.InverseLerp(0f, 72f, car2.air.BrakeCylinder.Pressure)), whiteTexture);
			num += 3;
			if (car2.EndGearB.IsAirConnected)
			{
				float velocity = car2.air.BrakeLineConnectionB.Velocity;
				float t = Mathf.InverseLerp(-300f, 300f, velocity);
				GUI.color = Color.cyan;
				GUI.DrawTexture(new Rect(num, num2 + 4, Mathf.Lerp(-6f, 6f, t), 2f), whiteTexture);
				num += 4;
			}
		}
	}
}
