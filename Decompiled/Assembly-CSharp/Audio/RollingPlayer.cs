using System;
using System.Collections;
using Model;
using Model.Physics;
using UnityEngine;

namespace Audio;

public class RollingPlayer : MonoBehaviour
{
	private class AudioSourcePair : IDisposable
	{
		public IAudioSource Current { get; private set; }

		public IAudioSource Next { get; private set; }

		public AudioSourcePair(Func<IAudioSource> createSource)
		{
			Current = createSource();
			Next = createSource();
		}

		public void Dispose()
		{
			VirtualAudioSourcePool.Return(Current);
			VirtualAudioSourcePool.Return(Next);
			Current = null;
			Next = null;
		}

		public void Swap()
		{
			IAudioSource next = Next;
			IAudioSource current = Current;
			IAudioSource audioSource = (Current = next);
			audioSource = (Next = current);
		}
	}

	public RollingProfile profile;

	public float? overrideVelocity;

	private float _absVelocity;

	private AudioSourcePair _rollSources;

	private AudioSourcePair _squealSources;

	private Car _car;

	public bool debugGraph;

	private bool _debugGraphSetup;

	private Coroutine _coroutineRolling;

	private Coroutine _coroutineSqueal;

	private float _movingThreshold = 0.1f;

	private Car Car
	{
		get
		{
			if (_car == null)
			{
				_car = GetComponentInParent<Car>();
			}
			return _car;
		}
	}

	private bool IsMoving => _absVelocity > _movingThreshold;

	private void OnEnable()
	{
		_rollSources = new AudioSourcePair(CreateRollingAudioSource);
		_squealSources = new AudioSourcePair(CreateSquealAudioSource);
		for (float num = 1f; num > 0f; num -= 0.01f)
		{
			if (profile.mphToVolume.Evaluate(num) < 0.01f)
			{
				_movingThreshold = num * 0.44703928f;
				break;
			}
		}
	}

	private void OnDisable()
	{
		_rollSources.Dispose();
		_squealSources.Dispose();
		if (_coroutineRolling != null)
		{
			StopCoroutine(_coroutineRolling);
		}
		if (_coroutineSqueal != null)
		{
			StopCoroutine(_coroutineSqueal);
		}
		_coroutineRolling = null;
		_coroutineSqueal = null;
	}

	public void SetVelocity(float velocity)
	{
		_absVelocity = Mathf.Abs(velocity);
		bool isMoving = IsMoving;
		if (isMoving && _coroutineRolling == null)
		{
			_coroutineRolling = StartCoroutine(RunRolling());
		}
		if (isMoving && _coroutineSqueal == null)
		{
			_coroutineSqueal = StartCoroutine(RunSqueal());
		}
	}

	private float GetVelocity()
	{
		if (overrideVelocity.HasValue)
		{
			return overrideVelocity.Value;
		}
		return _absVelocity;
	}

	private IEnumerator WaitForReady()
	{
		while (profile == null || profile.tracks.Length == 0)
		{
			yield return null;
		}
	}

	private IEnumerator RunRolling()
	{
		yield return WaitForReady();
		AudioSourcePair sources = _rollSources;
		while (IsMoving)
		{
			float num = Mathf.Abs(GetVelocity());
			int num2 = FindCurrentIndex(num, profile.tracks);
			ParametricAudioComposition.Track track = profile.tracks[num2];
			if (sources.Current.clip != track.clip)
			{
				sources.Next.Stop();
				sources.Next.clip = track.clip;
				sources.Next.time = UnityEngine.Random.Range(0f, track.clip.length * 0.75f);
				sources.Next.volume = 0f;
				sources.Next.pitch = track.pitchCurve.Evaluate(num);
				sources.Next.Play();
				yield return CrossfadeHelper.Crossfade(sources.Current, sources.Next, 1f, GetVolumeMultiplier());
				sources.Swap();
			}
			else
			{
				if (!sources.Current.isPlaying)
				{
					sources.Current.Play();
				}
				sources.Current.pitch = track.pitchCurve.Evaluate(num);
				float volumeMultiplier = GetVolumeMultiplier();
				sources.Current.volume = volumeMultiplier;
				yield return null;
			}
		}
		sources.Current.Stop();
		sources.Next.Stop();
		_coroutineRolling = null;
	}

