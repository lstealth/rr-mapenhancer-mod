using System;
using UnityEngine;

namespace Audio;

public class ParametricAudioPlayerOneShot : MonoBehaviour
{
	public ParametricAudioComposition composition;

	[Range(0f, 1f)]
	public float volume = 1f;

	[Range(0.25f, 1.5f)]
	public float pitchMultiplier = 1f;

	private IAudioSource[] _sources;

	[NonSerialized]
	public string AudioSourceName = "ParametricOneShot";

	public AudioController.Group outputAudioMixerGroup { get; set; }

	public int priority { get; set; }

	private void OnDestroy()
	{
		CleanUpSources();
	}

	public void Play(float parameter)
	{
		PlayScheduled(parameter, AudioSettings.dspTime);
	}

	public void PlayScheduled(float parameter, double time)
	{
		if (composition == null)
		{
			return;
		}
		ParametricAudioComposition.Track[] tracks = composition.tracks;
		if (_sources == null || tracks.Length != _sources.Length)
		{
			CleanUpSources();
			_sources = new IAudioSource[tracks.Length];
			for (int i = 0; i < tracks.Length; i++)
			{
				IAudioSource audioSource = VirtualAudioSourcePool.Checkout(AudioSourceName, tracks[i].clip, loop: false, outputAudioMixerGroup, priority, base.transform, AudioDistance.Distant);
				_sources[i] = audioSource;
			}
		}
		parameter = Mathf.Clamp01(parameter);
		volume = Mathf.Clamp01(volume);
		for (int j = 0; j < tracks.Length; j++)
		{
			ParametricAudioComposition.Track track = tracks[j];
			IAudioSource obj = _sources[j];
			obj.Stop();
			obj.volume = volume * track.volumeCurve.Evaluate(parameter);
			obj.pitch = pitchMultiplier * track.pitchCurve.Evaluate(parameter);
			obj.PlayScheduled(time);
		}
	}

	private void CleanUpSources()
	{
		if (_sources != null)
		{
			IAudioSource[] sources = _sources;
			for (int i = 0; i < sources.Length; i++)
			{
				VirtualAudioSourcePool.Return(sources[i]);
			}
		}
	}
}
