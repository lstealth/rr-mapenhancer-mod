using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Messages;
using Game.State;
using JetBrains.Annotations;
using KeyValue.Runtime;
using Model.AI;
using Model.Definition;
using Model.Definition.Components;
using Model.Physics;
using RollingStock;
using RollingStock.ContinuousControls;
using Serilog;
using UI;
using UnityEngine;
using UnityEngine.Pool;

namespace Model;

public abstract class BaseLocomotive : Car
{
	[Header("Base Locomotive")]
	public LocomotiveCabControlsHookup cabControls;

	public LocomotiveControlAdapter locomotiveControl;

	private float _tractiveEffort;

	protected float _wheelVelocity;

	private CarWheelState _wheelState;

	private Coroutine _periodicUpdateCoroutine;

	private float _idleTimerLastReset;

	public float slipSpeed = 0.05f;

	private const float LocomotiveBrakeNegative = -0.1f;

	public override bool IsLocomotive => true;

	public override float TractiveEffort => _tractiveEffort;

	public override CarWheelState WheelState => _wheelState;

	public AutoEngineerPlanner AutoEngineerPlanner { get; private set; }

	public LocomotiveControlHelper ControlHelper { get; private set; }

	public abstract float RatedTractiveEffort { get; }

	public bool IsIdle
	{
		get
		{
			return KeyValueObject[PropertyChange.KeyForControl(PropertyChange.Control.Idle)].BoolValue;
		}
		private set
		{
			KeyValueObject[PropertyChange.KeyForControl(PropertyChange.Control.Idle)] = Value.Bool(value);
		}
	}

	public abstract bool HasFuel { get; }

	internal bool IsMuEnabled => KeyValueObject[PropertyChange.KeyForControl(PropertyChange.Control.Mu)].BoolValue;

	protected abstract float AdhesiveWeight { get; }

	public event Action OnIdleDidChange;

	public event Action OnHasFuelDidChange;

	private void OnEnable()
	{
		_periodicUpdateCoroutine = StartCoroutine(PeriodicUpdateBody());
		ControlHelper = new LocomotiveControlHelper(this);
	}

	private void OnDisable()
	{
		if (_periodicUpdateCoroutine != null)
		{
			StopCoroutine(_periodicUpdateCoroutine);
		}
		_periodicUpdateCoroutine = null;
	}

	protected override void FixedUpdate()
	{
		UpdateTractiveEffortWheelState();
		base.FixedUpdate();
		UpdateCabControls();
	}

	public override void WillMove()
	{
		base.WillMove();
		if (AutoEngineerPlanner != null)
		{
			AutoEngineerPlanner.WillMove();
		}
	}

	private IEnumerator PeriodicUpdateBody()
	{
		if (ghost)
		{
			yield break;
		}
		WaitForSeconds wait = new WaitForSeconds(1f);
		while (true)
		{
			yield return wait;
			if (StateManager.IsHost)
			{
				bool flag = Mathf.Abs(base.velocity) < 0.01f;
				IsIdle = flag && _idleTimerLastReset + 600f < Time.time;
			}
			try
			{
				PeriodicUpdate(1f);
			}
			catch (Exception exception)
			{
				Log.Error(exception, "Error during periodic update");
			}
		}
	}

	protected virtual void PeriodicUpdate(float dt)
	{
		PeriodicUpdateForMu();
	}

	protected void InvokeHasFuelDidChange()
	{
		this.OnHasFuelDidChange?.Invoke();
	}

	private void PeriodicUpdateForMu()
	{
		if (air is LocomotiveAirSystem locomotiveAirSystem)
		{
			locomotiveAirSystem.IsMuEnabled = IsMuEnabled;
			locomotiveAirSystem.UpdateCachedShouldDeferToLocomotiveAir();
		}
		if (StateManager.IsHost && IsMuEnabled)
		{
			BaseLocomotive baseLocomotive = FindMuSourceLocomotive();
			if (!(baseLocomotive == null))
			{
				float abstractThrottle = baseLocomotive.locomotiveControl.AbstractThrottle;
				float num = CutoffSettingForVelocity(base.velocity);
				num *= (float)((FrontIsA == baseLocomotive.FrontIsA) ? 1 : (-1)) * Mathf.Sign(baseLocomotive.locomotiveControl.AbstractReverser);
				num = (float)Mathf.CeilToInt(num * 20f) / 20f;
				SendPropertyChange(PropertyChange.Control.Throttle, abstractThrottle);
				SendPropertyChange(PropertyChange.Control.Reverser, num);
			}
		}
	}

