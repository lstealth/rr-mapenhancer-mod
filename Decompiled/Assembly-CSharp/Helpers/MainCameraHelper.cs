using JetBrains.Annotations;
using UnityEngine;

namespace Helpers;

public static class MainCameraHelper
{
	[ContractAnnotation("=> true, mainCamera: notnull; => false, mainCamera: null")]
	public static bool TryGetIfNeeded(ref Camera mainCamera)
	{
		if (mainCamera == null)
		{
			Camera main = Camera.main;
			if (main == null)
			{
				return false;
			}
			mainCamera = main;
		}
		return true;
	}
}
