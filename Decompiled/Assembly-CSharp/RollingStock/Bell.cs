using Audio;
using Helpers.Animation;
using UnityEngine;
using UnityEngine.Playables;

namespace RollingStock;

public class Bell : MonoBehaviour
{
	public IntegerLoopingPlayer player;

	public AnimationClip animationClip;

	public Animator animator;

	private Helpers.Animation.PlayableHandle _clipPlayable;

	private float _animationLength;

	public bool IsOn
	{
		get
		{
			return player.play;
		}
		set
		{
			player.play = value;
		}
	}

	private float normalAnimationSpeed => 0.5f * _animationLength / player.averageClipLength;

	private void Start()
	{
		player.AudioSourceName = "Bell";
		player.priority = 10;
		SetupAnimation();
	}

	private void OnDestroy()
	{
		_clipPlayable?.Dispose();
	}

	private void Update()
	{
		UpdateAnimation();
	}

	private void SetupAnimation()
	{
		if (!(animationClip == null))
		{
			PlayableGraphAnimatorAdapter playableGraphAnimatorAdapter = animator.PlayableGraphAdapter();
			_clipPlayable = playableGraphAnimatorAdapter.AddPlayable(animationClip);
			_animationLength = animationClip.length;
		}
	}

	private void UpdateAnimation()
	{
		if (_clipPlayable == null)
		{
			return;
		}
		bool flag = _clipPlayable.PlayState == PlayState.Playing;
		if (IsOn)
		{
			_clipPlayable.ClampTimeToClipBounds();
			if (!flag)
			{
				_clipPlayable.Speed = normalAnimationSpeed;
				_clipPlayable.Play();
			}
			if (_clipPlayable.Time >= _animationLength)
			{
				_clipPlayable.Time = 0f;
			}
		}
		else if (flag)
		{
			float num = _clipPlayable.Time / _animationLength;
			if ((double)num >= 1.0)
			{
				_clipPlayable.Time = 0f;
				_clipPlayable.Pause();
			}
			else if (num > 0.6f)
			{
				_clipPlayable.Speed = Mathf.MoveTowards(_clipPlayable.Speed, 0.75f * normalAnimationSpeed, Time.deltaTime);
			}
		}
	}
}
