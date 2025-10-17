using System;
using System.Collections;
using Audio;
using Game.Messages;
using KeyValue.Runtime;
using Model;
using RollingStock;
using RollingStock.Steam;
using Serilog;
using UnityEngine;
using UnityEngine.VFX;

namespace Effects;

public class CylinderCockController : MonoBehaviour, ISteamLocomotiveSubcomponent
{
	[Header("Smoke")]
	[SerializeField]
	private VisualEffect[] smokeEffects;

	[SerializeField]
	private AnimationCurve smokeOutputCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[Header("Audio")]
	[SerializeField]
	private AudioClip audioClip;

	[SerializeField]
	private AnimationCurve volumeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	[SerializeField]
	private float audioDistanceMin = 8f;

	[SerializeField]
	private float audioDistanceMax = 50f;

	private BaseLocomotive _locomotive;

	private Coroutine _coroutine;

	private bool _open;

	private IDisposable _controlObserver;

	private float _forwardOffset;

	private IAudioSource[] _audioSources;

	private float _phase;

	private float _radius;

	private float _steam = 1f;

	private float _lastOffTime;

	public void Configure(float radius, float forwardOffset)
	{
		smokeEffects[0].transform.localPosition = Vector3.left * radius;
		smokeEffects[1].transform.localPosition = Vector3.right * radius;
		_radius = radius;
		_forwardOffset = forwardOffset;
	}

	private void OnEnable()
	{
		_locomotive = GetComponentInParent<BaseLocomotive>();
		VisualEffect[] array = smokeEffects;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Stop();
		}
		_audioSources = new IAudioSource[smokeEffects.Length];
		for (int j = 0; j < _audioSources.Length; j++)
		{
			Vector3 offset = ((j == 0) ? Vector3.left : Vector3.right) * _radius;
			IAudioSource audioSource = VirtualAudioSourcePool.Checkout("CylinderCock", audioClip, loop: true, AudioController.Group.LocomotiveCylCock, 10, base.transform, AudioDistance.Nearby, offset);
			audioSource.minDistance = audioDistanceMin;
			audioSource.maxDistance = audioDistanceMax;
			audioSource.volume = 0f;
			audioSource.Stop();
			_audioSources[j] = audioSource;
		}
		KeyValueObject componentInParent = GetComponentInParent<KeyValueObject>();
		_controlObserver = componentInParent.Observe(PropertyChange.KeyForControl(PropertyChange.Control.CylinderCock), delegate(Value value)
		{
			SetCylinderCockSteam(value.BoolValue);
		});
	}

	private void OnDisable()
	{
		_controlObserver?.Dispose();
		IAudioSource[] audioSources = _audioSources;
		foreach (IAudioSource audioSource in audioSources)
		{
			if (audioSource != null)
			{
				VirtualAudioSourcePool.Return(audioSource);
			}
		}
	}

	private void SetCylinderCockSteam(bool open)
	{
		if (smokeEffects == null)
		{
			return;
		}
		if (open)
		{
			_open = true;
			float value = Time.time - _lastOffTime;
			_steam = Mathf.Clamp01(_steam + Mathf.InverseLerp(0f, 60f, value));
			if (_coroutine == null)
			{
				_coroutine = StartCoroutine(UpdateCoroutine());
			}
		}
		else
		{
			_open = false;
			_lastOffTime = Time.time;
		}
	}

	private IEnumerator UpdateCoroutine()
	{
		float openness = 0f;
		float speed = Time.deltaTime * 10f;
		Log.Debug("CylinderCock Open");
		for (int i = 0; i < smokeEffects.Length; i++)
		{
			smokeEffects[i].Play();
			_audioSources[i].time = UnityEngine.Random.Range(0f, audioClip.length / 2f);
			_audioSources[i].Play();
		}
		while (true)
		{
			if (_open)
			{
				int num = (_locomotive.HasFuel ? 1 : 0);
				openness = Mathf.Lerp(openness, num, speed);
				SetSmokeEffects(openness);
				yield return null;
				continue;
			}
			while (openness > 0.1f)
			{
				SetSmokeEffects(openness);
				openness = Mathf.Lerp(openness, 0f, speed);
				yield return null;
			}
			SetSmokeEffects(0f);
			yield return new WaitForSeconds(1f);
			if (!_open)
			{
				break;
			}
		}
		for (int j = 0; j < smokeEffects.Length; j++)
		{
			smokeEffects[j].Stop();
			_audioSources[j].Stop();
		}
		Log.Debug("CylinderCock Closed");
		_coroutine = null;
	}

	private void SetSmokeEffects(float openness)
	{
		float t = Mathf.PerlinNoise(0f, Time.time);
		openness *= Mathf.Lerp(0.5f, 1f, t);
		_steam -= openness * Time.deltaTime * 0.1f;
		_steam = Mathf.Clamp01(_steam);
		float num = Mathf.Max(0.05f, _steam);
		float num2 = openness * num;
		for (int i = 0; i < smokeEffects.Length; i++)
		{
			float num3 = Mathf.Repeat(_phase + (float)i * 0.25f, 1f);
			float time = Mathf.Repeat(2f * (_phase + (float)i * 0.25f), 1f);
			float num4 = num2 * smokeOutputCurve.Evaluate(time);
			VisualEffect effect = smokeEffects[i];
			SmokeEffectWrapper smokeEffectWrapper = new SmokeEffectWrapper(effect)
			{
				Velocity = Mathf.Lerp(3f, 6f, num4),
				Rate = ((num4 > 0.01f) ? Mathf.Lerp(10f, 20f, num4) : 0f),
				Size0 = Mathf.Lerp(0.1f, 0.25f, num2),
				Size1 = Mathf.Lerp(1f, 2f, num2)
			};
			float num5 = ((num3 > 0.5f) ? _forwardOffset : 0f);
			smokeEffectWrapper.PositionOffset = Vector3.back * num5;
			_audioSources[i].volume = num2 * volumeCurve.Evaluate(time);
		}
	}

	public void ApplyDistanceMoved(MovementInfo info, float driverVelocity, float absReverser, float absThrottle, float driverPhase)
	{
		_phase = driverPhase + 0.25f;
		_steam = Mathf.Clamp01(_steam + absThrottle * Time.fixedDeltaTime * 0.001f);
	}
}
