using System;
using UnityEngine;

namespace Model.Physics;

public class LocomotiveAirSystem : CarAirSystem
{
	public readonly Reservoir MainReservoir = new Reservoir("Main Reservoir", 43f, 140f);

	public float trainBrakePressure;

	public float locomotiveBrakePressure;

	private readonly VentedValve _mainReservoirToBrakeLine = new VentedValve(Reservoir.Pipe.Line);

	private readonly VentedValve _mainReservoirToBrakeCylinder = new VentedValve(Reservoir.Pipe.Line);

	public float maximumLocomotiveBrakePressure = 72f;

	public float brakeFeedValvePressure = 90f;

	public float compressorLimitLower = 128f;

	public float compressorLimitUpper = 140f;

	public bool compressorRunning;

	public float compressorRate = 0.5f;

	public float brakeFeedValveFlow;

	private float _lapTrainBrakePressure;

	private float _locomotiveBrakeLineMemory;

	private float _locomotiveBrakeLineBank;

	public bool IsCutOut;

	private (bool should, LocomotiveAirSystem locoAir) _cachedShouldDeferToLocomotiveAir = (should: false, locoAir: null);

	public float trainBrakeSetting
	{
		get
		{
			return 1f - trainBrakePressure / brakeFeedValvePressure;
		}
		set
		{
			trainBrakePressure = (1f - value) * brakeFeedValvePressure;
		}
	}

	public float locomotiveBrakeSetting
	{
		get
		{
			return locomotiveBrakePressure / maximumLocomotiveBrakePressure;
		}
		set
		{
			locomotiveBrakePressure = value * maximumLocomotiveBrakePressure;
		}
	}

	public float locomotiveBrakeControlLine { get; private set; }

	public bool HasFuel { get; set; } = true;

	public bool IsMuEnabled { get; set; }

	public event Action OnResetBailOff;

	protected override void SetupReservoirs()
	{
		trainBrakePressure = brakeFeedValvePressure;
		_lapTrainBrakePressure = brakeFeedValvePressure;
		_locomotiveBrakeLineMemory = brakeFeedValvePressure;
		base.SetupReservoirs();
	}

	protected override void UpdateAir(float dt)
	{
		UpdateCompressor(dt);
		if (IsCutOut)
		{
			locomotiveBrakeSetting = 0f;
			trainBrakeSetting = 0f;
			base.UpdateAir(dt);
			return;
		}
		UpdateLocomotiveBrakeControlLine();
		exhaustFlow += _mainReservoirToBrakeCylinder.ValveVent(MainReservoir, BrakeCylinder, locomotiveBrakeControlLine, canValve: true, dt);
		UpdateBrakingForce();
		if (Mathf.Abs(trainBrakePressure - brakeFeedValvePressure) < 1f)
		{
			_lapTrainBrakePressure = brakeFeedValvePressure;
		}
		else
		{
			_lapTrainBrakePressure = Mathf.Min(trainBrakePressure, _lapTrainBrakePressure);
		}
		float lapTrainBrakePressure = _lapTrainBrakePressure;
		bool flag = Mathf.Abs(lapTrainBrakePressure - brakeFeedValvePressure) < 1f;
		brakeFeedValveFlow = _mainReservoirToBrakeLine.ValveAutomaticBrake(MainReservoir, BrakeLine, lapTrainBrakePressure, flag, dt);
		if (flag)
		{
			_locomotiveBrakeLineMemory = brakeFeedValvePressure;
			_locomotiveBrakeLineBank = 0f;
		}
	}

	private void UpdateCompressor(float dt)
	{
		if (MainReservoir.Pressure < compressorLimitLower)
		{
			compressorRunning = HasFuel;
		}
		if (MainReservoir.Pressure > compressorLimitUpper)
		{
			compressorRunning = false;
		}
		if (compressorRunning)
		{
			MainReservoir.Pressure += compressorRate * dt;
		}
	}

	protected override bool ShouldDeferToLocomotiveAir(out LocomotiveAirSystem locomotiveAirSystem)
	{
		locomotiveAirSystem = _cachedShouldDeferToLocomotiveAir.locoAir;
		return _cachedShouldDeferToLocomotiveAir.should;
	}

	private bool _ShouldDeferToLocomotiveAir(out LocomotiveAirSystem locomotiveAirSystem)
	{
		locomotiveAirSystem = null;
		if (car.set == null)
		{
			return false;
		}
		if (!(car.air is LocomotiveAirSystem locomotiveAirSystem2) || !(car is BaseLocomotive baseLocomotive))
		{
			return false;
		}
		if (!locomotiveAirSystem2.IsCutOut || !locomotiveAirSystem2.IsMuEnabled)
		{
			return false;
		}
		BaseLocomotive baseLocomotive2 = baseLocomotive.FindMuSourceLocomotive();
		if (baseLocomotive2 == null)
		{
			return false;
		}
		if (!(baseLocomotive2.air is LocomotiveAirSystem locomotiveAirSystem3))
		{
			return false;
		}
		locomotiveAirSystem = locomotiveAirSystem3;
		return true;
	}

	internal void UpdateCachedShouldDeferToLocomotiveAir()
	{
		LocomotiveAirSystem locomotiveAirSystem;
		bool item = _ShouldDeferToLocomotiveAir(out locomotiveAirSystem);
		_cachedShouldDeferToLocomotiveAir = (should: item, locoAir: locomotiveAirSystem);
	}

	private void UpdateLocomotiveBrakeControlLine()
	{
		if (locomotiveBrakePressure < 0f)
		{
			_locomotiveBrakeLineMemory = _lapTrainBrakePressure;
			_locomotiveBrakeLineBank = 0f;
			locomotiveBrakeControlLine = 0f;
			locomotiveBrakePressure = 0f;
			this.OnResetBailOff?.Invoke();
			return;
		}
		locomotiveBrakePressure = Mathf.Clamp(locomotiveBrakePressure, 0f, maximumLocomotiveBrakePressure);
		float num = Mathf.Clamp(_locomotiveBrakeLineBank + (_locomotiveBrakeLineMemory - _lapTrainBrakePressure) * 2.5f, 0f, maximumLocomotiveBrakePressure);
		if (locomotiveBrakePressure > num)
		{
			locomotiveBrakeControlLine = locomotiveBrakePressure;
			return;
		}
		locomotiveBrakeControlLine = num;
		if (_lapTrainBrakePressure < _locomotiveBrakeLineMemory)
		{
			_locomotiveBrakeLineBank += num - _locomotiveBrakeLineBank;
			_locomotiveBrakeLineMemory = _lapTrainBrakePressure;
		}
	}
}
