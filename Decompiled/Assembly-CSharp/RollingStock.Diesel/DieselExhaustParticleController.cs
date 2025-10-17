using Game;
using Model;
using RollingStock.Steam;
using UnityEngine;
using UnityEngine.VFX;

namespace RollingStock.Diesel;

public class DieselExhaustParticleController : MonoBehaviour
{
	public VisualEffect visualEffect;

	public SmokeEffectProfile profile;

	public float accelInfluenceLow;

	public float accelInfluenceHigh = 0.1f;

	private float _value;

	private float _accel;

	private SmokeEffectWrapper _smoke;

	private BaseLocomotive _locomotive;

	private static bool ParticlesEnabled => Preferences.GraphicsParticleLevel > Preferences.ParticleLevel.Off;

	public float NormalizedExhaustOutput { get; set; }

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
		float deltaTime = Time.deltaTime;
		if (deltaTime != 0f)
		{
			float value = _value;
			_value = Mathf.Lerp(_value, NormalizedExhaustOutput, deltaTime * 0.2f);
			_accel = (_value - value) / deltaTime;
			UpdateSmoke();
		}
	}

	private void UpdateSmoke()
	{
		_smoke.Rate = profile.rateCurve.Evaluate(_value);
		_smoke.Velocity = profile.velocityCurve.Evaluate(_value);
		_smoke.Color = SmokeStartColor();
		_smoke.Lifetime = profile.lifetimeCurve.Evaluate(_value);
		_smoke.Size0 = profile.sizeCurve.Evaluate(_value);
		_smoke.Size1 = profile.sizeCurve.Evaluate(_value) * 10f;
		_smoke.TurbulenceIntensity = profile.turbulenceCurve.Evaluate(_value);
	}

	private Color SmokeStartColor()
	{
		float num = Mathf.InverseLerp(accelInfluenceLow, accelInfluenceHigh, _accel);
		return profile.colorGradient.Evaluate(_value + num);
	}

	private void UpdatePlayStop()
	{
		bool flag = !_locomotive.IsIdle && _locomotive.HasFuel;
		if (ParticlesEnabled && flag)
		{
			visualEffect.Play();
		}
		else
		{
			visualEffect.Stop();
		}
	}
}
