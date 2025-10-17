using System;
using System.Collections.Generic;
using Audio.DynamicChuff;
using Game.Messages;
using Game.State;
using KeyValue.Runtime;
using Model.Definition;
using Model.Definition.Components;
using Model.Definition.Data;
using Model.Ops;
using Model.Physics;
using RollingStock;
using RollingStock.Steam;
using Serilog;
using Track;
using UnityEngine;

namespace Model;

public class SteamLocomotive : BaseLocomotive
{
	private SteamLocomotiveWheelAnimator _wheelAnimator;

	[Header("Steam Locomotive")]
	public SteamEngine engine;

	public bool hasTender = true;

	private SteamChuffParticleController _chuffParticles;

	private IChuffProvider _chuffAudio;

	private readonly List<ISteamLocomotiveSubcomponent> _subcomponents = new List<ISteamLocomotiveSubcomponent>();

	private bool _hasSetSlots;

	private int _coalSlot;

	private int _waterSlot = 1;

	private const float ReverserCenteredThreshold = 0.1f;

	private static readonly float[] CutoffSpeeds = new float[10] { 0f, 5f, 10f, 15f, 20f, 25f, 30f, 35f, 40f, 45f };

	private float[] _cutoffSettings;

	public SteamLocomotiveDefinition LocoDefinition => (SteamLocomotiveDefinition)DefinitionInfo.Definition;

	public override float NormalizedTractiveEffort => engine.NormalizedTractiveEffort;

	public override float RatedTractiveEffort => engine.MaximumTractiveEffort;

	public override bool HasFuel => engine.HasWaterAndCoal;

	protected override float AdhesiveWeight => engine.weightOnDrivers;

	protected override void OnDrawGizmosSelected()
	{
		base.OnDrawGizmosSelected();
	}

	protected override bool WantsEndGear(End end)
	{
		if (hasTender)
		{
			return end == End.F;
		}
		return true;
	}

	public override bool ForceConnectedToAtRear(Car other)
	{
		if (!hasTender)
		{
			return false;
		}
		return base.Archetype == CarArchetype.Tender;
	}

	protected override float CalculateTractiveEffort(float signedVelocityMph)
	{
		return engine.CalculateTractiveEffort(signedVelocityMph);
	}

	public override float MaxTractiveEffortAtVelocity(float absVelocityMph)
	{
		return engine.MaximumTractiveEffortAtVelocity(absVelocityMph);
	}

	protected override void UnloadModels()
	{
		base.UnloadModels();
		_subcomponents.Clear();
		_wheelAnimator = null;
		_chuffParticles = null;
	}

	protected override void DidLoadModels()
	{
		base.DidLoadModels();
		Animator componentInChildren = BodyTransform.GetComponentInChildren<Animator>();
		_wheelAnimator = BodyTransform.gameObject.AddComponent<SteamLocomotiveWheelAnimator>();
		_chuffParticles = BodyTransform.GetComponentInChildren<SteamChuffParticleController>();
		if (_chuffParticles != null)
		{
			_chuffParticles.absVelocity = 0f;
			_chuffParticles.tractiveEffort = 0f;
			_chuffParticles.isStopped = true;
			_chuffParticles.continuous = false;
		}
		_chuffAudio = BodyTransform.GetComponentInChildren<IChuffProvider>();
		SteamLocomotiveDefinition locoDefinition = LocoDefinition;
		SteamLocomotiveDefinition.Wheelset wheelset = locoDefinition.Wheelsets[locoDefinition.MainDriverIndex];
		if (_chuffAudio != null)
		{
			_chuffAudio.Configure(wheelset.Diameter, Mathf.InverseLerp(15000f, 65000f, engine.MaximumTractiveEffort));
			_chuffAudio.Delegate = _chuffParticles;
		}
		_wheelAnimator.Configure(locoDefinition.Wheelsets, locoDefinition.MainDriverIndex, componentInChildren, this);
		AddOilPointPickable(wheelset.Offset, wheelset.Length, wheelset.Diameter);
		WhistleController componentInChildren2 = BodyTransform.GetComponentInChildren<WhistleController>();
		if (componentInChildren2 != null)
		{
			locomotiveControl.audio.whistle = componentInChildren2.whistlePlayer;
			SmokeEffectWrapper whistleSteamEffect = componentInChildren2.EffectWrapper;
			if (whistleSteamEffect.IsValid)
			{
				_controlObservers.Add(KeyValueObject.Observe(PropertyChange.KeyForControl(PropertyChange.Control.Horn), delegate(Value value)
				{
					whistleSteamEffect.Rate = value.FloatValue * 100f;
				}));
			}
		}
		_subcomponents.AddRange(BodyTransform.GetComponentsInChildren<ISteamLocomotiveSubcomponent>());
	}