	private IEnumerator RunSqueal()
	{
		Car car = Car;
		AudioSourcePair sources = _squealSources;
		while (IsMoving)
		{
			if (debugGraph && !_debugGraphSetup)
			{
				_debugGraphSetup = true;
				DebugGUI.SetGraphProperties("perlin", "Perlin", 0f, 1f, 0, Color.cyan, autoScale: false);
				DebugGUI.SetGraphProperties("cc", "Curve Comp", 0f, 1f, 0, Color.yellow, autoScale: false);
				DebugGUI.SetGraphProperties("cu", "Curvature", 0f, 30f, 1, Color.magenta, autoScale: false);
				DebugGUI.SetGraphProperties("mcu", "Curvature", 0f, 30f, 1, Color.green, autoScale: false);
			}
			while (car.CurrentTrackCurvature < 2.2f || GetVelocity() < 0.011f)
			{
				yield return new WaitForSeconds(0.1f);
			}
			int num = UnityEngine.Random.Range(0, profile.squeals.Length);
			ParametricAudioComposition.Track track = profile.squeals[num];
			float random = UnityEngine.Random.Range(0f, 1f);
			IAudioSource next = sources.Next;
			next.Stop();
			next.clip = track.clip;
			next.time = UnityEngine.Random.Range(0f, track.clip.length * 0.75f);
			next.volume = 0f;
			next.loop = true;
			next.Play();
			sources.Swap();
			while (car.CurrentTrackCurvature > 2f && GetVelocity() > 0.01f)
			{
				float maximumTrackCurvature = car.MaximumTrackCurvature;
				float velocity = GetVelocity();
				float maxVelocityForCurve = TrainMath.MaximumSpeedMphForCurve(car.CurrentTrackCurvature, car.MaximumTrackCurvature) * 0.44703928f;
				float num2 = VolumeForSqueal(car.CurrentTrackCurvature, maximumTrackCurvature, velocity, maxVelocityForCurve, random);
				num2 *= GetVolumeMultiplier();
				sources.Current.volume = Mathf.MoveTowards(sources.Current.volume, num2, 0.1f);
				yield return null;
			}
			yield return sources.Current.FadeOutStop(0.5f);
		}
		_coroutineSqueal = null;
	}

	private float VolumeForSqueal(float curvature, float maxCurvature, float absVelocity, float maxVelocityForCurve, float squeal)
	{
		if (absVelocity > maxVelocityForCurve)
		{
			return 1f;
		}
		float distance = Car.WheelBoundsA.distance;
		float num = 1f - Mathf.PerlinNoise(distance * 0.01f, squeal) * (squeal * squeal);
		float num2 = Mathf.InverseLerp(Mathf.Min(maxCurvature, num * 12f), maxCurvature, curvature);
		if (debugGraph)
		{
			DebugGUI.Graph("perlin", num);
			DebugGUI.Graph("mcu", maxCurvature);
			DebugGUI.Graph("cu", curvature);
			DebugGUI.Graph("cc", num2);
		}
		return Mathf.InverseLerp(0.1f, 8f, absVelocity) * num2;
	}

	private float GetVolumeMultiplier()
	{
		return profile.mphToVolume.Evaluate(GetVelocity() * 2.23694f);
	}

	private int FindCurrentIndex(float velocity, ParametricAudioComposition.Track[] tracks)
	{
		int result = 0;
		float num = 0f;
		for (int i = 0; i < tracks.Length; i++)
		{
			float num2 = tracks[i].volumeCurve.Evaluate(velocity);
			if (num2 > num)
			{
				num = num2;
				result = i;
			}
		}
		return result;
	}

	private IAudioSource CreateRollingAudioSource()
	{
		IAudioSource audioSource = VirtualAudioSourcePool.Checkout("Rolling", null, loop: true, AudioController.Group.WheelsRoll, 30, base.transform, AudioDistance.Nearby);
		audioSource.rolloffMode = AudioRolloffMode.Linear;
		audioSource.minDistance = Car.carLength * 0.6f;
		audioSource.maxDistance = 50f;
		audioSource.volume = 0f;
		return audioSource;
	}

	private IAudioSource CreateSquealAudioSource()
	{
		IAudioSource audioSource = VirtualAudioSourcePool.Checkout("Squeal", null, loop: true, AudioController.Group.WheelsSqueal, 30, base.transform, AudioDistance.Nearby);
		audioSource.rolloffMode = AudioRolloffMode.Linear;
		audioSource.minDistance = Car.carLength * 0.6f;
		audioSource.maxDistance = 100f;
		audioSource.volume = 0f;
		return audioSource;
	}
}
