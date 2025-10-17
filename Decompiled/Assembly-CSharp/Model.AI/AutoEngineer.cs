using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game;
using Game.Messages;
using Game.Notices;
using Game.State;
using Helpers;
using JetBrains.Annotations;
using RollingStock;
using RollingStock.Controls;
using Serilog;
using Serilog.Events;
using Track;
using Track.Signals;
using UnityEngine;

namespace Model.AI;

public class AutoEngineer : MonoBehaviour
{
	public class Targets
	{
		public struct Target
		{
			public float SpeedMph { get; set; }

			public float Distance { get; set; }

			public string Reason { get; set; }

			public Target(float speedMph, float distance, string reason)
			{
				SpeedMph = speedMph;
				Distance = distance;
				Reason = reason;
			}

			public bool Equals(Target other)
			{
				if (SpeedMph.Equals(other.SpeedMph))
				{
					return Distance.Equals(other.Distance);
				}
				return false;
			}

			public override bool Equals(object obj)
			{
				if (obj is Target other)
				{
					return Equals(other);
				}
				return false;
			}

			public override int GetHashCode()
			{
				return HashCode.Combine(SpeedMph, Distance);
			}

			public override string ToString()
			{
				return $"{SpeedMph:F1}mph @ {Distance:F1}m ({Reason})";
			}
		}

		public readonly float MaxSpeedMph;

		public readonly List<Target> AllTargets;

		public readonly float AverageGradeUnder;

		public readonly float AverageGradeAhead;

		public readonly bool ChangeDirection;

		public readonly AutoEngineerMode Mode;

		public readonly StopAnnounce? StopAnnounce;

		[CanBeNull]
		public readonly CTCSignal NextSignal;

		public bool Equals(Targets other)
		{
			if (other == null)
			{
				return false;
			}
			float maxSpeedMph = MaxSpeedMph;
			if (maxSpeedMph.Equals(other.MaxSpeedMph))
			{
				maxSpeedMph = AverageGradeUnder;
				if (maxSpeedMph.Equals(other.AverageGradeUnder))
				{
					maxSpeedMph = AverageGradeAhead;
					if (maxSpeedMph.Equals(other.AverageGradeAhead) && ChangeDirection == other.ChangeDirection && Mode == other.Mode && StopAnnounce == other.StopAnnounce)
					{
						return AllTargets.SequenceEqual(other.AllTargets);
					}
				}
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is Targets other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(MaxSpeedMph, AllTargets, AverageGradeUnder, AverageGradeAhead, ChangeDirection, (int)Mode, StopAnnounce);
		}

		public Targets()
			: this(0f, new List<Target>(), 0f, 0f, changeDirection: false, AutoEngineerMode.Off, null, null)
		{
		}

		public Targets(float maxSpeedMph, List<Target> targets, float averageGradeUnder, float averageGradeAhead, bool changeDirection, AutoEngineerMode mode, StopAnnounce? stopAnnounce, CTCSignal nextSignal)
		{
			MaxSpeedMph = maxSpeedMph;
			AllTargets = targets ?? new List<Target>();
			AverageGradeUnder = averageGradeUnder;
			AverageGradeAhead = averageGradeAhead;
			ChangeDirection = changeDirection;
			Mode = mode;
			StopAnnounce = stopAnnounce;
			NextSignal = nextSignal;
		}

		public override string ToString()
		{
			return $"(max={MaxSpeedMph:F1}, gu={AverageGradeUnder:F1}%, ga={AverageGradeAhead:F1})";
		}
	}

	private enum State
	{
		Stopped,
		Starting,
		Running
	}

	private enum BlowPattern
	{
		Stopped,
		Forward,
		Reverse
	}

	private Targets _targets = new Targets();

	private LocomotiveControlHelper _control;

	private AutoOiler _oiler;

	private Graph _graph;

	private Coroutine _loopCoroutine;

	private readonly CoroutineKeepalive _loopKeepalive = CreateKeepalive();

	private AutoEngineerConfig _config;

	private float _pendingRunDuration;

	private Serilog.ILogger _log;

	[SerializeField]
	private PIDController throttleController = new PIDController();

	[SerializeField]
	private PIDController independentController = new PIDController();

	[SerializeField]
	private PIDController trainBrakeController = new PIDController();

	[Tooltip("Set true to kill the coroutine and test keepalive.")]
	[SerializeField]
	private bool testKeepalive;

	private const float BrakeSetThreshold = 5f;

	private float _debugContextualTargetVelocity;