	private BaseLocomotive FindSourceLocomotive(LogicalEnd searchDirection)
	{
		bool stop = false;
		int? num = base.set.IndexOfCar(this);
		if (!num.HasValue)
		{
			throw new Exception("Couldn't find car in set");
		}
		int carIndex = num.Value;
		LogicalEnd fromEnd = ((searchDirection == LogicalEnd.A) ? LogicalEnd.B : LogicalEnd.A);
		Car car;
		while (!stop && (car = base.set.NextCarConnected(ref carIndex, fromEnd, IntegrationSet.EnumerationCondition.AirAndCoupled, out stop)) != null)
		{
			if (!(car == this) && car.Archetype != CarArchetype.Tender)
			{
				if (!car.IsLocomotive || !(car is BaseLocomotive baseLocomotive))
				{
					return null;
				}
				if (!baseLocomotive.locomotiveControl.air.IsCutOut)
				{
					return baseLocomotive;
				}
			}
		}
		return null;
	}

	[CanBeNull]
	internal BaseLocomotive FindMuSourceLocomotive()
	{
		LogicalEnd searchDirection = EndToLogical(End.F);
		LogicalEnd searchDirection2 = EndToLogical(End.R);
		return FindSourceLocomotive(searchDirection) ?? FindSourceLocomotive(searchDirection2);
	}

	protected void ResetIdleTimer()
	{
		if (StateManager.IsHost && !base.IsInDidLoadModels)
		{
			_idleTimerLastReset = Time.time;
		}
	}

	public override bool ShouldUpdatePosition()
	{
		if (Mathf.Abs(_wheelVelocity) > 0.001f)
		{
			return true;
		}
		return base.ShouldUpdatePosition();
	}

	protected override void FireOnMovement(MovementInfo info)
	{
		base.FireOnMovement(info);
		if (AutoEngineerPlanner != null)
		{
			AutoEngineerPlanner.ApplyMovement(info);
		}
	}

	protected override float ServiceMetersFromActual(float meters)
	{
		return base.ServiceMetersFromActual(meters) * Config.serviceDistanceTractiveEffortMultiplier.Evaluate(NormalizedTractiveEffort);
	}

	protected virtual void ConnectBodyControls()
	{
		ConnectGauges();
		if (TryGetControl(ControlPurpose.LocomotiveBrake, out var foundControl))
		{
			cabControls.locomotiveBrake = foundControl;
			foundControl.ConfigurePropertyChange(delegate(float value)
			{
				value = LocomotiveBrakeMapFromControl(value);
				return new PropertyChange(id, PropertyChange.Control.LocomotiveBrake, value);
			}, TooltipTextForLocomotiveBrake);
		}
		if (TryGetControl(ControlPurpose.TrainBrake, out var foundControl2))
		{
			cabControls.trainBrake = foundControl2;
			foundControl2.ConfigurePropertyChange((float value) => new PropertyChange(id, PropertyChange.Control.TrainBrake, value), TooltipTextForTrainBrake);
		}
		if (TryGetControl(ControlPurpose.TrainBrakeCutOut, out var foundControl3))
		{
			cabControls.cutout = foundControl3;
			foundControl3.ConfigureSnap(1);
			foundControl3.ConfigurePropertyChange((float value) => new PropertyChange(id, PropertyChange.Control.CutOut, value < 0.5f), TooltipTextForTrainBrakeCutOut);
		}
		if (TryGetControl(ControlPurpose.Whistle, out var foundControl4))
		{
			cabControls.horn = foundControl4;
			foundControl4.ConfigurePropertyChange((float value) => new PropertyChange(id, PropertyChange.Control.Horn, value));
		}
		if (TryGetControl(ControlPurpose.Bell, out var bellControl))
		{
			cabControls.bell = bellControl;
			bellControl.ConfigurePropertyChange((float value) => new PropertyChange(id, PropertyChange.Control.Bell, value), () => (!((double)bellControl.Value > 0.5)) ? "Off" : "On");
		}
		LocomotiveCabControlsHookup locomotiveCabControlsHookup = cabControls;
		if ((object)locomotiveCabControlsHookup.locomotiveBrake == null)
		{
			locomotiveCabControlsHookup.locomotiveBrake = DummyControl();
		}
		locomotiveCabControlsHookup = cabControls;
		if ((object)locomotiveCabControlsHookup.trainBrake == null)
		{
			locomotiveCabControlsHookup.trainBrake = DummyControl();
		}
		locomotiveCabControlsHookup = cabControls;
		if ((object)locomotiveCabControlsHookup.horn == null)
		{
			locomotiveCabControlsHookup.horn = DummyControl();
		}
		locomotiveCabControlsHookup = cabControls;
		if ((object)locomotiveCabControlsHookup.bell == null)
		{
			locomotiveCabControlsHookup.bell = DummyControl();
		}
		_controlObservers.Add(KeyValueObject.Observe(PropertyChange.KeyForControl(PropertyChange.Control.Bell), delegate(Value value)
		{
			locomotiveControl.Bell = (double)value.FloatValue > 0.5;
			ResetIdleTimer();
		}));
		_controlObservers.Add(KeyValueObject.Observe(PropertyChange.KeyForControl(PropertyChange.Control.Horn), delegate(Value value)
		{
			locomotiveControl.Horn = value.FloatValue;
			ResetIdleTimer();
		}));
	}

