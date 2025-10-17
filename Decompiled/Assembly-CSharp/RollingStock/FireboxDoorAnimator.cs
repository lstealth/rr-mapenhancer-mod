using System;
using System.Collections;
using Helpers.Animation;
using KeyValue.Runtime;
using UnityEngine;

namespace RollingStock;

public class FireboxDoorAnimator : MonoBehaviour
{
	public string key = "fireboxDoor";

	public AnimationClip animationClip;

	[Range(0.1f, 10f)]
	public float animationSpeed = 1f;

	public Animator animator;

	private IDisposable _observer;

	private PlayableHandle _playable;

	private float _targetTime;

	private Coroutine _updateCoroutine;

	private void OnEnable()
	{
		_observer = GetComponentInParent<KeyValueObject>().Observe(key, PropertyDidChange);
		_playable = animator.PlayableGraphAdapter().AddPlayable(animationClip);
	}

	private void OnDisable()
	{
		_observer?.Dispose();
		_observer = null;
		_playable?.Dispose();
		_playable = null;
	}

	private void PropertyDidChange(Value value)
	{
		float length = animationClip.length;
		_targetTime = length * (value.BoolValue ? 0.5f : 1f);
		if (_updateCoroutine == null)
		{
			_updateCoroutine = StartCoroutine(MoveToTarget());
		}
	}

	private IEnumerator MoveToTarget()
	{
		if (_playable != null)
		{
			while (Math.Abs(_playable.Time - _targetTime) > 0.01f)
			{
				_playable.Time = Mathf.MoveTowards(_playable.Time, _targetTime, Time.deltaTime * animationSpeed);
				yield return null;
			}
			if (_targetTime > animationClip.length * 0.99f)
			{
				_playable.Time = 0f;
			}
		}
		_updateCoroutine = null;
	}
}
