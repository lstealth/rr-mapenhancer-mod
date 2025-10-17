using System;
using System.Collections;
using GalaSoft.MvvmLight.Messaging;
using Game;
using Game.Events;
using Helpers;
using Model;
using UI;
using UnityEngine;

namespace Cameras;

public class StrategyCameraController : MonoBehaviour, ICameraSelectable
{
	public float normalSpeed = 1f;

	public float fastSpeed = 3f;

	public float fasterSpeed = 10f;

	public float zoomSpeed = 10f;

	[Tooltip("Height off the ground or body position around which to orbit the camera.")]
	public float targetHeightFollow = 2.5f;

	public float targetHeightFree = 1.25f;

	public AnimationCurve distanceToSpeed = AnimationCurve.Linear(10f, 1f, 200f, 4f);

	public AnimationCurve zoomMultiplier;

	[SerializeField]
	private float zoomAngleSpeed = 50f;

	[SerializeField]
	private float zoomAngleYSpeed = 15f;

	private float _distance = 40f;

	private float _angleX = 20f;

	private float _extraHeightForGround;

	private float _angleY = 45f;

	private Vector3 _movementVelocity;

	private float _distanceVelocity;

	private float _angleXVelocity;

	private float _angleYVelocity;

	private float _targetHeight;

	private Rigidbody _rigidbody;

	private Vector3 _targetPosition;

	private Vector3? _moveToTarget;

	private float _moveTimer;

	private Vector3? _panStartPosition;

	private Vector3 _panStartTarget;

	private Vector3 _panStartCameraPosition;

	private Plane _panPlane;

	private bool _rotateStarted;

	private Vector3 _rotateStartPosition;

	private Vector3 _rotateCurrentPosition;

	private Camera _mainCamera;

	private Car _followCar;

	private Quaternion _followCarInitialRotation;

	private float _extraRotationY;

	private Coroutine _lateFixedUpdate;

	private Vector3 _movementInput;

	private float _distanceInput;

	private float _angleXInput;

	private float _angleYInput;

	[Range(0f, 1f)]
	[SerializeField]
	private float snapToGroundResponsiveness = 0.04f;

	private Vector3 _targetSnapPosition;

	private const int PanMouseButton = 0;

	public Transform CameraContainer => base.transform;

	public Vector3 GroundPosition => _targetPosition;

	public Car FollowCar
	{
		get
		{
			return _followCar;
		}
		set
		{
			_followCar = value;
			if (_followCar != null)
			{
				_followCarInitialRotation = _followCar.GetMoverTargetPositionRotation().rotation;
				_movementVelocity = Vector3.zero;
				_moveToTarget = null;
			}
			else
			{
				_angleY += _extraRotationY;
				_extraRotationY = 0f;
			}
		}
	}

	private float ZoomDelta => zoomSpeed * zoomMultiplier.Evaluate(_distance);

	private void Awake()
	{
		_rigidbody = GetComponent<Rigidbody>();
		Messenger.Default.Register<WorldDidMoveEvent>(this, WorldDidMove);
	}

	private void OnDestroy()
	{
		Messenger.Default.Unregister(this);
	}

	private void OnEnable()
	{
		_targetHeight = ((FollowCar != null) ? targetHeightFollow : targetHeightFree);
		UpdatePosition(immediate: true);
		_lateFixedUpdate = StartCoroutine(LateFixedUpdateLoop());
	}

	private void OnDisable()
	{
		StopCoroutine(_lateFixedUpdate);
		_lateFixedUpdate = null;
	}

	private void Update()
	{
		if (MainCameraHelper.TryGetIfNeeded(ref _mainCamera))
		{
			UpdateInput();
		}
	}

	private IEnumerator LateFixedUpdateLoop()
	{
		WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();
		while (base.enabled)
		{
			yield return waitForFixedUpdate;
			LateFixedUpdate();
		}
	}

	private void LateFixedUpdate()
	{
		UpdateCameraPosition();
	}

	private void WorldDidMove(WorldDidMoveEvent evt)
	{
		Vector3 offset = evt.Offset;
		_targetPosition += offset;
		base.transform.position += offset;
		if (_moveToTarget.HasValue)
		{
			_moveToTarget = _moveToTarget.Value + offset;
		}
		_panStartCameraPosition += offset;
		_panStartTarget += offset;
		if (_panStartPosition.HasValue)
		{
			_panStartPosition = _panStartPosition.Value + offset;
		}
	}