	private void ConnectGauges()
	{
		GaugeController[] gaugeControllers = BodyTransform.GetComponentsInChildren<GaugeController>();
		cabControls.speedometer = FindGauge("speedometer");
		cabControls.equalizingReservoir = FindGauge("eqlres");
		cabControls.brakeCylinder = FindGauge("cyl");
		cabControls.brakePipe = FindGauge("line");
		cabControls.mainReservoir = FindGauge("mainres");
		cabControls.boilerPressure = FindGauge("boiler");
		IGauge FindGauge(string gaugeId)
		{
			List<IGauge> list = CollectionPool<List<IGauge>, IGauge>.Get();
			try
			{
				GaugeController[] array = gaugeControllers;
				int i;
				for (i = 0; i < array.Length; i++)
				{
					GaugeBehaviour gaugeBehaviour = array[i].GaugeForId(gaugeId);
					if (!(gaugeBehaviour == null))
					{
						list.Add(gaugeBehaviour);
					}
				}
				i = list.Count;
				if (i > 1)
				{
					return new MultiGauge(list.ToList());
				}
				if (i == 1)
				{
					return list[0];
				}
				return new MultiGauge(null);
			}
			finally
			{
				CollectionPool<List<IGauge>, IGauge>.Release(list);
			}
		}
	}

	protected bool TryGetControl(ControlPurpose purpose, out ContinuousControl foundControl)
	{
		RadialAnimatedControl[] componentsInChildren = BodyTransform.gameObject.GetComponentsInChildren<RadialAnimatedControl>();
		foreach (RadialAnimatedControl radialAnimatedControl in componentsInChildren)
		{
			if (radialAnimatedControl.ControlComponentPurpose == purpose)
			{
				foundControl = radialAnimatedControl;
				return true;
			}
		}
		foundControl = null;
		return false;
	}

	protected ContinuousControl DummyControl()
	{
		GameObject obj = new GameObject("DummyControl");
		obj.transform.SetParent(BodyTransform);
		return obj.AddComponent<DummyControl>();
	}

	protected virtual void ObserveCoreProperties()
	{
		Observers.Add(KeyValueObject.Observe(PropertyChange.KeyForControl(PropertyChange.Control.Throttle), delegate(Value value)
		{
			locomotiveControl.AbstractThrottle = value.FloatValue;
			if (value.FloatValue > 0.001f)
			{
				ResetAtRest();
			}
			ResetIdleTimer();
		}));
		Observers.Add(KeyValueObject.Observe(PropertyChange.KeyForControl(PropertyChange.Control.Reverser), delegate(Value value)
		{
			locomotiveControl.AbstractReverser = value.FloatValue;
			if (Mathf.Abs(value.FloatValue) > 0.001f)
			{
				ResetAtRest();
			}
			ResetIdleTimer();
		}));
		Observers.Add(KeyValueObject.Observe(PropertyChange.KeyForControl(PropertyChange.Control.LocomotiveBrake), delegate(Value value)
		{
			locomotiveControl.LocomotiveBrakeSetting = value.FloatValue;
			ResetIdleTimer();
		}));
		Observers.Add(KeyValueObject.Observe(PropertyChange.KeyForControl(PropertyChange.Control.TrainBrake), delegate(Value value)
		{
			locomotiveControl.TrainBrakeSetting = value.FloatValue;
			ResetIdleTimer();
		}));
		Observers.Add(KeyValueObject.Observe(PropertyChange.KeyForControl(PropertyChange.Control.CutOut), delegate(Value value)
		{
			locomotiveControl.air.IsCutOut = value.BoolValue;
			ResetIdleTimer();
		}));
	}

