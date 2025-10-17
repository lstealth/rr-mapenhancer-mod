using Helpers.Animation;
using UnityEngine;

namespace RollingStock;

public class BrakeAnimator : MonoBehaviour, IBrakeAnimator
{
	public Animator animator;

	public AnimationClip[] brakeAnimationClips;

	private PlayableHandle[] _brakePlayables;

	private bool _brakeWasApplied;

	public bool BrakeApplied
	{
		get
		{
			return _brakeWasApplied;
		}
		set
		{
			if (value != _brakeWasApplied)
			{
				_brakeWasApplied = value;
				BrakeWasAppliedDidChange();
			}
		}
	}

	private void Start()
	{
		PlayableGraphAnimatorAdapter playableGraphAnimatorAdapter = animator.PlayableGraphAdapter();
		_brakePlayables = new PlayableHandle[brakeAnimationClips.Length];
		for (int i = 0; i < brakeAnimationClips.Length; i++)
		{
			_brakePlayables[i] = playableGraphAnimatorAdapter.AddPlayable(brakeAnimationClips[i]);
		}
	}

	private void OnDestroy()
	{
		PlayableHandle[] brakePlayables = _brakePlayables;
		for (int i = 0; i < brakePlayables.Length; i++)
		{
			brakePlayables[i]?.Dispose();
		}
	}

	private void BrakeWasAppliedDidChange()
	{
		if (_brakePlayables.Length != 0)
		{
			PlayableHandle[] brakePlayables = _brakePlayables;
			foreach (PlayableHandle obj in brakePlayables)
			{
				obj.ClampTimeToClipBounds();
				obj.Speed = (BrakeApplied ? 1 : (-1));
				obj.Play();
			}
			_brakeWasApplied = BrakeApplied;
		}
	}

	private void OnValidate()
	{
		if (animator == null)
		{
			animator = GetComponent<Animator>();
		}
	}
}
