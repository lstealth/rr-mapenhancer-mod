using System;
using System.Collections;
using Avatar;
using Cameras;
using Enviro;
using Helpers;
using JetBrains.Annotations;
using KinematicCharacterController;
using Map.Runtime;
using Model;
using RollingStock;
using Serilog;
using UI;
using UI.Common;
using UnityEngine;

namespace Character;

public class PlayerController : MonoBehaviour, ICameraSelectable
{
	public CharacterController character;

	public CharacterCameraController cameraController;

	private float _leanDownAt;

	private Lean _leanLastActive;

	private MouseLookInput _mouseLookInput;

	private PlayerCharacterInputs _characterInputs;

	private string _attachedCarId;

	private IDisposable _attachedCarLoadToken;

	private Coroutine _carCheckerCoroutine;

	[SerializeField]
	private Transform cameraContainer;

	private bool _isSelected;

	private CharacterPositionTransmitter _transmitter;

	public Transform CameraContainer => cameraContainer;

	public Vector3 GroundPosition => character.motor.TransientPosition;

	public bool IsOnGround
	{
		get
		{
			if (character.motor.GroundingStatus.IsStableOnGround)
			{
				return GetRelativeCar() == null;
			}
			return false;
		}
	}

	private void Awake()
	{
		CharacterController characterController = character;
		characterController.OnSeatDidChange = (Action)Delegate.Combine(characterController.OnSeatDidChange, new Action(SeatOrLadderDidChange));
		CharacterController characterController2 = character;
		characterController2.OnLadderDidChange = (Action)Delegate.Combine(characterController2.OnLadderDidChange, new Action(SeatOrLadderDidChange));
		_transmitter = base.gameObject.AddComponent<CharacterPositionTransmitter>();
		_mouseLookInput = base.gameObject.AddComponent<MouseLookInput>();
	}

	private void OnEnable()
	{
		_carCheckerCoroutine = StartCoroutine(AttachedCarChecker());
	}

	private void OnDisable()
	{
		if (_carCheckerCoroutine != null)
		{
			StopCoroutine(_carCheckerCoroutine);
			_carCheckerCoroutine = null;
		}
	}

	private void Update()
	{
		_mouseLookInput.UpdateInput(_isSelected);
		HandleCharacterInput();
	}

	private void LateUpdate()
	{
		if (GameInput.MovementInputEnabled && _isSelected)
		{
			float inputYaw = ((_characterInputs.Lean == Lean.Off) ? 0f : _mouseLookInput.Yaw);
			GameInput shared = GameInput.shared;
			float zoomDelta = shared.ZoomDelta;
			bool inputResetFOV = shared.InputResetFOV;
			cameraController.UpdateWithInput(Time.deltaTime, _mouseLookInput.Pitch, inputYaw, zoomDelta, inputResetFOV, _characterInputs.Lean);
			if (_transmitter != null)
			{
				_transmitter.SendIfConnected(GetPoseFromState());
			}
		}
	}

	private AvatarPose GetPoseFromState()
	{
		if (character.IsSeated)
		{
			return AvatarPose.Sit;
		}
		if (character.IsInAir)
		{
			return AvatarPose.Jump;
		}
		if (character.IsOnLadder)
		{
			return AvatarPose.Ladder;
		}
		return AvatarPose.Stand;
	}

	private Lean LeanInput(Lean current)
	{
		Lean lean = current;
		GameInput shared = GameInput.shared;
		Lean lean2 = (shared.LeanRight ? Lean.Right : (shared.LeanLeft ? Lean.Left : Lean.Off));
		float unscaledTime = Time.unscaledTime;
		bool num = lean2 != Lean.Off && _leanLastActive == Lean.Off;
		bool flag = lean2 == Lean.Off && _leanLastActive != Lean.Off;
		if (num)
		{
			lean = ((lean == Lean.Off && GetRelativeCar() != null) ? lean2 : Lean.Off);
			_leanDownAt = unscaledTime;
		}
		else if (flag && _leanDownAt + 0.25f < unscaledTime)
		{
			lean = Lean.Off;
		}
		_leanLastActive = lean2;
		return lean;
	}

