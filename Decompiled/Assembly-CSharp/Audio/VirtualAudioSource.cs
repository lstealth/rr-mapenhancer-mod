using JetBrains.Annotations;
using UnityEngine;

namespace Audio;

internal class VirtualAudioSource : IAudioSource
{
	internal Transform parentTransform;

	internal Vector3 parentOffset;

	internal readonly AudioDistance cullDistance;

	private readonly int _id;

	private readonly string _name;

	private AudioSource _audioSource;

	private bool _play;

	private AudioClip _clip;

	private float _volume = 1f;

	private AudioRolloffMode _rolloffMode;

	[CanBeNull]
	private AnimationCurve _rolloffCurve;

	private float _minDistance = 20f;

	private float _maxDistance = 100f;

	private float _time;

	private float _pitch = 1f;

	private float _dopplerLevel = 1f;

	private float _spatialBlend = 1f;

	private double? _schedulePlay;

	private bool _loop;

	private readonly AudioController.Group _mixerGroup;

	private readonly int _priority;

	private float? _highPassCutoff;

	private float? _lowPassCutoff;

	private AudioHighPassFilter _highPassFilter;

	private AudioLowPassFilter _lowPassFilter;

	public AudioClip clip
	{
		get
		{
			return _clip;
		}
		set
		{
			_clip = value;
			if (_audioSource != null)
			{
				_audioSource.clip = _clip;
			}
		}
	}

	public float volume
	{
		get
		{
			return _volume;
		}
		set
		{
			_volume = value;
			if (_audioSource != null)
			{
				_audioSource.volume = _volume;
			}
		}
	}

	public AudioRolloffMode rolloffMode
	{
		get
		{
			return _rolloffMode;
		}
		set
		{
			_rolloffMode = value;
			if (_audioSource != null)
			{
				_audioSource.rolloffMode = _rolloffMode;
			}
		}
	}

	public float minDistance
	{
		get
		{
			return _minDistance;
		}
		set
		{
			_minDistance = value;
			if (_audioSource != null)
			{
				_audioSource.minDistance = _minDistance;
			}
		}
	}

	public float maxDistance
	{
		get
		{
			return _maxDistance;
		}
		set
		{
			_maxDistance = value;
			if (_audioSource != null)
			{
				_audioSource.maxDistance = _maxDistance;
			}
		}
	}

