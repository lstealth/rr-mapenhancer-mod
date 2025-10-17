using Helpers;
using UnityEngine;

namespace Audio;

public class WhistlePlayer : MonoBehaviour
{
	public WhistleProfile profile;

	[Range(0f, 1f)]
	public float parameter;

	private float _parameterSmooth;

	private float _parameter;

	private float _airSpeed;

	private IAudioSource _audioSource;

	private AudioClip _ownedClip;

	private float rampUpPitch => profile.rampUpPitch;

	private float lerpSpeed => profile.lerpSpeed;

	private float airLerpSpeed => profile.airLerpSpeed;

	private AnimationCurve parameterToPitch => profile.parameterToPitch;

	private AnimationCurve parameterToVolume => profile.parameterToVolume;

	private void OnEnable()
	{
		if (_audioSource == null)
		{
			Configure(profile.audioClip);
		}
	}

	private void OnDisable()
	{
		VirtualAudioSourcePool.Return(_audioSource);
		DestroyOwnedClip();
	}

	private void Update()
	{
		if (_audioSource != null)
		{
			float deltaTime = Time.deltaTime;
			_parameter = parameter;
			_parameterSmooth = Mathf.Lerp(_parameterSmooth, _parameter, lerpSpeed * deltaTime);
			float parameterSmooth = _parameterSmooth;
			float num = airLerpSpeed * ((parameterSmooth > _airSpeed) ? 1f : 0.4f);
			_airSpeed = Mathf.Lerp(_airSpeed, parameterSmooth, deltaTime * num);
			float num2 = Mathf.Lerp(rampUpPitch, 1f, _airSpeed);
			_audioSource.pitch = num2 * parameterToPitch.Evaluate(_airSpeed);
			float b = parameterToVolume.Evaluate(_airSpeed);
			float volume = Mathf.Lerp(_audioSource.volume, b, deltaTime * 100f);
			_audioSource.volume = volume;
		}
	}

	public void Configure(AudioClip audioClip)
	{
		VirtualAudioSourcePool.Return(_audioSource);
		DestroyOwnedClip();
		_audioSource = VirtualAudioSourcePool.Checkout("Whistle", _ownedClip = audioClip.Loopify(), loop: true, AudioController.Group.LocomotiveWhistle, 10, base.transform, AudioDistance.Distant);
		_audioSource.volume = 0f;
		_audioSource.minDistance = 50f;
		_audioSource.maxDistance = 1000f;
		_audioSource.Play();
	}

	private void DestroyOwnedClip()
	{
		if (_ownedClip != null)
		{
			Object.Destroy(_ownedClip);
		}
		_ownedClip = null;
	}
}
