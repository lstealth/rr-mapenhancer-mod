using Game;
using Model;
using RollingStock.Steam;
using UnityEngine;
using UnityEngine.VFX;

namespace Audio;

public class DynamoPlayer : MonoBehaviour
{
	public AudioClip clip;

	public VisualEffect visualEffect;

	private IAudioSource _source;

	private BaseLocomotive _locomotive;

	private static bool ParticlesEnabled => Preferences.GraphicsParticleLevel > Preferences.ParticleLevel.Off;

	private void OnEnable()
	{
		_source = VirtualAudioSourcePool.Checkout("Dynamo", clip, loop: true, AudioController.Group.LocomotiveDynamo, 11, base.transform, AudioDistance.Local);
		_source.minDistance = 3f;
		_source.pitch = Random.Range(0.99f, 1f);
		_source.volume = 1f;
		SmokeEffectWrapper smokeEffectWrapper = new SmokeEffectWrapper(visualEffect);
		smokeEffectWrapper.Rate = 100f;
		smokeEffectWrapper.Size0 = 0.05f;
		smokeEffectWrapper.Size1 = 0.3f;
		smokeEffectWrapper.TurbulenceIntensity = 50f;
		smokeEffectWrapper.Lifetime = 0.5f;
		smokeEffectWrapper.Velocity = 5f;
		_locomotive = GetComponentInParent<BaseLocomotive>();
		_locomotive.OnIdleDidChange += UpdatePlayStop;
		_locomotive.OnHasFuelDidChange += UpdatePlayStop;
		UpdatePlayStop();
	}

	private void OnDisable()
	{
		_locomotive.OnIdleDidChange -= UpdatePlayStop;
		_locomotive.OnHasFuelDidChange -= UpdatePlayStop;
		if (_source != null)
		{
			VirtualAudioSourcePool.Return(_source);
			_source = null;
		}
	}

	private void UpdatePlayStop()
	{
		if (!_locomotive.IsIdle && _locomotive.HasFuel)
		{
			if (ParticlesEnabled)
			{
				visualEffect.Play();
			}
			if (!_source.isPlaying)
			{
				StartCoroutine(_source.FadeIn(3f, 0f, 1f, 0.8f, 1f));
			}
		}
		else
		{
			visualEffect.Stop();
			if (_source.isPlaying)
			{
				StartCoroutine(_source.FadeOutStop(3f, 0.7f));
			}
		}
	}
}
