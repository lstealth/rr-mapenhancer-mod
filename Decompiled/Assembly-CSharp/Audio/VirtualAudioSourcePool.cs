using System.Collections.Generic;
using Helpers;
using Serilog;
using UnityEngine;

namespace Audio;

public class VirtualAudioSourcePool : MonoBehaviour
{
	private static VirtualAudioSourcePool _instance;

	private CullingGroup _cullingGroup;

	private BoundingSphere[] _spheres;

	private readonly List<VirtualAudioSource> _sources = new List<VirtualAudioSource>();

	private readonly List<VirtualAudioSource> _returnAfterFinished = new List<VirtualAudioSource>();

	private int _sequence;

	private static readonly float[] DistanceBands = new float[5]
	{
		3f,
		50f,
		100f,
		1000f,
		float.MaxValue
	};

	internal static float GlobalDopplerLevel;

	private static VirtualAudioSourcePool Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Object.FindObjectOfType<VirtualAudioSourcePool>();
			}
			return _instance;
		}
	}

	public static IAudioSource Checkout(string name, AudioClip clip, bool loop, AudioController.Group mixerGroup, int priority, Transform parent, AudioDistance cullDistance, Vector3 offset = default(Vector3))
	{
		VirtualAudioSourcePool instance = Instance;
		if (instance == null)
		{
			Log.Error("VirtualAudioSourcePool.Checkout: Instance is null");
			return null;
		}
		return instance.ActualCheckout(name, clip, loop, mixerGroup, priority, parent, offset, cullDistance);
	}

	public static void Return(IAudioSource audioSource)
	{
		if (audioSource != null)
		{
			VirtualAudioSourcePool instance = Instance;
			if (!(instance == null))
			{
				instance.ActualReturn(audioSource);
			}
		}
	}

	public static void ReturnAfterFinished(IAudioSource audioSource)
	{
		if (audioSource != null)
		{
			VirtualAudioSourcePool instance = Instance;
			if (!(instance == null))
			{
				instance.ActualReturnAfterFinished(audioSource);
			}
		}
	}

	private void Awake()
	{
		_spheres = new BoundingSphere[64];
		_cullingGroup = new CullingGroup();
		_cullingGroup.SetBoundingSpheres(_spheres);
		_cullingGroup.SetBoundingSphereCount(0);
		_cullingGroup.SetBoundingDistances(DistanceBands);
		_cullingGroup.onStateChanged = OnStateChanged;
	}

	private void OnDestroy()
	{
		_cullingGroup?.Dispose();
		_cullingGroup = null;
	}

	private void OnStateChanged(CullingGroupEvent sphere)
	{
		if (sphere.index >= _sources.Count)
		{
			Debug.LogWarning($"Ignoring CullingGroup change for out-of-range index {sphere.index}");
		}
		else
		{
			_sources[sphere.index]?.UpdateNearbyForDistanceBand(sphere.currentDistance);
		}
	}

	private void FixedUpdate()
	{
		UpdateTargetCamera();
		UpdateCullingSpheres();
		for (int num = _returnAfterFinished.Count - 1; num >= 0; num--)
		{
			VirtualAudioSource virtualAudioSource = _returnAfterFinished[num];
			if (!virtualAudioSource.IsReal || !virtualAudioSource.isPlaying)
			{
				ActualReturn(virtualAudioSource);
				_returnAfterFinished.RemoveAt(num);
			}
		}
	}

	private IAudioSource ActualCheckout(string sourceName, AudioClip clip, bool loop, AudioController.Group mixerGroup, int priority, Transform parent, Vector3 parentOffset, AudioDistance cullDistance)
	{
		AudioReparenter componentInParent = parent.GetComponentInParent<AudioReparenter>();
		if (componentInParent != null)
		{
			parent = componentInParent.Reparent(parent, out var offset);
			parentOffset += offset;
		}
		VirtualAudioSource virtualAudioSource = new VirtualAudioSource(_sequence++, sourceName, clip, loop, mixerGroup, priority, cullDistance, parent, parentOffset);
		int i;
		for (i = 0; i < _sources.Count && _sources[i] != null; i++)
		{
		}
		if (i == _sources.Count)
		{
			_sources.Add(virtualAudioSource);
		}
		else
		{
			_sources[i] = virtualAudioSource;
		}
		if (_spheres.Length < _sources.Count)
		{
			_spheres = new BoundingSphere[(int)((double)_sources.Count * 1.5)];
			_cullingGroup.SetBoundingSpheres(_spheres);
		}
		UpdateCullingSpheres();
		UpdateTargetCamera();
		_cullingGroup.SetBoundingSphereCount(_sources.Count);
		int distanceBand = _cullingGroup.CalculateDistanceBand(_spheres[i].position, DistanceBands);
		virtualAudioSource.UpdateNearbyForDistanceBand(distanceBand);
		return virtualAudioSource;
	}

	private void ActualReturn(IAudioSource audioSource)
	{
		VirtualAudioSource virtualAudioSource = (VirtualAudioSource)audioSource;
		int num = _sources.IndexOf(virtualAudioSource);
		if (num >= 0)
		{
			_sources[num] = null;
		}
		virtualAudioSource.ReturnAudioSource();
	}

	private void ActualReturnAfterFinished(IAudioSource audioSource)
	{
		VirtualAudioSource virtualAudioSource = (VirtualAudioSource)audioSource;
		if (virtualAudioSource.IsReal)
		{
			_returnAfterFinished.Add(virtualAudioSource);
		}
		else
		{
			ActualReturn(virtualAudioSource);
		}
	}

	private void UpdateCullingSpheres()
	{
		for (int i = 0; i < _sources.Count; i++)
		{
			VirtualAudioSource virtualAudioSource = _sources[i];
			if (virtualAudioSource != null)
			{
				if (virtualAudioSource.parentTransform == null)
				{
					Debug.LogWarning($"Found null parentTransform for clip suggesting source was not correctly returned: {virtualAudioSource.clip}");
					ActualReturn(virtualAudioSource);
					break;
				}
				_spheres[i].position = virtualAudioSource.parentTransform.TransformPoint(virtualAudioSource.parentOffset);
				_spheres[i].radius = 1f;
			}
		}
	}

	private void UpdateTargetCamera()
	{
		if (!(_cullingGroup.targetCamera != null))
		{
			Camera main = Camera.main;
			if (!(main == null))
			{
				_cullingGroup.targetCamera = main;
				_cullingGroup.SetDistanceReferencePoint(main.transform);
			}
		}
	}

	public static void SetGlobalDopplerLevel(float value)
	{
		GlobalDopplerLevel = value;
		if (_instance == null)
		{
			return;
		}
		foreach (VirtualAudioSource source in _instance._sources)
		{
			source?.UpdateDopplerLevel();
		}
	}
}