	private State _lastState;

	private List<Car> _cachedCoupled;

	private float? _cachedCoupledWeightLbs;

	private List<BaseLocomotive> _cachedLocomotives;

	private List<Car> _cachedAirOpen;

	private static readonly string[] AnnounceStopSignal = new string[4] { "Holding at a red board.", "Looking at a red board.", "Holding at a stop signal.", "Stopped at a red board." };

	private static readonly string[] AnnounceSwitchAgainst = new string[2] { "Stopped for a switch lined against us.", "Switch lined against us." };

	private static readonly string[] AnnounceFusee = new string[2] { "Stopped for a fusee.", "Fusee in the gauge." };

	private static readonly string[] AnnounceOtherTrain = new string[1] { "Lookin' at another train." };

	private static readonly string[] AnnounceSwitchFouled = new string[2] { "Another train is on our switch.", "Switch is fouled." };

	public BaseLocomotive Locomotive { get; private set; }

	public bool Run { get; set; }

	private float TargetSpeedMph
	{
		get
		{
			foreach (Targets.Target allTarget in _targets.AllTargets)
			{
				if (allTarget.Distance < 1f)
				{
					return allTarget.SpeedMph;
				}
			}
			return _targets.MaxSpeedMph;
		}
	}

	private float TargetDistance
	{
		get
		{
			if (_targets.AllTargets.Count <= 0)
			{
				return 0f;
			}
			return _targets.AllTargets[0].Distance;
		}
	}

	private bool WantsChangeDirection => _targets.ChangeDirection;

	public bool WaitingForBrakes { get; set; }

	private IEnumerator WaitForChange => WaitFixed(0.5f);

	private float LocomotiveVelocityMphAbs => Locomotive.VelocityMphAbs;

	private bool IsStopped => IsZero(Locomotive.velocity);

	private string LogPrefix => $"{Time.fixedTime:F2} {Locomotive.velocity * 2.23694f:F1}mph ({_targets}, ctx {ContextualTargetVelocity() * 2.23694f:F1}): ";

	private float WeightParameter => Mathf.InverseLerp(_config.weightTonsLight, _config.weightTonsHeavy, CachedCoupledWeightLbs() / 2000f);

	private bool IsStoppedAndShouldStay
	{
		get
		{
			if (!IsStopped)
			{
				return false;
			}
			if (IsZero(ContextualTargetVelocity()))
			{
				return true;
			}
			if (IsZero(TargetSpeedMph))
			{
				return Mathf.Abs(TargetDistance) < 5f;
			}
			return false;
		}
	}

	[ContextMenu("Reset PIDs")]
	private void ResetPIDs()
	{
		throttleController.Reset();
		independentController.Reset();
		trainBrakeController.Reset();
	}

	internal static CoroutineKeepalive CreateKeepalive()
	{
		return new CoroutineKeepalive(60f, scaledTime: true);
	}

	private void OnEnable()
	{
		Locomotive = GetComponent<BaseLocomotive>();
		_log = Log.ForContext<AutoEngineer>().ForContext("locomotive", Locomotive.DisplayName);
		_config = TrainController.Shared.autoEngineerConfig;
		_control = Locomotive.ControlHelper;
		_config.throttlePID.CopyTo(throttleController);
		_config.independentPID.CopyTo(independentController);
		_config.trainBrakePID.CopyTo(trainBrakeController);
		_oiler = base.gameObject.AddComponent<AutoOiler>();
		_loopCoroutine = StartCoroutine(Loop());
		_loopKeepalive.Start(this, KeepaliveTimedOut);
	}

	private void OnDisable()
	{
		_loopKeepalive.Stop();
		StopCoroutine(_loopCoroutine);
		_loopCoroutine = null;
	}

	private void KeepaliveTimedOut()
	{
		_log.Warning("Keepalive timed out! Restarting.");
		StopCoroutine(_loopCoroutine);
		_loopCoroutine = StartCoroutine(Loop());
	}

	internal static bool IsZero(float value)
	{
		return Mathf.Abs(value) < 0.01f;
	}

	private static bool FloatEqual(float a, float b)
	{
		return Mathf.Abs(a - b) < 0.001f;
	}

	public void SetTargets(Targets targets)
	{
		if (!targets.Equals(_targets))
		{
			_targets = targets;
			LogInfo("SetTargets {0}", _targets);
		}
	}

	[StringFormatMethod("message")]
	private void LogInfo(string message, params object[] args)
	{
		if (_log.IsEnabled(LogEventLevel.Information))
		{
			string text = string.Format(message, args);
			_log.Information(LogPrefix + text);
		}
	}