	private void HandleCharacterInput()
	{
		PlayerCharacterInputs inputs = default(PlayerCharacterInputs);
		if (_isSelected)
		{
			GameInput shared = GameInput.shared;
			Vector2 moveVector = shared.MoveVector;
			float y = moveVector.y;
			float x = moveVector.x;
			Lean lean = LeanInput(character.Lean);
			if (Mathf.Abs(y) > 0.1f || Mathf.Abs(x) > 0.1f)
			{
				lean = Lean.Off;
			}
			inputs.MoveAxisForward = y;
			inputs.MoveAxisRight = x;
			inputs.CameraRotation = cameraContainer.transform.rotation;
			inputs.RotateAxisY = ((character.Lean == Lean.Off) ? _mouseLookInput.Yaw : 0f);
			inputs.JumpDown = shared.JumpDown;
			inputs.CrouchDown = shared.CrouchDown;
			inputs.CrouchUp = shared.CrouchUp;
			inputs.Lean = lean;
			inputs.Run = shared.ModifierRun;
		}
		bool num = _characterInputs.Lean != Lean.Off && inputs.Lean == Lean.Off;
		_characterInputs = inputs;
		if (num)
		{
			Vector3 position = cameraContainer.position;
			Quaternion rotation = cameraContainer.rotation;
			Quaternion rotation2 = Quaternion.LookRotation(Vector3.ProjectOnPlane(rotation * Vector3.forward, Vector3.up));
			Quaternion rotation3 = Quaternion.LookRotation(Vector3.ProjectOnPlane(rotation * Vector3.right, Vector3.forward));
			Quaternion transientRotation = character.motor.TransientRotation;
			Vector3 position2 = character.motor.transform.TransformPoint(Quaternion.Inverse(rotation2) * (transientRotation * character.motor.transform.InverseTransformPoint(position)));
			cameraContainer.position = position2;
			character.motor.SetRotation(rotation2);
			cameraContainer.rotation = rotation3;
		}
		character.SetInputs(ref inputs);
	}

	public void SetSelected(bool selected, Camera theCamera)
	{
		_mouseLookInput.SetMouseMovesCamera(mouseMovesCamera: false);
		_isSelected = selected;
		cameraController.SetSelected(selected, theCamera);
	}

	public void JumpToCar(Car car)
	{
		Car.MotionSnapshot motionSnapshot = car.GetMotionSnapshot();
		Vector3 position = motionSnapshot.Position + 3f * (motionSnapshot.Rotation * Vector3.left);
		Quaternion rotation = Quaternion.Euler(0f, 90f, 0f) * motionSnapshot.Rotation;
		character.UnsitUnladder();
		character.motor.SetPositionAndRotation(position, rotation);
	}

