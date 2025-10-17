using System;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Helpers.Animation;

public class PlayableHandle : IDisposable
{
	private readonly PlayableGraphAnimatorAdapter _adapter;

	private readonly int _port;

	private readonly AnimationClipPlayable _playable;

	public float Time
	{
		get
		{
			return (float)_playable.GetTime();
		}
		set
		{
			_playable.SetTime(value);
		}
	}

	public float Speed
	{
		get
		{
			return (float)_playable.GetSpeed();
		}
		set
		{
			_playable.SetSpeed(value);
		}
	}

	public PlayState PlayState => _playable.GetPlayState();

	public PlayableHandle(PlayableGraphAnimatorAdapter adapter, int port, AnimationClipPlayable playable)
	{
		_adapter = adapter;
		_port = port;
		_playable = playable;
	}

	public void Dispose()
	{
		_adapter.Remove(_port);
		_playable.Destroy();
	}

	public void Play()
	{
		_playable.Play();
	}

	public void Pause()
	{
		_playable.Pause();
	}

	public void ClampTimeToClipBounds()
	{
		float time = Time;
		if (time < 0f)
		{
			Time = 0f;
			return;
		}
		float length = _playable.GetAnimationClip().length;
		if (time > length)
		{
			Time = length;
		}
	}
}
