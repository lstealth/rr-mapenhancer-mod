using UnityEngine;

namespace Helpers.Animation;

public static class AnimationClipChecker
{
	public static bool CheckAnimationClip(this Object obj, AnimationClip animationClip)
	{
		if (animationClip != null)
		{
			return true;
		}
		try
		{
			Debug.LogError("animationClip on " + obj.name + " is null: " + animationClip.name, obj);
		}
		catch (MissingReferenceException)
		{
			Debug.LogError("animationClip on " + obj.name + " is null", obj);
		}
		return false;
	}
}