	private void UpdateInput()
	{
		_distanceInput = 0f;
		_angleXInput = 0f;
		_angleYInput = 0f;
		Vector3 movement = GameInput.shared.GetMovement(normalSpeed, fastSpeed, fasterSpeed);
		_movementInput = new Vector3(movement.x, 0f, movement.z);
		_angleYInput += movement.y / 5f;
		bool flag = false;
		bool flag2 = false;
		if (!GameInput.IsMouseOverUI(out var _, out var _))
		{
			flag = Input.GetMouseButtonDown(0) && !ObjectPicker.Shared.IsOverObject;
			flag2 = Input.GetMouseButtonDown(1);
		}
		if (GameInput.IsMouseOverGameWindow() && Math.Abs(Input.mouseScrollDelta.y) > 0.001f)
		{
			_distanceInput = 0f - Input.mouseScrollDelta.y;
		}
		bool mouseButton = Input.GetMouseButton(0);
		if (flag && RayPointFromMouse(out var point))
		{
			_panStartCameraPosition = CameraContainer.position;
			_panStartPosition = point;
			_panStartTarget = _targetPosition;
			_panPlane = new Plane(Vector3.up, point);
		}
		else if (mouseButton && _panStartPosition.HasValue)
		{
			Vector3 vector = CameraContainer.position - _panStartCameraPosition;
			Ray mouseRay = GetMouseRay();
			mouseRay.origin -= vector;
			_panPlane.Raycast(mouseRay, out var enter);
			Vector3 point2 = mouseRay.GetPoint(enter);
			_moveToTarget = SnapToGround(_panStartTarget + (_panStartPosition.Value - point2));
			_moveTimer = 0f;
			if (Vector3.Distance(_panStartPosition.Value, point2) > 1f && FollowCar != null)
			{
				FollowCar = null;
			}
		}
		else
		{
			_panStartPosition = null;
		}
		if (flag2)
		{
			_rotateStartPosition = Input.mousePosition;
			_rotateStarted = true;
		}
		if (Input.GetMouseButton(1) && _rotateStarted)
		{
			_rotateCurrentPosition = Input.mousePosition;
			Vector3 vector2 = _rotateStartPosition - _rotateCurrentPosition;
			_rotateStartPosition = _rotateCurrentPosition;
			int num = ((!Preferences.MouseLookInvert) ? 1 : (-1));
			_angleYInput = (0f - vector2.x) * 0.85f;
			_angleXInput = (float)num * vector2.y * 0.5f;
		}
		else
		{
			_rotateStarted = false;
		}
	}

	private void UpdateCameraPosition(bool immediate = false)
	{
		float fixedDeltaTime = Time.fixedDeltaTime;
		bool flag = FollowCar != null;
		_targetHeight = targetHeightFree;
		Vector3 vector = distanceToSpeed.Evaluate(_distance) * fixedDeltaTime * _movementInput;
		float t = fixedDeltaTime * 5f;
		vector.y = 0f;
		vector = base.transform.rotation.OnlyEulerY() * vector;
		_movementVelocity = Vector3.Lerp(_movementVelocity, vector, t);
		_targetPosition += 50f * fixedDeltaTime * _movementVelocity;
		if (_movementVelocity.magnitude > 0.001f)
		{
			_moveToTarget = null;
		}
		if (_moveToTarget.HasValue)
		{
			_moveTimer += fixedDeltaTime;
			_targetPosition = Vector3.Lerp(_targetPosition, _moveToTarget.Value, fixedDeltaTime * 10f);
			if (_moveTimer > 5f)
			{
				_targetPosition = _moveToTarget.Value;
				_moveToTarget = null;
			}
		}
		float extraRotationY = 0f;
		if (_movementVelocity.magnitude > 0.001f)
		{
			_targetPosition = SnapToGround(_targetPosition);
			FollowCar = null;
		}
		else if (flag)
		{
			Vector3 vector2 = _targetHeight * Vector3.up;
			(Vector3 position, Quaternion rotation) moverTargetPositionRotation = FollowCar.GetMoverTargetPositionRotation();
			Vector3 item = moverTargetPositionRotation.position;
			Quaternion item2 = moverTargetPositionRotation.rotation;
			_targetPosition = item + vector2;
			float y = (item2 * Quaternion.Inverse(_followCarInitialRotation)).eulerAngles.y;
			extraRotationY = ((Mathf.Abs(y - _extraRotationY) < 10f) ? Mathf.Lerp(_extraRotationY, y, t) : y);
		}
		_extraRotationY = extraRotationY;
		_distanceVelocity = Mathf.Lerp(_distanceVelocity, _distanceInput, t);
		_angleXVelocity = Mathf.Lerp(_angleXVelocity, _angleXInput, t);
		_angleYVelocity = Mathf.Lerp(_angleYVelocity, _angleYInput, t);
		_distance += _distanceVelocity * ZoomDelta * fixedDeltaTime;
		_angleX += _angleXVelocity * zoomAngleSpeed * fixedDeltaTime;
		_angleY += _angleYVelocity * zoomAngleYSpeed * fixedDeltaTime;
		_distance = Mathf.Clamp(_distance, 1f, 500f);
		_angleX = Mathf.Clamp(_angleX, -30f, 90f);
		CalculatePositionRotation(out var position, out var rotation);
		MoveAboveGround(ref position, ref rotation);
		SetPositionRotation(position, rotation, immediate);
	}

