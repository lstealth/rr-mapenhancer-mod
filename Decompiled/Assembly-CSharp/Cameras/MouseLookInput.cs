using Game;
using UI;
using UnityEngine;

namespace Cameras;

public class MouseLookInput : MonoBehaviour
{
	private bool _mouseMovesCamera;

	private const float CameraInputSharpness = 20f;

	public float Pitch { get; private set; }

	public float Yaw { get; private set; }

	private static bool ToggleMouseLook => Preferences.MouseLookToggle;

	private static float MouseLookSpeed => Preferences.MouseLookSpeed;

	private static float CameraXMultiplier => MouseLookSpeed;

	private static float CameraYMultiplier => MouseLookSpeed * (float)((!Preferences.MouseLookInvert) ? 1 : (-1));

	private void OnDisable()
	{
		SetMouseMovesCamera(mouseMovesCamera: false);
	}

	public void UpdateInput(bool selected)
	{
		if (!GameInput.MovementInputEnabled || !selected)
		{
			SetMouseMovesCamera(mouseMovesCamera: false);
		}
		if (selected)
		{
			GameInput shared = GameInput.shared;
			if (ToggleMouseLook)
			{
				if (shared.MouseLookToggle)
				{
					SetMouseMovesCamera(!_mouseMovesCamera);
				}
			}
			else if (shared.SecondaryLongPressBeganThisFrame)
			{
				SetMouseMovesCamera(mouseMovesCamera: true);
			}
			else if (shared.SecondaryLongPressEndedThisFrame)
			{
				SetMouseMovesCamera(mouseMovesCamera: false);
			}
			Vector2 vector = (_mouseMovesCamera ? shared.LookDelta : Vector2.zero);
			float b = CameraXMultiplier * vector.x;
			float b2 = CameraYMultiplier * vector.y;
			float t = 1f - Mathf.Exp(-20f * Time.deltaTime);
			Pitch = Mathf.Lerp(Pitch, b2, t);
			Yaw = Mathf.Lerp(Yaw, b, t);
		}
		else
		{
			Pitch = 0f;
			Yaw = 0f;
		}
	}

	public void SetMouseMovesCamera(bool mouseMovesCamera)
	{
		if (_mouseMovesCamera != mouseMovesCamera)
		{
			_mouseMovesCamera = mouseMovesCamera;
			Cursor.lockState = (_mouseMovesCamera ? CursorLockMode.Locked : CursorLockMode.None);
		}
	}
}
