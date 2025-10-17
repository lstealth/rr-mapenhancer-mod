using Helpers.Animation;
using KeyValue.Runtime;
using UnityEngine;
using UnityEngine.Playables;

namespace RollingStock.Controls;

public class KeyValueBoolLoopAnimator : MonoBehaviour
{
	public string key;

	public AnimationClip animationClip;

	public float speed = 1f;

	[Tooltip("True if the bool should be inverted.")]
	public bool invert;

	public Animator animator;

	private Helpers.Animation.PlayableHandle _playable;

	private KeyValueObject propertyObject => GetComponentInParent<KeyValueObject>();

	private void Start()
	{
		_playable = animator.PlayableGraphAdapter().AddPlayable(animationClip);
	}

	private void OnDestroy()
	{
		if (_playable != null)
		{
			_playable.Dispose();
			_playable = null;
		}
	}

	private void Update()
	{
		bool flag = _playable.PlayState == PlayState.Playing;
		bool flag2 = propertyObject[key].BoolValue;
		if (invert)
		{
			flag2 = !flag2;
		}
		if (flag2)
		{
			_playable.ClampTimeToClipBounds();
			if (!flag)
			{
				_playable.Speed = speed;
				_playable.Play();
			}
			if (_playable.Time >= animationClip.length)
			{
				_playable.Time = 0f;
			}
		}
		else if (flag)
		{
			float num = _playable.Time / animationClip.length;
			if ((double)num >= 1.0)
			{
				_playable.Time = 0f;
				_playable.Pause();
			}
			else if (num > 0.6f)
			{
				_playable.Speed = Mathf.MoveTowards(_playable.Speed, 0.75f * speed, Time.deltaTime);
			}
		}
	}
}
