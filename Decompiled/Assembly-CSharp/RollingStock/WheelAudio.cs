using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Audio;
using Model;
using UnityEngine;

namespace RollingStock;

public class WheelAudio : MonoBehaviour
{
	private WheelClackProfile _profile;

	private IAudioSource[] _audioSourceClack;

	private float _lastClackNoise;

	private float[] _clackOffsets;

	private float _rollClackDistance;

	private float _clackOdometer;

	private float _absVelocity;

	private Coroutine _rollClackCoroutine;

	private bool _carIsNearby;

	private Car _car;

	private const float MinRollClackVelocity = 0.01f;

	public float LinearOffset { get; set; }

	private bool ShouldRunLoop
	{
		get
		{
			if (_carIsNearby)
			{
				return _absVelocity > 0.01f;
			}
			return false;
		}
	}

	private void OnDisable()
	{
		if (_rollClackCoroutine != null)
		{
			StopCoroutine(_rollClackCoroutine);
		}
		_rollClackCoroutine = null;
	}

	private void OnDestroy()
	{
		if (_car != null)
		{
			_car.OnIsNearbyDidChange -= CarIsNearbyDidChange;
		}
		DestroyAudioSources();
	}

	public void Configure(WheelClackProfile profile, List<Transform> wheels, Car car)
	{
		int numWheels = ((!wheels[0].name.EndsWith("_LOD0")) ? wheels.Count : wheels.Count((Transform w) => w.name.EndsWith("_LOD0")));
		float wheelSeparation = Vector3.Distance(wheels[0].position, wheels[wheels.Count - 1].position);
		Configure(profile, numWheels, wheelSeparation, car);
	}

	private void Configure(WheelClackProfile profile, int numWheels, float wheelSeparation, Car car)
	{
		float[] array = new float[numWheels];
		float num = (0f - wheelSeparation * (float)(numWheels - 1)) / 2f;
		for (int i = 0; i < numWheels; i++)
		{
			array[i] = num;
			num += wheelSeparation;
		}
		Configure(profile, array, car);
	}

	public void Configure(WheelClackProfile profile, float[] clackOffsets, Car car)
	{
		_profile = profile;
		_clackOffsets = clackOffsets;
		_car = car;
		car.OnIsNearbyDidChange += CarIsNearbyDidChange;
		CarIsNearbyDidChange(car.IsNearby);
	}

	private void CarIsNearbyDidChange(bool isNearby)
	{
		_carIsNearby = isNearby;
	}

	public void Roll(float distance, float velocity)
	{
		_absVelocity = Mathf.Abs(velocity);
		_rollClackDistance += distance;
		if (ShouldRunLoop && _rollClackCoroutine == null && _clackOffsets.Length >= 2)
		{
			_rollClackCoroutine = StartCoroutine(RollClackCoroutine());
		}
	}

	private static float Repeat(float value, float length)
	{
		if (value < 0f)
		{
			return length - Mathf.Repeat(0f - value, length);
		}
		return Mathf.Repeat(value, length);
	}

	private void RollClack(float distance)
	{
		float jointDistance = _profile.jointDistance;
		float num = jointDistance * 0.1f;
		float num2 = jointDistance * 0.9f;
		_clackOdometer = Repeat(_clackOdometer, jointDistance * 10f);
		float clackOdometer = _clackOdometer;
		_clackOdometer -= distance;
		if (_audioSourceClack == null)
		{
			return;
		}
		int num3 = _clackOffsets.Length;
		float time = _absVelocity * 2.23694f;
		float linearOffset = LinearOffset;
		for (int i = 0; i < num3; i++)
		{
			float num4 = _clackOffsets[i] + linearOffset;
			float num5 = Repeat(clackOdometer + num4, jointDistance);
			float num6 = Repeat(_clackOdometer + num4, jointDistance);
			if ((!(distance <= 0f)) ? (num5 < num && num6 > num2) : (num5 > num2 && num6 < num))
			{
				IAudioSource obj = _audioSourceClack[i];
				obj.Stop();
				obj.time = 0f;
				float num7 = Random.Range(_profile.lowPassNoiseMagnitude, 0f);
				obj.SetLowPassCutoff(_profile.velocityMphToLowPassCutoff.Evaluate(time) + num7);
				obj.Play();
			}
		}
		float num8 = _profile.velocityMphToDuration.Evaluate(time);
		IAudioSource[] audioSourceClack = _audioSourceClack;
		foreach (IAudioSource audioSource in audioSourceClack)
		{
			if (audioSource.isPlaying)
			{
				float time2 = audioSource.time;
				audioSource.volume = _profile.velocityMphToVolume.Evaluate(time) * _profile.volumeEnvelope.Evaluate(time2 * num8);
			}
		}
	}

	private IEnumerator RollClackCoroutine()
	{
		CreateAudioSourcesIfNeeded();
		while (ShouldRunLoop)
		{
			RollClack(_rollClackDistance);
			_rollClackDistance = 0f;
			yield return null;
		}
		DestroyAudioSources();
		_rollClackCoroutine = null;
	}

	private void CreateAudioSourcesIfNeeded()
	{
		if (_audioSourceClack == null)
		{
			_audioSourceClack = new IAudioSource[_clackOffsets.Length];
			for (int i = 0; i < _audioSourceClack.Length; i++)
			{
				float num = _clackOffsets[i];
				IAudioSource audioSource = VirtualAudioSourcePool.Checkout("Clack", _profile.wheelClackClip, loop: false, AudioController.Group.WheelsClack, 20, base.transform, AudioDistance.Local, num * Vector3.back);
				audioSource.minDistance = _profile.rolloffMinDistance;
				audioSource.maxDistance = _profile.rolloffMaxDistance;
				audioSource.dopplerLevel = 0.1f;
				_audioSourceClack[i] = audioSource;
			}
		}
	}

	private void DestroyAudioSources()
	{
		if (_audioSourceClack != null)
		{
			IAudioSource[] audioSourceClack = _audioSourceClack;
			for (int i = 0; i < audioSourceClack.Length; i++)
			{
				VirtualAudioSourcePool.Return(audioSourceClack[i]);
			}
		}
		_audioSourceClack = null;
	}
}
