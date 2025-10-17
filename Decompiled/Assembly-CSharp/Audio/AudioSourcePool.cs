using System.Collections.Generic;
using UnityEngine;

namespace Audio;

public static class AudioSourcePool
{
	private static readonly Stack<AudioSource> Pool = new Stack<AudioSource>();

	private const int PoolSize = 64;

	private static readonly HashSet<AudioSource> All = new HashSet<AudioSource>();

	private static Transform _root;

	private static int _audioSourceCount = 0;

	private static Transform Root
	{
		get
		{
			if (_root != null)
			{
				return _root;
			}
			GameObject obj = new GameObject
			{
				hideFlags = HideFlags.DontSave,
				name = "AudioSourcePool"
			};
			Object.DontDestroyOnLoad(obj);
			_root = obj.transform;
			return _root;
		}
	}

	public static AudioSource Checkout(AudioClip clip, bool loop, AudioController.Group mixerGroup, int priority, Transform parent, Vector3 parentOffset)
	{
		AudioSource audioSource = AudioSourceForCheckout();
		audioSource.ApplyBase3DSettings();
		audioSource.outputAudioMixerGroup = mixerGroup;
		audioSource.priority = priority;
		audioSource.loop = loop;
		audioSource.pitch = 1f;
		audioSource.timeSamples = 0;
		audioSource.clip = clip;
		audioSource.volume = 1f;
		audioSource.dopplerLevel = 1f;
		audioSource.enabled = true;
		Transform transform = audioSource.transform;
		transform.SetParent(parent, worldPositionStays: false);
		transform.localPosition = parentOffset;
		return audioSource;
	}

	private static AudioSource AudioSourceForCheckout()
	{
		AudioSource audioSource = null;
		while (audioSource == null)
		{
			if (Pool.Count > 0)
			{
				audioSource = Pool.Pop();
				if (audioSource == null)
				{
					Debug.LogWarning("AudioSourcePool: Pop returned null source -- probably failed to de-parent while returning in OnDisable");
				}
			}
			else
			{
				audioSource = CreateAudioSource();
			}
		}
		return audioSource;
	}

	public static void Return(AudioSource audioSource)
	{
		if (!(audioSource == null))
		{
			audioSource.Stop();
			audioSource.clip = null;
			audioSource.name = "AudioSourcePooled";
			audioSource.GetComponent<AudioHighPassFilter>().enabled = false;
			audioSource.GetComponent<AudioLowPassFilter>().enabled = false;
			if (Pool.Count >= 64)
			{
				Object.Destroy(audioSource.gameObject);
				All.Remove(audioSource);
			}
			else
			{
				audioSource.transform.SetParent(Root, worldPositionStays: false);
				Pool.Push(audioSource);
			}
		}
	}

	private static AudioSource CreateAudioSource()
	{
		_audioSourceCount++;
		GameObject gameObject = new GameObject($"AS{_audioSourceCount:00}");
		gameObject.hideFlags = HideFlags.DontSave;
		gameObject.SetActive(value: false);
		AudioSource audioSource = gameObject.AddComponent<AudioSource>();
		audioSource.playOnAwake = false;
		AudioHighPassFilter audioHighPassFilter = gameObject.AddComponent<AudioHighPassFilter>();
		AudioLowPassFilter audioLowPassFilter = gameObject.AddComponent<AudioLowPassFilter>();
		audioHighPassFilter.enabled = false;
		audioLowPassFilter.enabled = false;
		gameObject.SetActive(value: true);
		All.Add(audioSource);
		return audioSource;
	}
}
