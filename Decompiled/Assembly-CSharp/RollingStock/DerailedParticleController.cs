using System.Collections;
using Game;
using RollingStock.Diesel;
using RollingStock.Steam;
using UnityEngine;
using UnityEngine.VFX;

namespace RollingStock;

public class DerailedParticleController : MonoBehaviour, ICarMovementListener
{
	[SerializeField]
	private VisualEffect visualEffect;

	[SerializeField]
	private SmokeEffectProfile profile;

	private float _value;

	private bool _playing;

	private SmokeEffectWrapper _smoke;

	private float _derailmentTarget;

	private Coroutine _coroutine;

	private const float MinDerailValue = 0.01f;

	private const float MinVelocity = 0.01f;

	private static bool ParticlesEnabled => Preferences.GraphicsParticleLevel > Preferences.ParticleLevel.Off;

	public float Derailment
	{
		get
		{
			return _derailmentTarget;
		}
		set
		{
			if (!(Mathf.Abs(value - _derailmentTarget) < 0.01f))
			{
				_derailmentTarget = value;
				StartUpdateCoroutineIfNeeded();
			}
		}
	}

	private float CarVelocity { get; set; }

	private void OnEnable()
	{
		_smoke = new SmokeEffectWrapper(visualEffect);
		_smoke.Rate = 0f;
		visualEffect.Stop();
		visualEffect.gameObject.SetActive(value: false);
		_playing = false;
	}

	private void OnDisable()
	{
		if (_coroutine != null)
		{
			StopCoroutine(_coroutine);
		}
		_coroutine = null;
	}

	private IEnumerator UpdateCoroutine()
	{
		visualEffect.gameObject.SetActive(value: true);
		while (Mathf.Abs(_derailmentTarget - _value) > 0.001f || (_value > 0.01f && CarVelocity > 0.01f))
		{
			float deltaTime = Time.deltaTime;
			_value = Mathf.Lerp(_value, _derailmentTarget, deltaTime * 0.2f);
			UpdateSmoke();
			yield return null;
		}
		_coroutine = null;
	}

	private void SetPlaying(bool play)
	{
		if (play != _playing)
		{
			if (play && ParticlesEnabled)
			{
				visualEffect.Play();
			}
			else
			{
				visualEffect.Stop();
			}
			_playing = play;
		}
	}

	private void UpdateSmoke()
	{
		if (_value < 0.01f || CarVelocity < 0.01f)
		{
			_smoke.Rate = 0f;
			SetPlaying(play: false);
			return;
		}
		SetPlaying(play: true);
		_smoke.Rate = profile.rateCurve.Evaluate(CarVelocity);
		_smoke.Velocity = profile.velocityCurve.Evaluate(CarVelocity);
		_smoke.Color = profile.colorGradient.Evaluate(_value);
		_smoke.Lifetime = profile.lifetimeCurve.Evaluate(_value);
		float num = profile.sizeCurve.Evaluate(CarVelocity);
		_smoke.Size0 = num;
		_smoke.Size1 = num * 5f;
		_smoke.TurbulenceIntensity = profile.turbulenceCurve.Evaluate(_value);
	}

	public void CarDidMove(MovementInfo info)
	{
		CarVelocity = ((info.DeltaTime == 0f) ? 0f : Mathf.Abs(info.Distance / info.DeltaTime));
		if (CarVelocity > 0.01f && _value > 0.01f)
		{
			StartUpdateCoroutineIfNeeded();
		}
	}

	private void StartUpdateCoroutineIfNeeded()
	{
		if (_coroutine == null && base.isActiveAndEnabled)
		{
			_coroutine = StartCoroutine(UpdateCoroutine());
		}
	}
}