	public void JumpTo(Vector3 position, Quaternion rotation)
	{
		rotation = Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);
		character.UnsitUnladder();
		character.motor.SetPositionAndRotation(position, rotation);
		cameraController.SetRotation(rotation);
	}

	public void AttachTo(Car car, Vector3 worldPosition, Quaternion rotation)
	{
		PhysicsMover componentInChildren = car.GetComponentInChildren<PhysicsMover>();
		if (componentInChildren == null)
		{
			Log.Error("AttachTo: Couldn't find PhysicsMover");
			return;
		}
		Rigidbody rigidbody = componentInChildren.Rigidbody;
		if (rigidbody == null)
		{
			Log.Error("AttachTo: Couldn't find PhysicsMover.Rigidbody");
			return;
		}
		character.motor.ApplyState(new KinematicCharacterMotorState
		{
			AttachedRigidbody = rigidbody,
			AttachedRigidbodyVelocity = rigidbody.velocity,
			BaseVelocity = Vector3.zero,
			GroundingStatus = new CharacterTransientGroundingReport
			{
				FoundAnyGround = true,
				GroundNormal = Vector3.up,
				IsStableOnGround = true,
				InnerGroundNormal = Vector3.up,
				OuterGroundNormal = Vector3.up,
				SnappingPrevented = false
			},
			LastMovementIterationFoundAnyGround = false,
			MustUnground = false,
			MustUngroundTime = 0f,
			Position = worldPosition,
			Rotation = rotation
		});
	}

	public void Sit([CanBeNull] Seat seat)
	{
		character.Sit(seat, immediate: false);
	}

	private void SeatOrLadderDidChange()
	{
		CameraSelector.shared.localAvatar.SetSeat(character.IsSeated, character.IsOnLadder);
	}

	public void SetRotation(Quaternion rotation)
	{
		cameraController.SetRotation(Quaternion.LookRotation(Vector3.ProjectOnPlane(rotation * Vector3.right, Vector3.forward)));
		character.motor.SetRotation(Quaternion.LookRotation(Vector3.ProjectOnPlane(rotation * Vector3.forward, Vector3.up)));
	}

	public void WillDestroyCar(Car car)
	{
		if (IsOnCar(car))
		{
			character.CarWillBeDestroyed();
		}
	}

	public bool IsOnCar(Car car)
	{
		return GetRelativeCar() == car;
	}

	[CanBeNull]
	internal Car GetRelativeCar()
	{
		Rigidbody rigidbody = ((character.motor.AttachedRigidbodyOverride == null) ? character.motor.AttachedRigidbody : character.motor.AttachedRigidbodyOverride);
		if (rigidbody == null)
		{
			return null;
		}
		return rigidbody.GetComponentInParent<Car>();
	}

	public (MotionSnapshot, Car) GetRelativePositionRotation()
	{
		MotionSnapshot motionSnapshot = character.GetMotionSnapshot();
		Car relativeCar = GetRelativeCar();
		if (relativeCar != null)
		{
			Car.MotionSnapshot motionSnapshot2 = relativeCar.GetMotionSnapshot();
			Quaternion quaternion = Quaternion.Inverse(motionSnapshot2.Rotation);
			motionSnapshot.Position = quaternion * (motionSnapshot.Position - motionSnapshot2.Position);
			motionSnapshot.BodyRotation = quaternion * motionSnapshot.BodyRotation;
			motionSnapshot.LookRotation = quaternion * motionSnapshot.LookRotation;
			motionSnapshot.Velocity = character.motor.BaseVelocity;
		}
		return (motionSnapshot, relativeCar);
	}

	private IEnumerator AttachedCarChecker()
	{
		WaitForSeconds wait = new WaitForSeconds(0.5f);
		while (true)
		{
			try
			{
				Car relativeCar = GetRelativeCar();
				if (_attachedCarId != relativeCar?.id)
				{
					Log.Information("AttachedCarChecker: {attachedCarId} -> {carId}", _attachedCarId ?? "<null>", (relativeCar != null) ? relativeCar.id : "<null>");
					_attachedCarId = relativeCar?.id;
					_attachedCarLoadToken?.Dispose();
					_attachedCarLoadToken = ((relativeCar != null) ? relativeCar.ModelLoadRetain("Attached") : null);
				}
				if (_attachedCarId == null)
				{
					CheckForTerrainBelow();
					MapManager instance = MapManager.Instance;
					if (instance != null)
					{
						instance.KeepLoaded = instance.TilePositionFromPoint(WorldTransformer.WorldToGame(character.GetMotionSnapshot().Position));
					}
				}
			}
			catch (Exception exception)
			{
				Log.Error(exception, "Exception in AttachedCarChecker");
			}
			yield return wait;
		}
	}

	private void CheckForTerrainBelow()
	{
		int layerMask = (1 << Layers.Terrain) + (1 << Layers.Track) + (1 << Layers.Default);
		Vector3 transientPosition = character.motor.TransientPosition;
		if (Physics.Raycast(new Ray(transientPosition + Vector3.up * 1f, Vector3.down), 50f, layerMask) || TryFixPlayerPosition(transientPosition, transientPosition))
		{
			return;
		}
		MapManager instance = MapManager.Instance;
		if (instance != null)
		{
			Vector2Int tilePosition = instance.TilePositionFromPoint(WorldTransformer.WorldToGame(transientPosition));
			if (instance.HasTileData(tilePosition))
			{
				return;
			}
		}
		Vector3[] array = new Vector3[5]
		{
			transientPosition,
			transientPosition + Vector3.forward * 100f,
			transientPosition + Vector3.back * 100f,
			transientPosition + Vector3.left * 100f,
			transientPosition + Vector3.right * 100f
		};
		foreach (Vector3 potentialPosition in array)
		{
			if (TryFixPlayerPosition(transientPosition, potentialPosition))
			{
				return;
			}
		}
		if (instance != null)
		{
			Vector2Int vector2Int = instance.TilePositionFromPoint(WorldTransformer.WorldToGame(transientPosition));
			if (!instance.HasTileData(vector2Int))
			{
				Log.Warning("Fixing player position {characterPosition}; no terrain at {tilePosition}", transientPosition, vector2Int);
				FixPlayerPositionNoTileData(transientPosition);
				return;
			}
		}
		Log.Warning("Nothing below player at {characterPosition}; waiting.", transientPosition);
	}

	private bool TryFixPlayerPosition(Vector3 characterPosition, Vector3 potentialPosition)
	{
		if (!IsSuitablePositionForFixedPlayerPosition(potentialPosition, out var hitPoint))
		{
			return false;
		}
		Log.Warning("Fixing player position {characterPosition}; was below terrain {hitPoint}", characterPosition, hitPoint);
		Quaternion rotation = ((Vector3.Distance(characterPosition, potentialPosition) > 1f) ? Quaternion.LookRotation(hitPoint - characterPosition) : character.motor.TransientRotation);
		JumpTo(hitPoint, rotation);
		if (EnviroManager.instance != null)
		{
			EnviroManager.instance.Reflections.RenderGlobalReflectionProbe(forced: true);
		}
		return true;
	}

	private bool IsSuitablePositionForFixedPlayerPosition(Vector3 worldPosition, out Vector3 hitPoint)
	{
		int layerMask = (1 << Layers.Terrain) + (1 << Layers.Track);
		if (Physics.Raycast(new Ray(new Vector3(worldPosition.x, 2000f, worldPosition.z), Vector3.down), out var hitInfo, 4000f, layerMask))
		{
			hitPoint = hitInfo.point;
			return true;
		}
		hitPoint = Vector3.zero;
		return false;
	}

	private void FixPlayerPositionNoTileData(Vector3 characterPositionWorld)
	{
		Transform transform = SpawnPoint.ClosestTo(characterPositionWorld).transform;
		JumpTo(transform.position, transform.rotation);
		Toast.Present("Back you go.");
	}

	GameObject ICameraSelectable.get_gameObject()
	{
		return base.gameObject;
	}
}
