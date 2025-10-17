using TMPro;
using UnityEngine;

internal static class LeanTweenTMPExt
{
	public static LTDescr alphaText(this TMP_Text textMesh, float to, float time)
	{
		Color color = textMesh.color;
		return LeanTween.value(textMesh.gameObject, color.a, to, time).setOnUpdate(delegate(float value)
		{
			color.a = value;
			textMesh.color = color;
		});
	}
}