	protected override LocomotiveControlAdapter CreateLocomotiveControl()
	{
		SteamLocomotiveControl steamLocomotiveControl = base.gameObject.AddComponent<SteamLocomotiveControl>();
		steamLocomotiveControl.locomotive = this;
		return steamLocomotiveControl;
	}

	protected override float CalculateCarLength()
	{
		float propertyValue = base.CalculateCarLength();
		float num = LocoDefinition.PositionHead - LocoDefinition.PositionTail;
		Log.Debug("{prototypeId} car lengths: {base}, {steam}", base.DefinitionIdentifier, propertyValue, num);
		return num;
	}

	protected override float OffsetToEnd(End end, float extra = 0f)
	{
		float num = ((end == End.F) ? 1f : (-1f));
		float num2 = ((end == End.F) ? LocoDefinition.PositionHead : LocoDefinition.PositionTail);
		return num * extra + num2;
	}

	protected override void UpdateTruckLinearOffset()
	{
		if (_wheelAnimator != null)
		{
			_wheelAnimator.SetLinearOffset(_linearOffset);
		}
	}

	protected override void ValidateDefinition()
	{
		base.ValidateDefinition();
		SteamLocomotiveDefinition locoDefinition = LocoDefinition;
		if (locoDefinition.Wheelsets == null)
		{
			locoDefinition.Wheelsets = new List<SteamLocomotiveDefinition.Wheelset>();
		}
		if (locoDefinition.Wheelsets.Count == 0 || locoDefinition.MainDriverIndex > locoDefinition.Wheelsets.Count - 1)
		{
			locoDefinition.Wheelsets.Add(new SteamLocomotiveDefinition.Wheelset
			{
				Diameter = 1f
			});
			locoDefinition.MainDriverIndex = 0;
		}
		if (locoDefinition.Wheelsets[locoDefinition.MainDriverIndex].Length < 0.001f)
		{
			locoDefinition.Wheelsets[locoDefinition.MainDriverIndex].Length = 1f;
		}
		if (locoDefinition.WeightOnDrivers < 100f)
		{
			locoDefinition.WeightOnDrivers = locoDefinition.WeightEmpty;
		}
		if (locoDefinition.PositionHead < 0.001f)
		{
			locoDefinition.PositionHead = locoDefinition.Length / 2f;
		}
		if (locoDefinition.PositionTail > -0.001f)
		{
			locoDefinition.PositionTail = (0f - locoDefinition.Length) / 2f;
		}
	}

	protected override void FinishSetup()
	{
		SteamLocomotiveDefinition locoDefinition = LocoDefinition;
		hasTender = !string.IsNullOrEmpty(locoDefinition.TenderIdentifier);
		SteamLocomotiveDefinition.Wheelset wheelset = locoDefinition.Wheelsets[locoDefinition.MainDriverIndex];
		engine = base.gameObject.AddComponent<SteamEngine>();
		engine.OverrideStartingTractiveEffort = ((locoDefinition.PublishedTractiveEffort == 0) ? ((float?)null) : new float?(locoDefinition.PublishedTractiveEffort));
		engine.driverDiameterInches = wheelset.Diameter * 39.37008f;
		engine.numberOfCylinders = 2;
		engine.maximumBoilerPressure = locoDefinition.MaximumBoilerPressure;
		engine.pistonStrokeInches = locoDefinition.PistonStrokeInches;
		engine.pistonDiameterInches = locoDefinition.PistonDiameterInches;
		engine.weightOnDrivers = locoDefinition.WeightOnDrivers;
		engine.totalHeatingSurface = locoDefinition.TotalHeatingSurface;
		engine.UpdateMaximumTractiveEffort();
		maxSpeedMph = engine.driverDiameterInches + (float)UnityEngine.Random.Range(5, 10);
		engine.MaximumSpeedMph = maxSpeedMph;
		Log.Debug("Locomotive {car} {definitionId} maxSpeedMph = {maxSpeedMph}, maxTE = {maxTractiveEffort}", this, base.DefinitionIdentifier, maxSpeedMph, engine.MaximumTractiveEffort);
		SteamLocomotiveDefinition.Wheelset wheelset2 = locoDefinition.Wheelsets[0];
		List<SteamLocomotiveDefinition.Wheelset> wheelsets = locoDefinition.Wheelsets;
		SteamLocomotiveDefinition.Wheelset wheelset3 = wheelsets[wheelsets.Count - 1];
		float num = wheelset2.Length / 2f + wheelset2.Diameter * 0.3f;
		float num2 = wheelset3.Length / 2f + wheelset3.Diameter * 0.3f;
		wheelInsetF = locoDefinition.PositionHead - wheelset2.Offset - num;
		wheelInsetR = 0f - (locoDefinition.PositionTail - wheelset3.Offset) - num2;
		base.FinishSetup();
	}