	[StringFormatMethod("message")]
	private void LogError(string message, params object[] args)
	{
		if (_log.IsEnabled(LogEventLevel.Error))
		{
			string text = string.Format(message, args);
			_log.Error(LogPrefix + text);
		}
	}

	private IEnumerator Loop()
	{
		while (true)
		{
			if (!Run || Locomotive.set == null)
			{
				_loopKeepalive.StillAlive();
				yield return WaitFixed(0.5f);
				continue;
			}
			while (Run)
			{
				_loopKeepalive.StillAlive();
				State state = ((!IsStoppedAndShouldStay) ? ((IsStopped && !IsZero(TargetSpeedMph)) ? State.Starting : State.Running) : State.Stopped);
				if (_lastState == State.Stopped && state == State.Starting)
				{
					PostStopNotice(null);
				}
				else if (_lastState == State.Running && state == State.Stopped)
				{
					_control.CylinderCocksOpen = false;
					_control.LocomotiveBrake = 1f;
					yield return Blow(BlowPattern.Stopped);
					if (_targets.StopAnnounce.HasValue)
					{
						AnnounceStop(_targets.StopAnnounce.Value, _targets.NextSignal);
					}
					HeadlightDim();
				}
				if (state != _lastState)
				{
					LogInfo("State {0} -> {1}", _lastState, state);
				}
				if (state == State.Stopped)
				{
					_oiler.Configure(Locomotive, CachedCoupled());
				}
				_oiler.SetStopped(state == State.Stopped);
				switch (state)
				{
				case State.Stopped:
					if (Mathf.Abs(_targets.AverageGradeUnder) < 0.2f)
					{
						_control.TrainBrake = 0f;
					}
					else
					{
						TrainBrakeSetToAtLeast(10f);
					}
					_control.LocomotiveBrake = 1f;
					_control.Throttle = 0f;
					_control.Reverser = 0f;
					yield return WaitFixed(1f);
					break;
				case State.Starting:
				{
					float t0 = Time.fixedTime;
					yield return StartMovement();
					ReportRunElapsed(Time.fixedTime - t0);
					_control.Bell = false;
					break;
				}
				case State.Running:
					yield return MaintainSpeed();
					ReportRunElapsed(0f, force: true);
					break;
				}
				_lastState = state;
			}
			PostStopNotice(null);
		}
	}

	private void EmergencyStop()
	{
		LogError("Emergency Stop!");
		_control.TrainBrake = 1f;
		_control.LocomotiveBrake = 1f;
		Locomotive.set.SetVelocity(0f, Locomotive.EnumerateCoupled().ToList());
	}

	internal float CalculateTotalAvailableBraking()
	{
		float absVelocity = Mathf.Abs(Locomotive.velocity);
		float num = 0f;
		foreach (Car item in AirOpenCars())
		{
			num += item.CalculateBrakingForce(1f, absVelocity);
		}
		return num;
	}

	internal float ContextualTargetVelocity()
	{
		if (WantsChangeDirection || _config == null)
		{
			return 0f;
		}
		float totalAvailableBraking = CalculateTotalAvailableBraking();
		float num = Mathf.Abs(_targets.MaxSpeedMph * 0.44703928f);
		float weightParameter = WeightParameter;
		foreach (Targets.Target allTarget in _targets.AllTargets)
		{
			float value = Mathf.Abs(allTarget.SpeedMph);
			float num2 = Mathf.Abs(allTarget.SpeedMph * 0.44703928f);
			if (num2 < num)
			{
				float num5;
				if (allTarget.Distance > 0.1f)
				{
					float num3 = _config.maxVelocityForDistanceLight.FindTimeForValue(value, 0.1f);
					float num4 = _config.maxVelocityForDistanceHeavy.FindTimeForValue(value, 0.1f);
					float a = _config.maxVelocityForDistanceLight.Evaluate(allTarget.Distance + num3);
					float b = _config.maxVelocityForDistanceHeavy.Evaluate(allTarget.Distance + num4);
					float b2 = Mathf.Lerp(a, b, weightParameter) * 0.44703928f;
					num5 = Mathf.Min(CalculateMaxVelocityToSlowToSpeedAtDistance(num2, allTarget.Distance, totalAvailableBraking), b2);
				}
				else
				{
					num5 = num2;
				}
				if (num5 < num)
				{
					num = num5;
				}
			}
		}
		return Mathf.Sign(_targets.MaxSpeedMph) * num;
	}

