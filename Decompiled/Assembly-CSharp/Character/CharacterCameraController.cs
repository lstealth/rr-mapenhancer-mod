using Game;
using JetBrains.Annotations;
using UnityEngine;

namespace Character;

public class CharacterCameraController : MonoBehaviour
{
	[Header("Rotation")]
	[Range(-90f, 90f)]
	public float defaultVerticalAngle;

	[Range(-90f, 90f)]
	public float minVerticalAngle = -90f;

	[Range(-90f, 90f)]
	public float maxVerticalAngle = 90f;

	public float rotationSpeed = 1f;

	public float rotationSharpness = 10000f;

	private Transform _transform;

	private bool _distanceIsObstructed;

	private float _targetPitch;

	private float _targetYaw;

	private Lean _lastLean;

	[CanBeNull]
	private Camera _camera;

	private float _targetFieldOfView;

	private static float DefaultFOV => Preferences.DefaultFOV;

	private static float DefaultWideFOV => Preferences.AlternateFOV;

	private void OnValidate()
	{
		defaultVerticalAngle = Mathf.Clamp(defaultVerticalAngle, minVerticalAngle, maxVerticalAngle);
	}

	private void Awake()
	{
		_transform = base.transform;
		_targetPitch = defaultVerticalAngle;
		_targetFieldOfView = DefaultFOV;
	}

	public void Configure(float initialYaw)
	{
		_targetYaw = initialYaw;
	}

	public void UpdateWithInput(float deltaTime, float inputPitch, float inputYaw, float inputZoom, bool inputResetZoom, Lean lean)
	{
		if (lean != _lastLean)
		{
			_targetYaw = 0f;
			_lastLean = lean;
		}
		_targetPitch -= inputPitch * rotationSpeed;
		_targetPitch = Mathf.Clamp(_targetPitch, minVerticalAngle, maxVerticalAngle);
		_targetYaw += inputYaw * rotationSpeed;
		Quaternion b = Quaternion.Euler(_targetPitch, _targetYaw, 0f);
		Quaternion localRotation = Quaternion.Slerp(_transform.localRotation, b, 1f - Mathf.Exp((0f - rotationSharpness) * deltaTime));
		_transform.localRotation = localRotation;
		UpdateZoom(inputZoom, inputResetZoom);
	}

	public void SetRotation(Quaternion rotation)
	{
		float x = Quaternion.LookRotation(Vector3.ProjectOnPlane(rotation * Vector3.forward, Vector3.up)).eulerAngles.x;
		_targetPitch = x;
	}

	private void UpdateZoom(float inputZoom, bool inputResetFOV)
	{
		if (Mathf.Abs(inputZoom) > 0.001f)
		{
			float num = (0f - inputZoom) * 8f;
			_targetFieldOfView = Mathf.Clamp(_targetFieldOfView + num, 4f, 120f);
		}
		if (inputResetFOV)
		{
			float num2 = Mathf.Abs(_targetFieldOfView - DefaultFOV);
			float num3 = Mathf.Abs(_targetFieldOfView - DefaultWideFOV);
			if (num2 < 1f)
			{
				_targetFieldOfView = DefaultWideFOV;
			}
			else if (num3 < 1f)
			{
				_targetFieldOfView = DefaultFOV;
			}
			else if (num2 < num3)
			{
				_targetFieldOfView = DefaultFOV;
			}
			else
			{
				_targetFieldOfView = DefaultWideFOV;
			}
		}
		_camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, _targetFieldOfView, Time.deltaTime * 10f);
	}

	public void SetSelected(bool selected, Camera theCamera)
	{
		_camera = theCamera;
		if (selected)
		{
			_camera.fieldOfView = _targetFieldOfView;
		}
	}
}