	protected override void PeriodicUpdate(float dt)
	{
		base.PeriodicUpdate(dt);
		bool isHost = StateManager.IsHost;
		Car car = FuelCar();
		if (car == null)
		{
			return;
		}
		if (!_hasSetSlots)
		{
			_coalSlot = car.Definition.LoadSlots.FindIndex((LoadSlot loadSlot) => loadSlot.RequiredLoadIdentifier == "coal");
			_waterSlot = car.Definition.LoadSlots.FindIndex((LoadSlot loadSlot) => loadSlot.RequiredLoadIdentifier == "water");
			_hasSetSlots = true;
		}
		float num = car.GetLoadInfo(_coalSlot)?.Quantity ?? 0f;
		float num2 = car.GetLoadInfo(_waterSlot)?.Quantity ?? 0f;
		bool flag = num > 0.001f && num2 > 0.001f;
		float num3 = engine.CoalConsumptionRate * dt;
		float num4 = engine.WaterConsumptionRate * dt;
		if (isHost && flag && (num3 > 0.001f || num4 > 0.001f))
		{
			num = Mathf.Max(0f, num - num3);
			num2 = Mathf.Max(0f, num2 - num4);
			car.SetLoadInfo(_coalSlot, new CarLoadInfo("coal", num));
			car.SetLoadInfo(_waterSlot, new CarLoadInfo("water", num2));
		}
		bool hasWaterAndCoal = engine.HasWaterAndCoal;
		engine.HasWaterAndCoal = num > 0.001f && num2 > 0.001f;
		if (hasWaterAndCoal != engine.HasWaterAndCoal)
		{
			InvokeHasFuelDidChange();
			if (air is LocomotiveAirSystem locomotiveAirSystem)
			{
				locomotiveAirSystem.HasFuel = engine.HasWaterAndCoal;
			}
		}
	}

	internal Car FuelCar()
	{
		if (!hasTender)
		{
			return this;
		}
		if (TryGetTender(out var tender))
		{
			return tender;
		}
		return null;
	}

	internal bool TryGetTender(out Car tender)
	{
		if (hasTender && TryGetAdjacentCar(EndToLogical(End.R), out tender) && tender.Archetype == CarArchetype.Tender)
		{
			return true;
		}
		tender = null;
		return false;
	}

	protected override void UpdateCabControls()
	{
		if (cabControls != null)
		{
			cabControls.johnsonBar.Value = Mathf.InverseLerp(-1f, 1f, engine.reverser);
			cabControls.regulator.Value = engine.regulator;
			cabControls.boilerPressure.Value = engine.maximumBoilerPressure;
		}
		base.UpdateCabControls();
	}

	protected override void ConnectBodyControls()
	{
		if (cabControls == null)
		{
			Debug.LogWarning(base.DisplayName + " Missing cabControls");
			return;
		}
		if (TryGetControl(ControlPurpose.Throttle, out var throttleControl))
		{
			cabControls.regulator = throttleControl;
			throttleControl.ConfigureSnap(100);
			throttleControl.OnValueChanged += delegate(float value)
			{
				base.ControlHelper.Throttle = value;
			};
			throttleControl.CheckAuthorized = () => StateManager.CheckAuthorizedToSendMessage(new PropertyChange(id, PropertyChange.Control.Throttle, 0));
			throttleControl.tooltipText = () => BaseLocomotive.Percent(throttleControl.Value);
		}
		if (TryGetControl(ControlPurpose.Reverser, out var foundControl))
		{
			cabControls.johnsonBar = foundControl;
			foundControl.ConfigureSnap(40);
			foundControl.OnValueChanged += delegate(float value)
			{
				float reverser = Mathf.Lerp(-1f, 1f, value);
				base.ControlHelper.Reverser = reverser;
			};
			foundControl.CheckAuthorized = () => StateManager.CheckAuthorizedToSendMessage(new PropertyChange(id, PropertyChange.Control.Reverser, 0));
			foundControl.tooltipText = () => (Mathf.Abs(engine.reverser) < 0.1f) ? "Centered" : string.Format("{0}% {1}", Mathf.Abs(Mathf.RoundToInt(engine.reverser * 100f)), (engine.reverser < 0f) ? "Reverse" : "Forward");
		}
		LocomotiveCabControlsHookup locomotiveCabControlsHookup = cabControls;
		if ((object)locomotiveCabControlsHookup.regulator == null)
		{
			locomotiveCabControlsHookup.regulator = DummyControl();
		}
		locomotiveCabControlsHookup = cabControls;
		if ((object)locomotiveCabControlsHookup.johnsonBar == null)
		{
			locomotiveCabControlsHookup.johnsonBar = DummyControl();
		}
		base.ConnectBodyControls();
	}

	protected override bool RequiresConnectionToEnd(End end)
	{
		if (hasTender)
		{
			return end == End.R;
		}
		return false;
	}