	private float CalculateMaxVelocityToSlowToSpeedAtDistance(float velocityFinalMpsAbs, float distance, float totalAvailableBraking)
	{
		distance = Mathf.Max(0f, distance);
		float num = CachedCoupledWeightLbs() * 0.4536f;
		float num2 = totalAvailableBraking / num;
		return Mathf.Sqrt(Mathf.Pow(velocityFinalMpsAbs, 2f) + 2f * num2 * distance);
	}

	public static float CalculateDistanceToSlowToSpeed(float initialVelocityMps, float finalVelocityMps, float totalAvailableBraking, float trainMass)
	{
		initialVelocityMps = Mathf.Max(initialVelocityMps, finalVelocityMps);
		float num = totalAvailableBraking / trainMass;
		float b = (Mathf.Pow(initialVelocityMps, 2f) - Mathf.Pow(finalVelocityMps, 2f)) / (2f * num);
		return Mathf.Max(0f, b);
	}

	private bool WantsMovement()
	{
		if (Run && !IsZero(TargetSpeedMph))
		{
			return !WantsChangeDirection;
		}
		return false;
	}

	private void UpdateVelocityValues(out float velocity, out float targetVelocitySign, out bool forward)
	{
		velocity = ContextualTargetVelocity();
		targetVelocitySign = Mathf.Sign(velocity);
		forward = targetVelocitySign > 0f;
	}

	private IEnumerator StartMovement()
	{
		InvalidateCachedCars();
		if (!IsZero(_control.LocomotiveBrake))
		{
			_control.LocomotiveBrake = 1f;
			yield return WaitFixed(1f);
		}
		if (!WantsMovement())
		{
			yield break;
		}
		UpdateVelocityValues(out var velocity, out var targetVelocitySign, out var forward);
		HeadlightOn(forward);
		_control.TrainBrake = 0f;
		while (AverageTrainBrakeCylinder() > 5f)
		{
			_loopKeepalive.StillAlive();
			FixMuCutOutIfNeeded();
			WaitingForBrakes = true;
			yield return WaitForChange;
			if (!WantsMovement())
			{
				WaitingForBrakes = false;
				yield break;
			}
		}
		WaitingForBrakes = false;
		StartCoroutine(StartingEffects());
		yield return Blow(forward ? BlowPattern.Forward : BlowPattern.Reverse);
		UpdateVelocityValues(out velocity, out targetVelocitySign, out forward);
		HeadlightOn(forward);
		float sequenceTimeMultiplier = SequenceTimeMultiplier();
		yield return WaitFixed(sequenceTimeMultiplier * 0.5f);
		_control.Reverser = (forward ? 1 : (-1));
		yield return WaitFixed(sequenceTimeMultiplier * 0.5f);
		var (throttle, locomotiveBrake) = CalculateSettingsForHolding();
		_control.Throttle = throttle;
		_control.LocomotiveBrake = locomotiveBrake;
		while (AverageTrainBrakeCylinder() > 1f && LocomotiveVelocityMphAbs < 1f)
		{
			WaitingForBrakes = true;
			_loopKeepalive.StillAlive();
			FixMuCutOutIfNeeded();
			yield return WaitFixed(0.5f);
			if (!WantsMovement())
			{
				WaitingForBrakes = false;
				yield break;
			}
		}
		WaitingForBrakes = false;
		_control.LocomotiveBrake = 0f;
		while (LocomotiveVelocityMphAbs < 1f)
		{
			_loopKeepalive.StillAlive();
			UpdateVelocityValues(out velocity, out targetVelocitySign, out forward);
			HeadlightOn(forward);
			FixMuCutOutIfNeeded();
			_control.Reverser = targetVelocitySign * CutOffSettingForVelocity(Locomotive.velocity);
			_control.Throttle = CalculateThrottleForTargetVelocityStarting(velocity, 0f, 0.5f);
			yield return WaitForChange;
			if (!WantsMovement())
			{
				break;
			}
		}
	}

	private IEnumerator StartingEffects()
	{
		yield return new WaitForSeconds(UnityEngine.Random.Range(0.5f, 1.5f));
		bool bell = _targets.Mode == AutoEngineerMode.Road;
		_control.Bell = bell;
		int minimumTime = UnityEngine.Random.Range(15, 20);
		float t0 = Time.time;
		while (WantsMovement() && Time.time - t0 < (float)minimumTime)
		{
			yield return WaitFixed(1f);
			_control.CylinderCocksOpen = LocomotiveVelocityMphAbs > 0.1f;
		}
		_control.CylinderCocksOpen = false;
		_control.Bell = false;
	}

