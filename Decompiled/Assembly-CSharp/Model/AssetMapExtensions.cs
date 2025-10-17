using AssetPack.Common;
using Model.Definition.Data;
using UnityEngine;

namespace Model;

public static class AssetMapExtensions
{
	public static AnimationClip Resolve(this AnimationMap animationMap, AnimationReference animationReference)
	{
		if (animationReference == null)
		{
			Debug.LogError("AnimationReference is null");
			return null;
		}
		return animationMap.ClipForName(animationReference.ClipName);
	}

	public static Material Resolve(this MaterialMap animationMap, MaterialReference materialReference)
	{
		if (materialReference == null)
		{
			Debug.LogError("materialReference is null");
			return null;
		}
		return animationMap.MaterialForName(materialReference.MaterialName);
	}
}
