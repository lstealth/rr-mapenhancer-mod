using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using UnityEngine;

namespace Audio;

public class IntegerLoopingPlayer : MonoBehaviour
{
	public IndexedClipDescriptor indexedClip;

	public bool play;

	[Range(0f, 1f)]
	public float volume = 1f;

	public AudioController.Group mixerGroup = AudioController.Group.Locomotive;

	public int priority = 10;

	public AudioDistance audioDistance = AudioDistance.Distant;

	[Tooltip("True if the next indexed clip should be chosen randomly.")]
	public bool randomize;

	[NonSerialized]
	public float averageClipLength;

	[NonSerialized]
	public string AudioSourceName = "IntegerLooping";

	private readonly List<IAudioSource> _sources = new List<IAudioSource>();

	private int _currentSourceIndex;

	private double _nextEndTime;

	private AudioClip[] _clips;

	private const int CrossSamples = 20;

	private int _innerIndex = 1;

	private Coroutine _coroutine;

	private AudioClip[] _ownedClips = Array.Empty<AudioClip>();

	public Action<IAudioSource> ConfigureSource { get; set; }

	private IAudioSource SourceA => _sources[_currentSourceIndex];

	private IAudioSource SourceB => _sources[1 - _currentSourceIndex];

	private void OnEnable()
	{
		PrepareSources();
		PrepareClips();
		_coroutine = StartCoroutine(Loop());
	}

	private void OnDisable()
	{
		if (_coroutine != null)
		{
			StopCoroutine(_coroutine);
		}
		_coroutine = null;
		ClearSources();
		DestroyOwnedClips();
	}

	private void Mark(string tag)
	{
	}

	private IEnumerator Loop()
	{
		while (_sources == null || _sources.Count < 2)
		{
			yield return null;
		}
		while (true)
		{
			Mark("Wait for play");
			while (!play)
			{
				yield return null;
			}
			Mark("Start Playing");
			StartPlaying();
			while (play)
			{
				if (!SourceA.isPlaying)
				{
					Mark("Swap");
					SwapAB();
					PlayNext(_clips[_innerIndex]);
					NextInnerIndex();
				}
				yield return null;
			}
			Mark("Stopping");
			SourceB.Stop();
			_nextEndTime -= (double)SourceB.clip.samples / (double)SourceB.clip.frequency;
			PlayNext(_clips.Last());
			Mark("Wait for Stop");
			while (!play && (SourceA.isPlaying || SourceB.isPlaying))
			{
				yield return null;
			}
			Mark("Stop");
			SourceA.Stop();
			SourceB.Stop();
		}
	}

	private void StartPlaying()
	{
		PlayNow(_clips[0]);
		_innerIndex = 1;
		PlayNext(_clips[1]);
		NextInnerIndex();
	}

	private void SwapAB()
	{
		_currentSourceIndex = 1 - _currentSourceIndex;
	}

	private void NextInnerIndex()
	{
		if (_clips.Length > 2)
		{
			if (randomize)
			{
				_innerIndex = UnityEngine.Random.Range(1, _clips.Length - 1);
			}
			else
			{
				_innerIndex = _innerIndex % (_clips.Length - 2) + 1;
			}
		}
	}

	private void PlayNow(AudioClip nowClip)
	{
		SourceA.Stop();
		SourceB.Stop();
		double dspTime = AudioSettings.dspTime;
		IAudioSource sourceA = SourceA;
		sourceA.clip = nowClip;
		sourceA.time = 0f;
		sourceA.volume = volume;
		sourceA.PlayScheduled(dspTime);
		_nextEndTime = dspTime + (double)nowClip.samples / (double)nowClip.frequency;
	}

	private void PlayNext(AudioClip nextClip)
	{
		IAudioSource sourceB = SourceB;
		sourceB.Stop();
		sourceB.clip = nextClip;
		sourceB.time = 0f;
		sourceB.volume = volume;
		sourceB.PlayScheduled(_nextEndTime);
		_nextEndTime += (double)nextClip.samples / (double)nextClip.frequency;
	}

	public void PrepareSources()
	{
		ClearSources();
		for (int i = 0; i < 2; i++)
		{
			IAudioSource audioSource = VirtualAudioSourcePool.Checkout(AudioSourceName, null, loop: false, mixerGroup, priority, base.transform, audioDistance);
			audioSource.dopplerLevel = 0.25f;
			audioSource.minDistance = 5f;
			audioSource.maxDistance = 200f;
			audioSource.volume = 0f;
			ConfigureSource?.Invoke(audioSource);
			_sources.Add(audioSource);
		}
	}

	private void ClearSources()
	{
		foreach (IAudioSource source in _sources)
		{
			VirtualAudioSourcePool.Return(source);
		}
		_sources.Clear();
	}

	private void DestroyOwnedClips()
	{
		AudioClip[] ownedClips = _ownedClips;
		for (int i = 0; i < ownedClips.Length; i++)
		{
			UnityEngine.Object.Destroy(ownedClips[i]);
		}
		_ownedClips = Array.Empty<AudioClip>();
	}

	private void PrepareClips()
	{
		if (!indexedClip)
		{
			return;
		}
		DestroyOwnedClips();
		if (indexedClip.indexes.Length != 0)
		{
			_clips = indexedClip.clip.Split(indexedClip.indexes.Select((IndexedClipDescriptor.Index i) => i.time).ToArray(), 20);
			_ownedClips = _clips.ToArray();
		}
		else
		{
			_clips = new AudioClip[2] { indexedClip.clip, indexedClip.clip };
		}
		averageClipLength = _clips.Select((AudioClip c) => c.length).Average();
	}
}