	private bool MoveAboveGround(ref Vector3 cameraPosition, ref Quaternion cameraRotation)
	{
		if (!FindGround(cameraPosition, out var groundPoint))
		{
			return false;
		}
		float num = Mathf.Lerp(0.25f, 2f, Mathf.InverseLerp(20f, 100f, _distance));
		float num2 = groundPoint.y + num;
		_extraHeightForGround = Mathf.Lerp(_extraHeightForGround, 0f, 5f * Time.deltaTime);
		if (cameraPosition.y < num2)
		{
			_extraHeightForGround = Mathf.Max(_extraHeightForGround, num2 - cameraPosition.y);
			cameraPosition.y += _extraHeightForGround;
		}
		else
		{
			if (_extraHeightForGround < 0.01f)
			{
				_extraHeightForGround = 0f;
				return false;
			}
			if (!(_extraHeightForGround > 0f))
			{
				return false;
			}
			cameraPosition.y += _extraHeightForGround;
		}
		cameraRotation = Quaternion.LookRotation(_targetPosition - cameraPosition, Vector3.up);
		return true;
	}

	private void CalculatePositionRotation(out Vector3 position, out Quaternion rotation)
	{
		Quaternion quaternion = Quaternion.Euler(_angleX, 0f, 0f);
		Quaternion quaternion2 = Quaternion.Euler(0f, _angleY + _extraRotationY, 0f);
		rotation = quaternion2 * quaternion;
		position = _targetPosition + rotation * (Vector3.back * _distance);
	}

	private void UpdatePosition(bool immediate)
	{
		CalculatePositionRotation(out var position, out var rotation);
		SetPositionRotation(position, rotation, immediate);
	}

	private void SetPositionRotation(Vector3 position, Quaternion rotation, bool immediate)
	{
		if (immediate)
		{
			Transform obj = base.transform;
			obj.position = position;
			obj.rotation = rotation;
		}
		else
		{
			_rigidbody.MovePosition(position);
			_rigidbody.MoveRotation(rotation);
		}
	}

	private Vector3 SnapToGround(Vector3 position, bool immediate = false)
	{
		if (!FindGround(position, out var groundPoint))
		{
			return position;
		}
		_targetSnapPosition = groundPoint + Vector3.up * _targetHeight;
		if (immediate || _targetSnapPosition.IsZero())
		{
			return _targetSnapPosition;
		}
		float t = 1f - Mathf.Pow(snapToGroundResponsiveness, Time.deltaTime);
		return Vector3.Lerp(position, _targetSnapPosition, t);
	}

	private static bool FindGround(Vector3 below, out Vector3 groundPoint)
	{
		int layerMask = (1 << Layers.Terrain) | (1 << Layers.Water) | (1 << Layers.Track);
		RaycastHit hitInfo;
		bool result = Physics.Raycast(Vector3.up * 2000f + below, Vector3.down, out hitInfo, 4000f, layerMask);
		groundPoint = hitInfo.point;
		return result;
	}

	private bool RayPointFromMouse(out Vector3 point)
	{
		if (Physics.Raycast(GetMouseRay(), out var hitInfo, _mainCamera.farClipPlane, 1 << Layers.Terrain))
		{
			point = hitInfo.point;
			return true;
		}
		point = Vector3.zero;
		return false;
	}

	private Ray GetMouseRay()
	{
		return _mainCamera.ScreenPointToRay(Input.mousePosition);
	}

	public void SetSelected(bool selected, Camera maybeCamera)
	{
		base.gameObject.SetActive(selected);
		if (selected)
		{
			maybeCamera.fieldOfView = 40f;
			_extraHeightForGround = 0f;
		}
	}

	public void JumpToCar(Car car)
	{
		FollowCar = car;
		_extraHeightForGround = 0f;
		UpdateCameraPosition(immediate: true);
	}

	public void JumpTo(Vector3 gamePosition, Quaternion? rotation = null)
	{
		FollowCar = null;
		_moveToTarget = null;
		_targetPosition = gamePosition.GameToWorld();
		_targetPosition = SnapToGround(_targetPosition, immediate: true);
		_extraHeightForGround = 0f;
		if (rotation.HasValue)
		{
			_angleY = rotation.Value.eulerAngles.y;
		}
		UpdatePosition(immediate: true);
	}

	GameObject ICameraSelectable.get_gameObject()
	{
		return base.gameObject;
	}
}