	protected override void FinishSetup()
	{
		LocomotiveAirSystem locomotiveAirSystem = base.gameObject.AddComponent<LocomotiveAirSystem>();
		if (StateManager.IsHost)
		{
			locomotiveAirSystem.OnResetBailOff += delegate
			{
				base.ControlProperties[PropertyChange.Control.LocomotiveBrake] = 0;
			};
		}
		air = locomotiveAirSystem;
		locomotiveControl = CreateLocomotiveControl();
		locomotiveControl.air = (LocomotiveAirSystem)air;
		ObserveCoreProperties();
		MapIcon = UnityEngine.Object.Instantiate(_setupPrefabs.LocomotiveMapIcon, base.transform);
		MapIcon.SetText(base.Ident.RoadNumber);
		MapIcon.OnClick = delegate
		{
			if (GameInput.IsShiftDown)
			{
				TrainController.Shared.SelectedCar = this;
			}
			else
			{
				CarPickable.HandleShowInspector(this);
			}
		};
		if (StateManager.IsHost && GetComponent<AutoEngineer>() == null)
		{
			AutoEngineerPlanner = base.gameObject.AddComponent<AutoEngineerPlanner>();
		}
	}

	public override void SetIdent(CarIdent ident)
	{
		base.SetIdent(ident);
		if (MapIcon != null)
		{
			MapIcon.SetText(base.Ident.RoadNumber);
		}
	}

	public override void PostRestoreProperties()
	{
		base.PostRestoreProperties();
		if (StateManager.IsHost && locomotiveControl.LocomotiveBrakeSetting < 0f)
		{
			Log.Warning("Fixing {car} with bailed brake setting: {locomotiveBrakeSetting}", this, locomotiveControl.LocomotiveBrakeSetting);
			locomotiveControl.LocomotiveBrakeSetting = 0f;
		}
	}

	protected override void DidLoadModels()
	{
		base.DidLoadModels();
		_controlObservers.Add(KeyValueObject.Observe(PropertyChange.KeyForControl(PropertyChange.Control.Idle), delegate
		{
			this.OnIdleDidChange?.Invoke();
		}));
	}

	protected override void PreSetupComponents(ComponentLifetime lifetime)
	{
		base.PreSetupComponents(lifetime);
		if (lifetime == ComponentLifetime.Model)
		{
			cabControls = BodyTransform.gameObject.AddComponent<LocomotiveCabControlsHookup>();
			locomotiveControl.audio = BodyTransform.gameObject.AddComponent<LocomotiveAudio>();
		}
	}

	protected override void DidSetBodyActive()
	{
		base.DidSetBodyActive();
		locomotiveControl.audio.bell = BodyTransform.GetComponentInChildren<Bell>();
		ConnectBodyControls();
	}

	protected override void UnloadModels()
	{
		base.UnloadModels();
		locomotiveControl.audio = null;
		cabControls = null;
	}

	protected abstract LocomotiveControlAdapter CreateLocomotiveControl();

