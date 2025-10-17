using System.Collections;
using Game.State;
using Model.Definition;
using RollingStock;
using Serilog;
using UnityEngine;

namespace Model.AI;

public class AutoEngineerCrossingSignaler : AutoEngineerComponentBase
{
	private Coroutine _crossingCoroutine;

	private float _nextCrossingDistance;

	private bool _crossingBlowing;

	private float _lastSignalStop;

	private static bool SignalForCrossings => StateManager.Shared.Storage.AICrossingSignal != CrossingSignalSetting.Off;

	private bool EnableHorn => !base.Planner.IsYardMode;

	private void OnEnable()
	{
	}

	private void OnDisable()
	{
		SetNextCrossingDistance(null);
		_locomotive.ControlHelper.Horn = 0f;
		_locomotive.ControlHelper.Bell = false;
	}

	public void SetNextCrossingDistance(float? nextCrossingDistance)
	{
		if (!nextCrossingDistance.HasValue)
		{
			_nextCrossingDistance = 0f;
			if (_crossingCoroutine != null && !_crossingBlowing)
			{
				_locomotive.ControlHelper.Horn = 0f;
				_locomotive.ControlHelper.Bell = false;
				StopCoroutine(_crossingCoroutine);
				_crossingCoroutine = null;
			}
		}
		else if (SignalForCrossings)
		{
			_nextCrossingDistance = nextCrossingDistance.Value;
			if (_crossingCoroutine == null)
			{
				_crossingCoroutine = StartCoroutine(CrossingCoroutine());
			}
		}
	}

	private IEnumerator CrossingCoroutine()
	{
		int crossingWhistleIndex = Random.Range(0, _config.crossingWhistlePatterns.Length);
		while (_nextCrossingDistance > 0f && SignalForCrossings)
		{
			_crossingBlowing = false;
			while (_locomotive.VelocityMphAbs < 0.1f)
			{
				yield return new WaitForSeconds(1f);
			}
			float num = Mathf.Abs(_locomotive.velocity);
			AnimationCurve pattern = _config.crossingWhistlePatterns[crossingWhistleIndex];
			float patternSpeed = Mathf.Lerp(0.7f, 0.3f, Mathf.InverseLerp(10f, 30f, num * 2.23694f));
			float patternDuration = pattern[pattern.length - 1].time / patternSpeed;
			float num2 = patternDuration * 0.95f;
			float num3 = _nextCrossingDistance / num;
			float num4 = num3 - num2;
			Log.Debug("Crossing: {distance} {duration} {timeUntilStart}", _nextCrossingDistance, num3, num4);
			if (num3 < 20f)
			{
				_locomotive.ControlHelper.Bell = true;
			}
			if (float.IsInfinity(num3))
			{
				break;
			}
			if (num3 > num2)
			{
				yield return new WaitForSeconds(Mathf.Clamp(num4, 0.1f, 1f));
				continue;
			}
			if (num3 < 0f || Time.time - _lastSignalStop < _config.minimumTimeBetweenCrossingWhistles)
			{
				break;
			}
			Log.Debug("Crossing Blowing! {distance} {duration} {patternSpeed}", _nextCrossingDistance, num3, patternSpeed);
			_crossingBlowing = true;
			_locomotive.ControlHelper.Bell = true;
			float t0 = Time.time;
			float t1 = 0f;
			bool isDiesel = _locomotive.Archetype == CarArchetype.LocomotiveDiesel;
			float lastHorn = _locomotive.ControlHelper.Horn;
			while (t1 < patternDuration)
			{
				float num5 = pattern.Evaluate(t1 * patternSpeed);
				if (Mathf.Abs(num5 - lastHorn) > 0.05f && EnableHorn)
				{
					if (isDiesel)
					{
						num5 = Mathf.Pow(num5, 4f);
					}
					_locomotive.ControlHelper.Horn = num5;
					lastHorn = num5;
				}
				yield return new WaitForSeconds(0.05f);
				float time = Time.time;
				float num6 = time - t0;
				float num7 = Mathf.Lerp(0.8f, 1f, Mathf.PerlinNoise(0f, time));
				t1 += num6 * num7;
				t0 = time;
			}
			_locomotive.ControlHelper.Horn = 0f;
			_lastSignalStop = Time.time;
			yield return new WaitForSeconds(2f);
			_locomotive.ControlHelper.Bell = false;
			_crossingBlowing = false;
		}
		_locomotive.ControlHelper.Bell = false;
		_crossingCoroutine = null;
	}

	public override void ApplyMovement(MovementInfo info)
	{
		_nextCrossingDistance -= info.Distance;
		_nextCrossingDistance = Mathf.Max(0f, _nextCrossingDistance);
	}

	public override void WillMove()
	{
		SetNextCrossingDistance(null);
	}
}
