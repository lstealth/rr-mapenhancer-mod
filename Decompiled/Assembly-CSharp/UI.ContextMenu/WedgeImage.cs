using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI.ContextMenu;

public class WedgeImage : Image
{
	[Range(0f, 360f)]
	public float startAngle;

	[Range(0f, 360f)]
	public float angleRange = 45f;

	[Range(0f, 1f)]
	public float innerRadius = 0.5f;

	protected override void OnPopulateMesh(VertexHelper vh)
	{
		vh.Clear();
		float num = base.rectTransform.rect.width * 0.5f;
		float num2 = num * innerRadius;
		int num3 = Mathf.Max(1, Mathf.CeilToInt(angleRange / 5f));
		float num4 = angleRange / (float)num3;
		UIVertex simpleVert = UIVertex.simpleVert;
		simpleVert.color = color;
		for (int i = 0; i <= num3; i++)
		{
			float f = (startAngle + (float)i * num4) * (MathF.PI / 180f);
			float num5 = Mathf.Cos(f);
			float num6 = Mathf.Sin(f);
			simpleVert.position = new Vector3(num5 * num2, num6 * num2, 0f);
			simpleVert.uv0 = new Vector2((simpleVert.position.x / num + 1f) * 0.5f, (simpleVert.position.y / num + 1f) * 0.5f);
			vh.AddVert(simpleVert);
			simpleVert.position = new Vector3(num5 * num, num6 * num, 0f);
			simpleVert.uv0 = new Vector2((simpleVert.position.x / num + 1f) * 0.5f, (simpleVert.position.y / num + 1f) * 0.5f);
			vh.AddVert(simpleVert);
			if (i > 0)
			{
				int num7 = (i - 1) * 2;
				vh.AddTriangle(num7, num7 + 1, num7 + 2);
				vh.AddTriangle(num7 + 1, num7 + 3, num7 + 2);
			}
		}
	}

	public override bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
	{
		RectTransformUtility.ScreenPointToLocalPointInRectangle(base.rectTransform, screenPoint, eventCamera, out var localPoint);
		float magnitude = localPoint.magnitude;
		float num = base.rectTransform.rect.width * 0.5f;
		float num2 = num * innerRadius;
		if (magnitude < num2 || magnitude > num)
		{
			return false;
		}
		float num3 = Vector2.SignedAngle(Vector2.right, localPoint);
		if (num3 < 0f)
		{
			num3 += 360f;
		}
		float num4 = startAngle + angleRange;
		if (num4 > 360f)
		{
			if (!(num3 >= startAngle))
			{
				return num3 <= num4 - 360f;
			}
			return true;
		}
		if (num3 >= startAngle)
		{
			return num3 <= num4;
		}
		return false;
	}
}