	public AnimationCurve rolloffCurve
	{
		get
		{
			return _rolloffCurve;
		}
		set
		{
			_rolloffCurve = value;
			if (_audioSource != null && _rolloffCurve != null)
			{
				_audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, _rolloffCurve);
			}
		}
	}

	public float time
	{
		get
		{
			if (!(_audioSource == null))
			{
				return _audioSource.time;
			}
			return _time;
		}
		set
		{
			_time = value;
			if (_audioSource != null)
			{
				_audioSource.time = _time;
			}
		}
	}

	public float pitch
	{
		get
		{
			return _pitch;
		}
		set
		{
			_pitch = value;
			if (_audioSource != null)
			{
				_audioSource.pitch = _pitch;
			}
		}
	}

	public float dopplerLevel
	{
		get
		{
			return _dopplerLevel;
		}
		set
		{
			_dopplerLevel = value;
			if (_audioSource != null)
			{
				_audioSource.dopplerLevel = VirtualAudioSourcePool.GlobalDopplerLevel * _dopplerLevel;
			}
		}
	}

	public float spatialBlend
	{
		get
		{
			return _spatialBlend;
		}
		set
		{
			_spatialBlend = value;
			if (_audioSource != null)
			{
				_audioSource.spatialBlend = _spatialBlend;
			}
		}
	}

	public bool loop
	{
		get
		{
			return _loop;
		}
		set
		{
			_loop = value;
			if (_audioSource != null)
			{
				_audioSource.loop = _loop;
			}
		}
	}

	public AudioController.Group outputAudioMixerGroup => _mixerGroup;

	public bool isPlaying
	{
		get
		{
			if (_audioSource != null)
			{
				return _audioSource.isPlaying;
			}
			if (_schedulePlay.HasValue)
			{
				return AudioSettings.dspTime < _schedulePlay.Value + (double)clip.length;
			}
			return _play;
		}
	}

	public bool IsReal => _audioSource != null;

	internal VirtualAudioSource(int id, string name, AudioClip clip, bool loop, AudioController.Group mixerGroup, int priority, AudioDistance cullDistance, Transform parentTransform, Vector3 parentOffset)
	{
		_id = id;
		_name = name;
		this.clip = clip;
		_loop = loop;
		_mixerGroup = mixerGroup;
		_priority = priority;
		this.cullDistance = cullDistance;
		this.parentTransform = parentTransform;
		this.parentOffset = parentOffset;
	}

	public override string ToString()
	{
		return string.Format("V{0:D3} {1}", _id, _name ?? "");
	}

	public void Play()
	{
		_play = true;
		_schedulePlay = null;
		if (_audioSource != null)
		{
			_audioSource.Play();
		}
	}

	public void Stop()
	{
		_play = false;
		_schedulePlay = null;
		if (_audioSource != null)
		{
			_audioSource.Stop();
		}
	}

	public void PlayScheduled(double scheduleTime)
	{
		_schedulePlay = scheduleTime;
		_play = true;
		if (_audioSource != null)
		{
			_audioSource.PlayScheduled(scheduleTime);
		}
	}

	internal void ReturnAudioSource()
	{
		if (_audioSource != null)
		{
			AudioSourcePool.Return(_audioSource);
		}
		_audioSource = null;
		if (_highPassFilter != null)
		{
			_highPassFilter.enabled = false;
		}
		if (_lowPassFilter != null)
		{
			_lowPassFilter.enabled = false;
		}
		_highPassFilter = null;
		_lowPassFilter = null;
	}

	public void SetNearby(bool nearby)
	{
		if (nearby == (_audioSource != null))
		{
			return;
		}
		if (nearby)
		{
			_audioSource = AudioSourcePool.Checkout(clip, _loop, _mixerGroup, _priority, parentTransform, parentOffset);
			_audioSource.name = ToString();
			_audioSource.volume = _volume;
			_audioSource.rolloffMode = _rolloffMode;
			_audioSource.minDistance = _minDistance;
			_audioSource.maxDistance = _maxDistance;
			if (_rolloffCurve != null)
			{
				_audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, _rolloffCurve);
			}
			_audioSource.time = _time;
			_audioSource.pitch = _pitch;
			_audioSource.dopplerLevel = VirtualAudioSourcePool.GlobalDopplerLevel * _dopplerLevel;
			_audioSource.spatialBlend = _spatialBlend;
			if (_highPassCutoff.HasValue)
			{
				SetHighPassCutoff(_highPassCutoff.Value);
			}
			if (_lowPassCutoff.HasValue)
			{
				SetLowPassCutoff(_lowPassCutoff.Value);
			}
			if (_play && clip != null)
			{
				if (_schedulePlay.HasValue)
				{
					_audioSource.PlayScheduled(_schedulePlay.Value);
				}
				else
				{
					_audioSource.Play();
				}
			}
		}
		else
		{
			_play = _audioSource.isPlaying;
			ReturnAudioSource();
		}
	}

	public void UpdateNearbyForDistanceBand(int distanceBand)
	{
		bool nearby = distanceBand <= (int)cullDistance;
		SetNearby(nearby);
	}

	public void SetHighPassCutoff(float cutoff)
	{
		_highPassCutoff = cutoff;
		if (!(_audioSource == null))
		{
			if ((object)_highPassFilter == null)
			{
				_highPassFilter = _audioSource.GetComponent<AudioHighPassFilter>();
			}
			_highPassFilter.enabled = true;
			_highPassFilter.cutoffFrequency = cutoff;
		}
	}

	public void SetLowPassCutoff(float cutoff)
	{
		_lowPassCutoff = cutoff;
		if (!(_audioSource == null))
		{
			if ((object)_lowPassFilter == null)
			{
				_lowPassFilter = _audioSource.GetComponent<AudioLowPassFilter>();
			}
			_lowPassFilter.enabled = true;
			_lowPassFilter.cutoffFrequency = cutoff;
		}
	}

	public void UpdateDopplerLevel()
	{
		if (!(_audioSource == null))
		{
			_audioSource.dopplerLevel = VirtualAudioSourcePool.GlobalDopplerLevel * _dopplerLevel;
		}
	}
}
