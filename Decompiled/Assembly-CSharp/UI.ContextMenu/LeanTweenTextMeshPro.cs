using TMPro;

namespace UI.ContextMenu;

internal static class LeanTweenTextMeshPro
{
	public static LTDescr fontSize(this TMP_Text textMesh, float targetSize, float time)
	{
		float num = textMesh.fontSize;
		return LeanTween.value(textMesh.gameObject, num, targetSize, time).setOnUpdate(delegate(float value)
		{
			textMesh.fontSize = value;
		});
	}
}
