using System.Collections;
using Audio;
using Audio.DynamicChuff;
using Game;
using Model;
using UnityEngine;
using UnityEngine.VFX;

namespace RollingStock.Steam;

public class SteamChuffParticleController : MonoBehaviour, ISteamLocomotiveSubcomponent, IDynamicChuffDelegate
{
	public VisualEffect visualEffect;

	public ChuffProfile profile;

	public float tractiveEffort;

	public float absVelocity;

	public bool continuous;

	public bool isStopped;

	private float _targetTractiveEffort;

	private Coroutine _puffCoroutine;

	private SmokeEffectWrapper _smoke;

	private BaseLocomotive _locomotive;

	private static bool ParticlesEnabled => Preferences.GraphicsParticleLevel > Preferences.ParticleLevel.Off;

	private void OnEnable()
	{
		_smoke = new SmokeEffectWrapper(visualEffect);
		_locomotive = GetComponentInParent<BaseLocomotive>();
		_locomotive.OnIdleDidChange += UpdatePlayStop;
		_locomotive.OnHasFuelDidChange += UpdatePlayStop;
		UpdatePlayStop();
		_smoke.Rate = 0f;
	}

	private void OnDisable()
	{
		_locomotive.OnIdleDidChange -= UpdatePlayStop;
		_locomotive.OnHasFuelDidChange -= UpdatePlayStop;
	}

	private void Update()
	{
		int num = ((tractiveEffort < _targetTractiveEffort) ? 2 : 4);
		tractiveEffort = Mathf.Lerp(tractiveEffort, _targetTractiveEffort, Time.deltaTime * (float)num);
		if (isStopped || continuous)
		{
			UpdateSmoke();
		}
	}

	private void UpdateSmoke()
	{
		_smoke.Rate = profile.effortToRate.Evaluate(tractiveEffort);
		_smoke.Velocity = profile.effortToVelocity.Evaluate(tractiveEffort);
		_smoke.Color = SmokeStartColor();
		_smoke.Lifetime = profile.effortToLifetime.Evaluate(tractiveEffort);
		_smoke.Size0 = profile.effortToSize.Evaluate(tractiveEffort);
		_smoke.Size1 = profile.effortToSize.Evaluate(tractiveEffort) * 10f;
		_smoke.TurbulenceIntensity = ((tractiveEffort > 0.01f) ? 100 : 10);
	}

	private void UpdatePlayStop()
	{
		bool isIdle = _locomotive.IsIdle;
		if (ParticlesEnabled && !isIdle && _locomotive.HasFuel)
		{
			visualEffect.Play();
		}
		else
		{
			visualEffect.Stop();
		}
	}

	private IEnumerator Puff(float seconds, float chuffDuration)
	{
		chuffDuration *= 0.6f;
		if (seconds > 0.001f)
		{
			yield return new WaitForSeconds(seconds);
		}
		if (!(tractiveEffort < 0.01f))
		{
			UpdateSmoke();
			double t0 = Time.realtimeSinceStartupAsDouble;
			while (Time.realtimeSinceStartupAsDouble - t0 < (double)chuffDuration)
			{
				yield return null;
				float t1 = Time.deltaTime * 4f;
				_smoke.Velocity = Mathf.Lerp(_smoke.Velocity, 0f, t1);
				_smoke.Rate = Mathf.Lerp(_smoke.Rate, 0f, t1);
			}
			_smoke.Rate = 0f;
			_puffCoroutine = null;
		}
	}

	private Color SmokeStartColor()
	{
		float white = Mathf.Lerp(1f, 0.25f, tractiveEffort);
		float alpha = profile.effortToAlpha.Evaluate(tractiveEffort);
		return WhiteWithAlpha(white, alpha);
	}

	private static Color WhiteWithAlpha(float white, float alpha)
	{
		return new Color(white, white, white, alpha);
	}

	public void ScheduleNextChuff(float delay, float chuffDuration)
	{
		if (!continuous && ParticlesEnabled)
		{
			if (_puffCoroutine != null)
			{
				StopCoroutine(_puffCoroutine);
			}
			_puffCoroutine = StartCoroutine(Puff(delay, chuffDuration));
		}
	}

	public void ApplyDistanceMoved(MovementInfo info, float driverVelocity, float absReverser, float absThrottle, float driverPhase)
	{
		absVelocity = Mathf.Abs(driverVelocity);
		isStopped = absVelocity < 0.01f;
		continuous = absVelocity > 5f;
		_targetTractiveEffort = ((isStopped || !_locomotive.HasFuel) ? 0f : absThrottle);
	}
}