	protected virtual void UpdateCabControls()
	{
		if (cabControls == null)
		{
			return;
		}
		if (air is LocomotiveAirSystem locomotiveAirSystem)
		{
			cabControls.brakeCylinder.Value = locomotiveAirSystem.BrakeCylinder.Pressure;
			cabControls.mainReservoir.Value = locomotiveAirSystem.MainReservoir.Pressure;
			cabControls.brakePipe.Value = locomotiveAirSystem.BrakeLine.Pressure;
			cabControls.equalizingReservoir.Value = locomotiveAirSystem.trainBrakePressure;
			cabControls.trainBrake.Value = locomotiveAirSystem.trainBrakeSetting;
			if (cabControls.cutout != null)
			{
				cabControls.cutout.Value = ((!locomotiveAirSystem.IsCutOut) ? 1 : 0);
			}
			cabControls.locomotiveBrake.Value = LocomotiveBrakeMapToControl(locomotiveAirSystem.locomotiveBrakeSetting);
			if (StateManager.IsHost)
			{
				KeyValueObject[PropertyChange.KeyForControl(PropertyChange.Control.Compressor)] = Value.Bool(locomotiveAirSystem.compressorRunning);
			}
		}
		if (cabControls.speedometer != null)
		{
			cabControls.speedometer.Value = Mathf.Abs(base.velocity * 2.23694f);
		}
		cabControls.horn.Value = locomotiveControl.Horn;
	}

	protected abstract float CalculateTractiveEffort(float signedVelocityMph);

	private void UpdateTractiveEffortWheelState()
	{
		float num = TrainMath.TrackCoefficientOfFriction(TrainMath.TrackCondition.Dry, _wheelVelocity);
		if (base.IsDerailed)
		{
			num = 0.1f;
		}
		if (_wheelState == CarWheelState.Slip)
		{
			num = Mathf.Min(num, 0.1f);
		}
		float num2 = AdhesiveWeight * num;
		float num3 = Car.TractiveForceMultiplier * CalculateTractiveEffort(_wheelVelocity * 2.23694f);
		num3 *= Config.tractiveEffortMultForCondition.Evaluate(base.Condition);
		num3 *= Mathf.Lerp(1f, 0f, Mathf.Clamp01(Mathf.InverseLerp(maxSpeedMph, maxSpeedMph + 1f, Mathf.Abs(_wheelVelocity) * 2.23694f)));
		if (Mathf.Abs(num3) > Mathf.Abs(num2))
		{
			_wheelState = CarWheelState.Slip;
		}
		else
		{
			_wheelState = CarWheelState.Tracking;
		}
		float deltaTime = Time.deltaTime;
		switch (_wheelState)
		{
		case CarWheelState.Tracking:
			_wheelVelocity = Mathf.Lerp(_wheelVelocity, base.velocity, deltaTime * 10f);
			break;
		case CarWheelState.Slip:
		{
			_wheelVelocity += num3 * deltaTime * deltaTime * slipSpeed;
			float a = Mathf.Abs(base.velocity) + 17.88157f;
			_wheelVelocity = Mathf.Sign(_wheelVelocity) * Mathf.Min(a, Mathf.Abs(_wheelVelocity));
			break;
		}
		case CarWheelState.Lock:
			_wheelVelocity = Mathf.Lerp(_wheelVelocity, 0f, deltaTime * 10f);
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
		float tractiveEffort = Mathf.Sign(num3) * Mathf.Min(num2, Mathf.Abs(num3));
		_tractiveEffort = tractiveEffort;
	}

	protected static string Percent(float value)
	{
		return Mathf.RoundToInt(value * 100f) + "%";
	}

	private string TooltipTextForLocomotiveBrake()
	{
		if (air is LocomotiveAirSystem { IsCutOut: not false })
		{
			return "Locomotive Cut Out";
		}
		float num = LocomotiveBrakeMapFromControl(cabControls.locomotiveBrake.Value);
		if (!(num < 0f))
		{
			return Percent(num);
		}
		return "Bail-Off";
	}

	private string TooltipTextForTrainBrake()
	{
		if (air is LocomotiveAirSystem { IsCutOut: not false })
		{
			return "Locomotive Cut Out";
		}
		return Percent(cabControls.trainBrake.Value);
	}

	private string TooltipTextForTrainBrakeCutOut()
	{
		if (!(air is LocomotiveAirSystem { IsCutOut: not false }))
		{
			return "Cut In";
		}
		return "Cut Out";
	}

	private static float LocomotiveBrakeMapToControl(float value)
	{
		return Mathf.InverseLerp(-0.1f, 1f, value);
	}

	private static float LocomotiveBrakeMapFromControl(float value)
	{
		return Mathf.Lerp(-0.1f, 1f, value);
	}

	public abstract float MaxTractiveEffortAtVelocity(float absVelocityMph);

	public abstract float CutoffSettingForVelocity(float velocityMps);
}