	private (float throttle, float independent) CalculateSettingsForHolding()
	{
		float num = SumGravityForce();
		float num2 = Locomotive.CalculateBrakingForce(1f, 0f);
		float ratedTractiveEffort = Locomotive.RatedTractiveEffort;
		float num3 = num - Mathf.Sign(num) * num2;
		if (SignChanged(num3, num))
		{
			num3 = 0f;
		}
		if (SameSignOrZero(num3, Locomotive.velocity))
		{
			LogInfo("CalculateSettingsForHolding: {0}, {1}, {2}, {3}, {4} (no throttle)", num, num2, ratedTractiveEffort, num3, Locomotive.velocity);
			return (throttle: 0f, independent: 1f);
		}
		float num4 = RoundUpToNotch(Mathf.Abs(num) / ratedTractiveEffort);
		LogInfo("CalculateSettingsForHolding:{0}, {1}, {2}, {3}, {4} -> {5}", num, num2, ratedTractiveEffort, num3, Locomotive.velocity, num4);
		return (throttle: num4, independent: 1f);
	}

	private float CalculateThrottleForTargetVelocityStarting(float targetVelocity, float brakingForceN, float aggressiveness = 1f)
	{
		float num = Mathf.Abs(targetVelocity) * 2.23694f;
		float locomotiveMph = LocomotiveVelocityMphAbs;
		float num2 = CachedMuConnectedLocomotives().Sum((BaseLocomotive l) => l.MaxTractiveEffortAtVelocity(locomotiveMph));
		float num3 = SumGravityForce() * 0.22480904f;
		float num4 = Mathf.Abs(num3);
		float num5 = ((!SignChanged(targetVelocity, num3)) ? (0f - num4) : num4);
		num5 += Mathf.Sign(targetVelocity) * brakingForceN * 0.22480904f;
		float num6 = num - locomotiveMph;
		if (num6 > 0f)
		{
			float value = num5 + num2 * aggressiveness * Mathf.InverseLerp(-1f, 3f, num6);
			return RoundUpToNotch(Mathf.InverseLerp(0f, num2, value));
		}
		return 0f;
	}

	private static float RoundUpToNotch(float throttle)
	{
		return Mathf.Ceil(throttle * 8f) / 8f;
	}

	private static float RoundDownToNotch(float throttle)
	{
		return Mathf.Floor(throttle * 8f) / 8f;
	}

	private bool SameSignOrZero(float value, float reference)
	{
		if (!IsZero(value))
		{
			return FloatEqual(Mathf.Sign(value), Mathf.Sign(reference));
		}
		return true;
	}

	private IReadOnlyList<Car> CachedCoupled()
	{
		return _cachedCoupled ?? (_cachedCoupled = Locomotive.EnumerateCoupled().ToList());
	}

	private float CachedCoupledWeightLbs()
	{
		if (!_cachedCoupledWeightLbs.HasValue)
		{
			float num = 0f;
			foreach (Car item in CachedCoupled())
			{
				num += item.Weight;
			}
			_cachedCoupledWeightLbs = num;
		}
		return _cachedCoupledWeightLbs.Value;
	}

	private IReadOnlyList<Car> AirOpenCars()
	{
		if (_cachedAirOpen == null)
		{
			_cachedAirOpen = Locomotive.EnumerateAirOpen().ToList();
		}
		return _cachedAirOpen;
	}

	private IEnumerable<BaseLocomotive> CachedMuConnectedLocomotives()
	{
		if (_cachedLocomotives != null)
		{
			return _cachedLocomotives;
		}
		_cachedLocomotives = new List<BaseLocomotive>();
		foreach (Car item in CachedCoupled())
		{
			if (item is BaseLocomotive baseLocomotive)
			{
				if (baseLocomotive == Locomotive)
				{
					_cachedLocomotives.Add(baseLocomotive);
				}
				else if (baseLocomotive.IsMuEnabled && baseLocomotive.FindMuSourceLocomotive() == Locomotive)
				{
					_cachedLocomotives.Add(baseLocomotive);
				}
			}
		}
		return _cachedLocomotives;
	}

	internal void InvalidateCachedCars()
	{
		_cachedCoupled = null;
		_cachedCoupledWeightLbs = null;
		_cachedAirOpen = null;
		_cachedLocomotives = null;
	}

