using Character;
using Helpers;
using UI;
using UnityEngine;

namespace Cameras;

public class StationaryCameraController : MonoBehaviour, ICameraSelectable
{
	private MouseLookInput _mouseLookInput;

	private CharacterCameraController _cameraController;

	private bool _isSelected;

	public Transform CameraContainer => base.transform;

	public Vector3 GroundPosition => base.transform.position;

	private void Awake()
	{
		_mouseLookInput = base.gameObject.AddComponent<MouseLookInput>();
		_cameraController = base.gameObject.AddComponent<CharacterCameraController>();
		_cameraController.Configure(base.transform.eulerAngles.y);
	}

	private void Update()
	{
		if (_isSelected && GameInput.shared.MoveVector.sqrMagnitude > 0.01f)
		{
			Vector3 worldPosition = base.transform.position;
			if (Physics.Raycast(base.transform.position, Vector3.down, out var hitInfo, 5f, 1 << Layers.Default))
			{
				worldPosition = hitInfo.point;
			}
			CameraSelector.shared.JumpCharacterTo(worldPosition.WorldToGame(), null, base.transform.forward);
		}
		_mouseLookInput.UpdateInput(_isSelected);
	}

	private void LateUpdate()
	{
		if (GameInput.MovementInputEnabled && _isSelected)
		{
			float yaw = _mouseLookInput.Yaw;
			GameInput shared = GameInput.shared;
			float zoomDelta = shared.ZoomDelta;
			bool inputResetFOV = shared.InputResetFOV;
			_cameraController.UpdateWithInput(Time.deltaTime, _mouseLookInput.Pitch, yaw, zoomDelta, inputResetFOV, Lean.Off);
		}
	}

	public void SetSelected(bool selected, Camera theCamera)
	{
		_isSelected = selected;
		_cameraController.SetSelected(selected, theCamera);
	}

	GameObject ICameraSelectable.get_gameObject()
	{
		return base.gameObject;
	}
}
