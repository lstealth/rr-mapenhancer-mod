using System;
using Game;
using Helpers.Animation;
using KeyValue.Runtime;
using UnityEngine;

namespace RollingStock.Controls;

[RequireComponent(typeof(Animator))]
public class KeyValueBoolAnimator : GameBehaviour
{
	public string key;

	public AnimationClip animationClip;

	public float speed = 1f;

	[Tooltip("True if the bool should be inverted.")]
	public bool invert;

	public bool logEvents;

	private IDisposable _observer;

	private PlayableHandle _playable;

	private void Awake()
	{
		Animator component = GetComponent<Animator>();
		_playable = component.PlayableGraphAdapter().AddPlayable(animationClip);
	}

	private void OnDestroy()
	{
		_playable?.Dispose();
	}

	protected override void OnEnableWithProperties()
	{
		KeyValueObject componentInParent = GetComponentInParent<KeyValueObject>();
		if (!(componentInParent == null))
		{
			bool flag = componentInParent[key].BoolValue;
			if (invert)
			{
				flag = !flag;
			}
			_playable.Time = TargetTime(flag);
			_playable.Speed = Speed(flag);
			_playable.Play();
			_observer = componentInParent.Observe(key, PropertyChanged, callInitial: false);
			if (logEvents)
			{
				Debug.Log($"KeyValueBoolAnimator {key} Play: speed {_playable.Speed} @ {_playable.Time}");
			}
		}
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		_observer?.Dispose();
	}

	private void PropertyChanged(Value value)
	{
		bool flag = value.BoolValue;
		if (invert)
		{
			flag = !flag;
		}
		float time = _playable.Time;
		if (time < 0f || time > animationClip.length)
		{
			_playable.Time = TargetTime(!flag);
		}
		_playable.Speed = Speed(flag);
		if (logEvents)
		{
			Debug.Log($"KeyValueBoolAnimator {key} Changed: speed {_playable.Speed} @ {_playable.Time}");
		}
	}

	private float Speed(bool value)
	{
		if (animationClip.isLooping)
		{
			if (!value)
			{
				return 0f;
			}
			return speed;
		}
		if (!value)
		{
			return 0f - speed;
		}
		return speed;
	}

	private float TargetTime(bool value)
	{
		if (!value)
		{
			return 0f;
		}
		return animationClip.length;
	}
}