	internal float CalculateLookaheadDistance()
	{
		float velocityMphAbs = Locomotive.VelocityMphAbs;
		float a = _config.maxVelocityForDistanceLight.FindTimeForValue(velocityMphAbs, 0.1f);
		float b = _config.maxVelocityForDistanceHeavy.FindTimeForValue(velocityMphAbs, 0.1f);
		float weightParameter = WeightParameter;
		return Mathf.Lerp(a, b, weightParameter) + 100f;
	}

	private IEnumerator MaintainSpeed()
	{
		float lastTimestamp = Time.fixedTime;
		ResetPIDs();
		while (ShouldContinue())
		{
			_loopKeepalive.StillAlive();
			if (testKeepalive)
			{
				testKeepalive = false;
				throw new Exception("Testing Keepalive");
			}
			InvalidateCachedCars();
			FixMuCutOutIfNeeded();
			if (!IsStopped && TargetWantsEmergencyStop())
			{
				EmergencyStop();
			}
			float num = (_debugContextualTargetVelocity = ContextualTargetVelocity());
			float num2 = Mathf.Sign(num);
			if (!IsZero(num))
			{
				HeadlightOn(num2 > 0f);
			}
			float velocity = Locomotive.velocity;
			float num3 = Time.fixedTime - lastTimestamp;
			lastTimestamp = Time.fixedTime;
			ReportRunElapsed(num3);
			float num4 = Mathf.Abs(num);
			float num5 = Mathf.Abs(velocity);
			if (IsZero(num4) && TargetDistance < 0f)
			{
				num4 = -1f;
			}
			num5 += _config.paddingForSpeedMph.Evaluate(Mathf.Abs(velocity) * 2.23694f) * 0.44703928f;
			float num7;
			float num6 = (num7 = num4 - num5);
			if (num7 > 0f)
			{
				num7 *= 10f;
			}
			float throttle = throttleController.Compute(num7, num3);
			_control.Throttle = throttle;
			bool flag = !IsZero(_control.Throttle);
			if (flag)
			{
				_control.Reverser = num2 * CutOffSettingForVelocity(velocity);
			}
			float num8 = num6;
			if (num8 < 0f)
			{
				num8 = Mathf.Sign(num8) * Mathf.Pow(num8, _config.brakeErrorPower);
			}
			if (ShouldUseLocomotiveBrake())
			{
				float locomotiveBrake = independentController.Compute(num8, num3);
				_control.LocomotiveBrake = locomotiveBrake;
				_control.TrainBrake = 0f;
			}
			else
			{
				int count = AirOpenCars().Count;
				trainBrakeController.derivativeGain = _config.trainBrakeDerivativeGainForNumAirOpenCars.Evaluate(count);
				float num9 = trainBrakeController.Compute(num8, num3);
				if (num9 > 0.01f && num9 > _control.TrainBrake)
				{
					_control.TrainBrake = Mathf.Ceil(num9 * 30f) / 30f;
				}
				else if (num9 < _config.trainBrakeReleaseBelowOutput)
				{
					_control.TrainBrake = 0f;
				}
				if (flag && Locomotive.air.BrakeCylinder.Pressure > 0f && !Locomotive.locomotiveControl.air.IsCutOut)
				{
					_control.BailOff();
				}
			}
			float num10 = 0.5f;
			if (IsZero(num))
			{
				num10 = Mathf.Min(num10, MaxWaitForStopAtDistance(TargetDistance));
			}
			yield return WaitFixed(num10);
		}
		bool ShouldContinue()
		{
			if (!Run || IsStopped)
			{
				return false;
			}
			return true;
		}
		bool TargetWantsEmergencyStop()
		{
			if (_targets.AllTargets.Count == 0)
			{
				return false;
			}
			Targets.Target target = _targets.AllTargets[0];
			if (!_targets.StopAnnounce.HasValue)
			{
				return false;
			}
			if (target.Distance < -3f && IsZero(target.SpeedMph))
			{
				return !WantsChangeDirection;
			}
			return false;
		}
	}

	private void FixMuCutOutIfNeeded()
	{
		if (Locomotive.IsMuEnabled)
		{
			_log.Information("Disabling MU.");
			Locomotive.ControlProperties[PropertyChange.Control.Mu] = false;
			Say("Turning off MU.");
		}
		if (Locomotive.ControlProperties[PropertyChange.Control.CutOut].BoolValue && CountCoupledLocomotives() == 1)
		{
			_log.Information("Disabling Cut-Out.");
			Locomotive.ControlProperties[PropertyChange.Control.CutOut] = false;
			Say("Cutting in - we're the only engine.");
		}
	}