	public override Location PositionWheelBoundsFront(Location wheelBoundsF, Graph graph, MovementInfo info, bool update)
	{
		bool isFirstPosition = _isFirstPosition;
		if (update)
		{
			_isFirstPosition = false;
		}
		SteamLocomotiveDefinition locoDefinition = LocoDefinition;
		Location location = default(Location);
		Location loc = default(Location);
		Location location2 = graph.LocationByMoving(wheelBoundsF, wheelInsetF, checkSwitchAgainstMovement: false, Graph.EndOfTrackHandling.Unclamped);
		float num = locoDefinition.PositionHead;
		for (int i = 0; i < locoDefinition.Wheelsets.Count; i++)
		{
			SteamLocomotiveDefinition.Wheelset wheelset = locoDefinition.Wheelsets[i];
			Location location3;
			Location location4;
			if (wheelset.Length < 0.1f)
			{
				float num2 = num - wheelset.Offset;
				num -= num2;
				location2 = graph.LocationByMoving(location2, -1f * num2);
				location3 = location2;
				location4 = location2;
			}
			else
			{
				float num3 = num - (wheelset.Offset + wheelset.Length / 2f);
				num -= num3;
				location2 = graph.LocationByMoving(location2, -1f * num3);
				location3 = location2;
				num3 = wheelset.Length;
				num -= num3;
				location2 = graph.LocationByMoving(location2, -1f * num3);
				location4 = location2;
			}
			if (i == locoDefinition.MainDriverIndex && update)
			{
				location = location3;
				loc = location4;
			}
		}
		float num4 = num - (locoDefinition.PositionTail + wheelInsetR);
		Location location5 = graph.LocationByMoving(location2, -1f * num4);
		if (!update)
		{
			return location5;
		}
		UpdateBaseLocations(wheelBoundsF, location5, graph, isFirstPosition);
		if (locoDefinition.MainDriverIndex < 0 || locoDefinition.MainDriverIndex >= locoDefinition.Wheelsets.Count)
		{
			throw new Exception("MainDriverIndex out of range");
		}
		PositionAccuracy accuracy = (base.IsVisible ? PositionAccuracy.High : PositionAccuracy.Standard);
		SteamLocomotiveDefinition.Wheelset wheelset2 = locoDefinition.Wheelsets[locoDefinition.MainDriverIndex];
		Graph.PositionRotation positionRotation = graph.GetPositionRotation(location, accuracy);
		Graph.PositionRotation positionRotation2 = graph.GetPositionRotation(loc, accuracy);
		SetBodyPosition(wheelBoundsF, positionRotation, positionRotation2, wheelset2.Offset, isFirstPosition);
		UpdateCurvatureForLocation(location);
		SubcomponentsApplyDistanceMoved(info);
		if (_rollingPlayer != null)
		{
			_rollingPlayer.SetVelocity(base.velocity);
		}
		FireOnMovement(info);
		return location5;
	}

	private void SubcomponentsApplyDistanceMoved(MovementInfo info)
	{
		float absReverser = Mathf.Abs(locomotiveControl.AbstractReverser);
		float absThrottle = Mathf.Abs(locomotiveControl.AbstractThrottle);
		float driverPhase = ((_wheelAnimator == null) ? 0f : _wheelAnimator.DriverPhase);
		foreach (ISteamLocomotiveSubcomponent subcomponent in _subcomponents)
		{
			subcomponent.ApplyDistanceMoved(info, _wheelVelocity, absReverser, absThrottle, driverPhase);
		}
	}

	public override float CutoffSettingForVelocity(float velocityMps)
	{
		if (_cutoffSettings == null)
		{
			_cutoffSettings = new float[CutoffSpeeds.Length];
			for (int i = 0; i < CutoffSpeeds.Length; i++)
			{
				float absVelocityMph = CutoffSpeeds[i];
				float num = 0f;
				float num2 = 1f;
				do
				{
					float num3 = TrainMath.ReverserPowerMultiplier(num2, absVelocityMph, maxSpeedMph);
					if (num3 <= num)
					{
						num2 += 0.05f;
						break;
					}
					num = num3;
					num2 -= 0.05f;
				}
				while (num2 > 0f);
				_cutoffSettings[i] = num2;
			}
		}
		float num4 = Mathf.Abs(velocityMps) * 2.23694f;
		for (int j = 0; j < _cutoffSettings.Length; j++)
		{
			if (!(CutoffSpeeds[j] < num4))
			{
				if (j == 0)
				{
					return _cutoffSettings[j];
				}
				return Mathf.Lerp(_cutoffSettings[j - 1], _cutoffSettings[j], Mathf.InverseLerp(CutoffSpeeds[j - 1], CutoffSpeeds[j], num4));
			}
		}
		return _cutoffSettings[^1];
	}
}
