using UnityEngine;

namespace Helpers.Animation;

public static class AnimatorExtensionForPlayableGraph
{
	public static PlayableGraphAnimatorAdapter PlayableGraphAdapter(this Animator animator)
	{
		PlayableGraphAnimatorAdapter playableGraphAnimatorAdapter = animator.GetComponent<PlayableGraphAnimatorAdapter>();
		if (playableGraphAnimatorAdapter == null)
		{
			playableGraphAnimatorAdapter = animator.gameObject.AddComponent<PlayableGraphAnimatorAdapter>();
		}
		return playableGraphAnimatorAdapter;
	}
}