	private int CountCoupledLocomotives()
	{
		int num = 0;
		foreach (Car item in CachedCoupled())
		{
			if (item.IsLocomotive)
			{
				num++;
			}
		}
		return num;
	}

	private static float MaxWaitForStopAtDistance(float targetDistance)
	{
		float num = Mathf.Abs(targetDistance);
		if (!(num < 50f))
		{
			if (num < 100f)
			{
				return 0.75f;
			}
			return 1f;
		}
		return 0.5f;
	}

	private bool ShouldUseLocomotiveBrake()
	{
		foreach (Car item in AirOpenCars())
		{
			if (!(item == Locomotive) && !item.air.DefersToLocomotiveAir)
			{
				return false;
			}
		}
		return true;
	}

	private void TrainBrakeMakeSet(float psi)
	{
		_control.TrainBrakeMakeSet(psi);
		LogInfo("TrainBrakeMakeSet {0} -> {1}", psi, _control.TrainBrake * 90f);
	}

	private void TrainBrakeSetToAtLeast(float psi)
	{
		float trainBrakeSet = _control.TrainBrakeSet;
		if (trainBrakeSet < psi)
		{
			TrainBrakeMakeSet(psi - trainBrakeSet);
		}
	}

	private float AverageTrainBrakeCylinder()
	{
		return AverageTrainBrakeValues().Cylinder;
	}

	private IEnumerable<Car> TrainBrakeCars()
	{
		return AirOpenCars().Where(IncludeCar);
		bool IncludeCar(Car car)
		{
			if (car != Locomotive)
			{
				return !car.air.DefersToLocomotiveAir;
			}
			return false;
		}
	}

	private (float Cylinder, float Line, float Reservoir) AverageTrainBrakeValues()
	{
		var (num, num2, num3, num4) = TrainBrakeCars().Aggregate((0f, 0f, 0f, 0), ((float, float, float, int) tuple2, Car car) => (tuple2.Item1 + car.air.BrakeCylinder.Pressure, tuple2.Item2 + car.air.BrakeLine.Pressure, tuple2.Item3 + car.air.BrakeReservoir.Pressure, tuple2.Item4 + 1));
		if (num4 == 0)
		{
			return (Cylinder: 0f, Line: 0f, Reservoir: 0f);
		}
		return (Cylinder: num / (float)num4, Line: num2 / (float)num4, Reservoir: num3 / (float)num4);
	}

	public bool HandbrakeApplied(out int numHandbrakes)
	{
		numHandbrakes = CachedCoupled().Count((Car c) => c.air.handbrakeApplied);
		return numHandbrakes > 0;
	}

	public bool BrakeLineTogether()
	{
		IReadOnlyList<Car> readOnlyList = AirOpenCars();
		if (!readOnlyList[0].EndGearA.IsAnglecockOpen)
		{
			return !readOnlyList[readOnlyList.Count - 1].EndGearB.IsAnglecockOpen;
		}
		return false;
	}

	public bool BrakesReleasedOnNonAirConnectedCars()
	{
		IReadOnlyList<Car> source = AirOpenCars();
		foreach (Car item in CachedCoupled())
		{
			if (!source.Contains(item) && item.air.BrakeCylinder.Pressure > 5f)
			{
				return false;
			}
		}
		return true;
	}

	public float SumGravityForce()
	{
		Car.LogicalEnd locomotiveEndF = Locomotive.EndToLogical(Car.End.F);
		return CachedCoupled().Sum((Car car) => car.GravityForce * (float)((locomotiveEndF == car.EndToLogical(Car.End.F)) ? 1 : (-1))) * 4.44822f;
	}

	private static bool SignChanged(float a, float b)
	{
		if (IsZero(a) || IsZero(b))
		{
			return false;
		}
		return !FloatEqual(Mathf.Sign(a), Mathf.Sign(b));
	}

	private float CutOffSettingForVelocity(float velocity)
	{
		return (float)Mathf.RoundToInt(Locomotive.CutoffSettingForVelocity(velocity) * 20f) / 20f;
	}

	private void HeadlightOn(bool forward)
	{
		HeadlightToggleLogic.SetHeadlightState(Locomotive.KeyValueObject, forward ? HeadlightToggleLogic.State.ForwardOn : HeadlightToggleLogic.State.RearOn);
	}

