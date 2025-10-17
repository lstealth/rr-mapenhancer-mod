using UnityEngine;

namespace UI;

public static class CanvasPositioningExtensions
{
	public static Vector3 WorldToCanvasPosition(this Canvas canvas, Vector3 worldPosition, Camera camera)
	{
		Vector3 viewportPosition = camera.WorldToViewportPoint(worldPosition);
		return canvas.ViewportToCanvasPosition(viewportPosition);
	}

	public static Vector3 ScreenToCanvasPosition(this Canvas canvas, Vector3 screenPosition)
	{
		Vector3 viewportPosition = new Vector3(screenPosition.x / (float)Screen.width, screenPosition.y / (float)Screen.height, 0f);
		return canvas.ViewportToCanvasPosition(viewportPosition);
	}

	public static Vector3 ViewportToCanvasPosition(this Canvas canvas, Vector3 viewportPosition)
	{
		Vector2 sizeDelta = canvas.GetComponent<RectTransform>().sizeDelta;
		return Vector3.Scale(viewportPosition, sizeDelta);
	}

	public static Vector2 Clamped(this Canvas canvas, Vector2 pos, Rect rect)
	{
		Rect pixelRect = canvas.pixelRect;
		float num = rect.width / 2f;
		float max = pixelRect.width - num;
		float num2 = rect.height / 2f;
		float max2 = pixelRect.height - num2;
		return new Vector2
		{
			x = Mathf.Clamp(pos.x, num, max),
			y = Mathf.Clamp(pos.y, num2, max2)
		};
	}
}
