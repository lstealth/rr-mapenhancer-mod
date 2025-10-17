using System.Collections.Generic;
using System.Linq;
using Audio;
using Game.Messages;
using Game.State;
using Model.Definition.Components;
using Model.Definition.Data;
using Model.Ops;
using Model.Physics;
using RollingStock;
using RollingStock.Diesel;
using Serilog;
using UnityEngine;

namespace Model;

public class DieselLocomotive : BaseLocomotive
{
	public PrimeMover primeMover;

	private IPrimeMoverAudioPlayer _primeMoverAudioPlayer;

	private List<DieselExhaustParticleController> _particleControllers = new List<DieselExhaustParticleController>();

	private DieselLocomotiveDefinition LocoDefinition => (DieselLocomotiveDefinition)DefinitionInfo.Definition;

	public override float NormalizedTractiveEffort => primeMover.NormalizedTractiveEffort;

	public override float RatedTractiveEffort => primeMover.startingTractiveEffort;

	public override bool HasFuel => primeMover.HasFuel;

	protected override float AdhesiveWeight => base.Weight;

	protected override void UnloadModels()
	{
		base.UnloadModels();
		if (_primeMoverAudioPlayer != null)
		{
			_primeMoverAudioPlayer.NormalizedExhaustOutputEvent = null;
		}
		_primeMoverAudioPlayer = null;
		_particleControllers.Clear();
	}

	protected override void DidLoadModels()
	{
		base.DidLoadModels();
		_particleControllers = BodyTransform.GetComponentsInChildren<DieselExhaustParticleController>().ToList();
		_primeMoverAudioPlayer = BodyTransform.GetComponentInChildren<IPrimeMoverAudioPlayer>();
		if (_primeMoverAudioPlayer != null)
		{
			_primeMoverAudioPlayer.NormalizedExhaustOutputEvent = delegate(float normalizedExhaustOutput)
			{
				foreach (DieselExhaustParticleController particleController in _particleControllers)
				{
					particleController.NormalizedExhaustOutput = normalizedExhaustOutput;
				}
			};
		}
		HornPlayer componentInChildren = BodyTransform.GetComponentInChildren<HornPlayer>();
		if (componentInChildren != null)
		{
			locomotiveControl.audio.horn = componentInChildren;
		}
	}

	protected override void ConnectBodyControls()
	{
		if (TryGetControl(ControlPurpose.Throttle, out var foundControl))
		{
			cabControls.throttle = foundControl;
			foundControl.OnValueChanged += delegate(float value)
			{
				base.ControlHelper.Throttle = value;
			};
			foundControl.CheckAuthorized = () => StateManager.CheckAuthorizedToSendMessage(new PropertyChange(id, PropertyChange.Control.Throttle, 0));
			foundControl.tooltipText = delegate
			{
				int throttleDisplay = locomotiveControl.ThrottleDisplay;
				return (throttleDisplay != 0) ? $"Notch {throttleDisplay}" : "Idle";
			};
		}
		if (TryGetControl(ControlPurpose.Reverser, out var foundControl2))
		{
			cabControls.reverser = foundControl2;
			foundControl2.OnValueChanged += delegate(float value)
			{
				int num = Mathf.RoundToInt(Mathf.Lerp(-1f, 1f, value));
				base.ControlHelper.Reverser = num;
			};
			foundControl2.CheckAuthorized = () => StateManager.CheckAuthorizedToSendMessage(new PropertyChange(id, PropertyChange.Control.Reverser, 0));
			foundControl2.tooltipText = () => (primeMover.reverser >= 0) ? ((primeMover.reverser <= 0) ? "Neutral" : "Forward") : "Reverse";
		}
		LocomotiveCabControlsHookup locomotiveCabControlsHookup = cabControls;
		if ((object)locomotiveCabControlsHookup.reverser == null)
		{
			locomotiveCabControlsHookup.reverser = DummyControl();
		}
		locomotiveCabControlsHookup = cabControls;
		if ((object)locomotiveCabControlsHookup.throttle == null)
		{
			locomotiveCabControlsHookup.throttle = DummyControl();
		}
		base.ConnectBodyControls();
	}

	protected override void UpdateCabControls()
	{
		if (cabControls != null)
		{
			cabControls.reverser.Value = ((primeMover.reverser < 0) ? 0f : ((primeMover.reverser > 0) ? 1f : 0.5f));
			cabControls.throttle.Value = (float)primeMover.notch / 8f;
		}
		if (_primeMoverAudioPlayer != null)
		{
			_primeMoverAudioPlayer.Notch = (primeMover.HasFuel ? primeMover.notch : 0);
		}
		base.UpdateCabControls();
	}

	protected override void FinishSetup()
	{
		primeMover = base.gameObject.AddComponent<PrimeMover>();
		primeMover.startingTractiveEffort = LocoDefinition.StartingTractiveEffort;
		maxSpeedMph = Random.Range(63f, 66f);
		Log.Debug("Locomotive {car} maxSpeedMph = {maxSpeedMph}", this, maxSpeedMph);
		base.FinishSetup();
	}

	protected override void PeriodicUpdate(float dt)
	{
		base.PeriodicUpdate(dt);
		bool isHost = StateManager.IsHost;
		if (this == null)
		{
			return;
		}
		float num = this.GetLoadInfo(0)?.Quantity ?? 0f;
		float num2 = primeMover.FuelConsumptionRate * dt;
		if (isHost && num2 > 0.001f)
		{
			num = Mathf.Max(0f, num - num2);
			this.SetLoadInfo(0, new CarLoadInfo("diesel-fuel", num));
		}
		bool hasFuel = primeMover.HasFuel;
		primeMover.HasFuel = num > 0.001f;
		if (hasFuel != primeMover.HasFuel)
		{
			InvokeHasFuelDidChange();
			if (air is LocomotiveAirSystem locomotiveAirSystem)
			{
				locomotiveAirSystem.HasFuel = primeMover.HasFuel;
			}
		}
	}

	protected override LocomotiveControlAdapter CreateLocomotiveControl()
	{
		DieselLocomotiveControl dieselLocomotiveControl = base.gameObject.AddComponent<DieselLocomotiveControl>();
		dieselLocomotiveControl.primeMover = primeMover;
		return dieselLocomotiveControl;
	}

	protected override float CalculateTractiveEffort(float signedVelocityMph)
	{
		return primeMover.CalculateTractiveEffort(Mathf.Abs(signedVelocityMph));
	}

	public override float MaxTractiveEffortAtVelocity(float absVelocityMph)
	{
		return primeMover.MaxTractiveEffort(absVelocityMph);
	}

	public override float CutoffSettingForVelocity(float velocityMps)
	{
		return 1f;
	}
}