	private void HeadlightDim()
	{
		HeadlightToggleLogic.State headlightState = HeadlightToggleLogic.GetHeadlightState(Locomotive.KeyValueObject);
		HeadlightToggleLogic.SetHeadlightState(Locomotive.KeyValueObject, (headlightState != HeadlightToggleLogic.State.RearOn) ? HeadlightToggleLogic.State.ForwardDim : HeadlightToggleLogic.State.RearDim);
	}

	private IEnumerator Blow(int blasts, float duration, float pause)
	{
		WaitForSeconds durationWait = new WaitForSeconds(duration);
		WaitForSeconds pauseWait = new WaitForSeconds(pause);
		for (int i = 0; i < blasts; i++)
		{
			_control.Horn = 1f;
			yield return durationWait;
			_control.Horn = 0f;
			if (i != blasts - 1)
			{
				yield return pauseWait;
				continue;
			}
			break;
		}
	}

	private IEnumerator Blow(BlowPattern pattern)
	{
		float num = SequenceTimeMultiplier();
		if (pattern == BlowPattern.Stopped)
		{
			num = 1f;
		}
		switch (pattern)
		{
		case BlowPattern.Stopped:
			yield return Blow(1, num * 0.4f, num * 0.4f);
			break;
		case BlowPattern.Forward:
			yield return Blow(2, num * 1f, num * 0.5f);
			break;
		case BlowPattern.Reverse:
			yield return Blow(3, num * 0.4f, num * 0.4f);
			break;
		default:
			throw new ArgumentOutOfRangeException("pattern", pattern, null);
		}
	}

	private float SequenceTimeMultiplier()
	{
		float num = StopDistance();
		if (num < 304.80002f)
		{
			if (num < 146.304f)
			{
				return 0.3f;
			}
			return 0.4f;
		}
		if (num < 609.60004f)
		{
			return 0.6f;
		}
		return 1f;
	}

	private float StopDistance()
	{
		foreach (Targets.Target allTarget in _targets.AllTargets)
		{
			if (IsZero(allTarget.SpeedMph))
			{
				return allTarget.Distance;
			}
		}
		return float.PositiveInfinity;
	}

	public void ApplyMovement(MovementInfo info)
	{
		for (int i = 0; i < _targets.AllTargets.Count; i++)
		{
			Targets.Target value = _targets.AllTargets[i];
			value.Distance -= info.Distance;
			_targets.AllTargets[i] = value;
		}
	}

	private void ReportRunElapsed(float unitySeconds, bool force = false)
	{
		float num = TimeWeather.TimeMultiplier * unitySeconds;
		_pendingRunDuration += num;
		if (force || !(_pendingRunDuration < 60f))
		{
			StateManager.Shared.RecordAutoEngineerRunDuration(_pendingRunDuration);
			_pendingRunDuration = 0f;
		}
	}

	private void AnnounceStop(StopAnnounce stopAnnounce, CTCSignal nextSignal)
	{
		Say(stopAnnounce switch
		{
			StopAnnounce.StopSignal => AnnounceStopSignal.RandomElement(), 
			StopAnnounce.SwitchAgainst => AnnounceSwitchAgainst.RandomElement(), 
			StopAnnounce.Fusee => AnnounceFusee.RandomElement(), 
			StopAnnounce.OtherTrain => AnnounceOtherTrain.RandomElement(), 
			StopAnnounce.SwitchFouled => AnnounceSwitchFouled.RandomElement(), 
			StopAnnounce.CTCSwitchLocked => "CTC switch is locked.", 
			_ => "<unknown>", 
		});
		PostStopNotice(stopAnnounce switch
		{
			StopAnnounce.StopSignal => (!(nextSignal != null)) ? "Stop Signal" : ("Stop Signal " + nextSignal.DisplayName), 
			StopAnnounce.SwitchAgainst => "Stopped for Switch Against", 
			StopAnnounce.Fusee => "Stopped for Fusee", 
			StopAnnounce.OtherTrain => "Stopped for Other Train", 
			StopAnnounce.SwitchFouled => "Stopped for Fouled Switch", 
			StopAnnounce.CTCSwitchLocked => "Stopped for Locked CTC Switch", 
			_ => "Unknown", 
		});
	}

	private void Say(string text)
	{
		GetComponent<AutoEngineerPlanner>().Say(text);
	}

	private void PostStopNotice(string message)
	{
		Locomotive.PostNotice("ai-stop", message);
	}

	internal static IEnumerator WaitFixed(float seconds)
	{
		return TrainController.WaitFixed(seconds);
	}
}
