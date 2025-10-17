using System;
using System.Collections.Generic;
using Core;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Helpers;
using JetBrains.Annotations;
using KinematicCharacterController;
using Model;
using RollingStock;
using Serilog;
using UnityEngine;

namespace Character;

public class CharacterController : MonoBehaviour, ICharacterController
{
	private enum AttachState
	{
		Anchoring,
		Stable,
		Deanchoring
	}

	public KinematicCharacterMotor motor;

	[Header("Stable Movement")]
	public float maxStableMoveSpeed = 10f;

	[Tooltip("How quickly does the character accelerate? 15 is snappy, 1 is slower.")]
	public float stableMovementAccelSharpness = 1f;

	[Tooltip("How quickly does the character decelerate? 15 is snappy, 1 is slower.")]
	public float stableMovementDecelSharpness = 15f;

	public float orientationSharpness = 10f;

	public float runMoveSpeed = 40f;

	[Header("Air Movement")]
	public float maxAirMoveSpeed = 15f;

	public float airAccelerationSpeed = 15f;

	public float drag = 0.1f;

	[Header("Jumping")]
	public bool allowJumpingWhenSliding;

	public float jumpUpSpeed = 10f;

	public float jumpScalableForwardSpeed = 10f;

	public float jumpPreGroundingGraceTime;

	public float jumpPostGroundingGraceTime;

	[Header("Ladders")]
	public float ladderSpeedNormal = 2f;

	public float ladderSpeedFast = 4f;

	[Header("Misc")]
	public List<Collider> ignoredColliders = new List<Collider>();

	public Vector3 gravity = new Vector3(0f, -30f, 0f);

	public Transform cameraContainer;

	public float crouchedCapsuleHeight = 1f;

	public float maintainUpDegreesPerSecond = 0.001f;

	private Collider[] _probedColliders = new Collider[8];

	private RaycastHit[] _probedHits = new RaycastHit[8];

	private Vector3 _moveInputVector;

	private Quaternion _lookInputRotation;

	private bool _jumpRequested;

	private bool _jumpConsumed;

	private bool _jumpedThisFrame;

	private bool _jumpedFromLadder;

	private float _timeSinceJumpRequested = float.PositiveInfinity;

	private float _timeSinceLastAbleToJump;

	private Vector3 _internalVelocityAdd = Vector3.zero;

	private bool _shouldBeCrouching;

	private bool _isCrouching;

	private bool _inputRun;

	private Vector3 lastInnerNormal = Vector3.zero;

	private Vector3 lastOuterNormal = Vector3.zero;

	private float _initialCapsuleRadius;

	private float _initialCapsuleHeight;

	private Quaternion _tmpTransientRot;

	private AttachState _attachState = AttachState.Stable;

	private float _anchoringTimer;

	private const float AnchoringDuration = 0.15f;

	public float eyeHeightStanding = 1.55f;

	public float eyeHeightSeated = 1.25f;

	private Seat _seat;

	private float _seatStickyRemaining;

	private Ladder _ladder;

	private Vector3 _ladderLocalPosition;

	private float _ladderDuration;

	[Range(0.25f, 1.25f)]
	public float leanDistance = 0.5f;

	private Lean? _leanLast;

	private bool _cameraSeated;

	private int _cameraContainerMoveId;

	public Action OnSeatDidChange;

	public Action OnLadderDidChange;

	private bool _hasAttachedRigidbody;

	public CharacterState CurrentCharacterState { get; private set; }

	private float AnchoringParameter => _anchoringTimer / 0.15f;

	public Lean Lean { get; private set; }

	public Seat Seat => _seat;

	public bool IsSeated => _seat != null;

	public bool IsOnLadder => _ladder != null;

	private float LadderStickyRemaining => 0.25f - _ladderDuration;

	public bool IsInAir
	{
		get
		{
			switch (CurrentCharacterState)
			{
			case CharacterState.Default:
				if (motor.GroundingStatus.IsStableOnGround)
				{
					return false;
				}
				if (motor.AttachedRigidbodyOverride != null || motor.AttachedRigidbody != null)
				{
					return false;
				}
				return true;
			case CharacterState.Seated:
			case CharacterState.Ladder:
				return false;
			default:
				throw new ArgumentOutOfRangeException();
			}
		}
	}

	private void Awake()
	{
		TransitionToState(CharacterState.Default);
		motor.CharacterController = this;
	}

	private void Start()
	{
		_initialCapsuleHeight = motor.Capsule.height;
		_initialCapsuleRadius = motor.Capsule.radius;
		Debug.Log($"Character Capsule initial values: height = {_initialCapsuleHeight:F3}, radius = {_initialCapsuleRadius:F3}");
	}

	private void OnEnable()
	{
		Messenger.Default.Register<WorldDidMoveEvent>(this, WorldDidMove);
	}

	private void OnDisable()
	{
		Messenger.Default.Unregister(this);
	}

	private void WorldDidMove(WorldDidMoveEvent evt)
	{
		motor.OffsetCharacter(evt.Offset);
	}

	private void SetCrouched(bool crouched)
	{
		if (crouched)
		{
			motor.SetCapsuleDimensions(_initialCapsuleRadius, crouchedCapsuleHeight, crouchedCapsuleHeight * 0.5f);
		}
		else
		{
			motor.SetCapsuleDimensions(_initialCapsuleRadius, _initialCapsuleHeight, _initialCapsuleHeight * 0.5f);
		}
	}

	private void TransitionToState(CharacterState newState)
	{
		if (newState != CurrentCharacterState)
		{
			Log.Debug("TransitionToState: {currentState} -> {newState}", CurrentCharacterState, newState);
			CharacterState currentCharacterState = CurrentCharacterState;
			OnStateExit(currentCharacterState, newState);
			CurrentCharacterState = newState;
			OnStateEnter(newState, currentCharacterState);
		}
	}

	private void OnStateEnter(CharacterState state, CharacterState fromState)
	{
		switch (state)
		{
		case CharacterState.Default:
			motor.SetMovementCollisionsSolvingActivation(movementCollisionsSolvingActive: true);
			motor.SetGroundSolvingActivation(stabilitySolvingActive: true);
			break;
		case CharacterState.Seated:
			_attachState = AttachState.Anchoring;
			_anchoringTimer = 0f;
			_seatStickyRemaining = 0.75f;
			motor.SetMovementCollisionsSolvingActivation(movementCollisionsSolvingActive: false);
			SetSolvingActivated(active: false);
			break;
		case CharacterState.Ladder:
			_attachState = AttachState.Anchoring;
			_anchoringTimer = 0f;
			_ladderDuration = 0f;
			_ladderLocalPosition = _ladder.transform.InverseTransformPoint(_ladder.ClosestPointTo(motor.TransientPosition));
			_ladderLocalPosition += Vector3.forward * motor.Capsule.radius;
			SetSolvingActivated(active: false);
			break;
		}
	}

	private void OnStateExit(CharacterState state, CharacterState toState)
	{
		switch (state)
		{
		case CharacterState.Seated:
			SetCameraSeated(seatedParameter: false);
			motor.AttachedRigidbodyOverride = null;
			SetSolvingActivated(active: true);
			_seat = null;
			OnSeatDidChange?.Invoke();
			break;
		case CharacterState.Ladder:
			motor.AttachedRigidbodyOverride = null;
			SetSolvingActivated(active: true);
			_ladder = null;
			OnLadderDidChange?.Invoke();
			break;
		case CharacterState.Default:
			break;
		}
	}

	private Collider ProbeForCollider(LayerMask layerMask)
	{
		if (motor.CharacterOverlap(motor.TransientPosition, motor.TransientRotation, _probedColliders, layerMask, QueryTriggerInteraction.Collide) != 0)
		{
			return _probedColliders[0];
		}
		return null;
	}

	private bool IsMovingInto(Collider target, Vector3 moveInputVector, bool selective = false)
	{
		if (moveInputVector.sqrMagnitude < 0.1f)
		{
			return false;
		}
		if (selective && Vector3.Dot(target.transform.forward, moveInputVector.normalized) > -0.5f)
		{
			return false;
		}
		(Vector3, Vector3, float) capsuleSphereLineSegment = target.GetCapsuleSphereLineSegment();
		Vector3 linePoint0 = capsuleSphereLineSegment.Item1;
		Vector3 linePoint1 = capsuleSphereLineSegment.Item2;
		float colliderRadius = capsuleSphereLineSegment.Item3;
		moveInputVector = moveInputVector.normalized * colliderRadius;
		Vector3 transientPosition = motor.TransientPosition;
		if (!CheckPosition(transientPosition))
		{
			return CheckPosition(transientPosition + Vector3.up * eyeHeightStanding);
		}
		return true;
		bool CheckPosition(Vector3 position)
		{
			Vector3 vector = position + moveInputVector;
			LineSegment.Intersects(linePoint0, linePoint1, position, vector, out var point, colliderRadius);
			if (Vector3.Distance(point, position) < colliderRadius / 2f)
			{
				return false;
			}
			return Vector3.Distance(point, vector) < colliderRadius;
		}
	}

	private static (float radius, Vector3 startPos, Vector3 endPos) GetColliderVolume(Collider coll)
	{
		if (!(coll is CapsuleCollider capsuleCollider))
		{
			if (coll is SphereCollider sphereCollider)
			{
				Vector3 vector = sphereCollider.transform.position + sphereCollider.center;
				return (radius: sphereCollider.radius, startPos: vector + Vector3.down, endPos: vector + Vector3.up);
			}
			throw new ArgumentException($"Collider not supported: {coll}");
		}
		var (item, item2) = capsuleCollider.StartEndPoints();
		return (radius: capsuleCollider.radius, startPos: item, endPos: item2);
	}

	public void SetInputs(ref PlayerCharacterInputs inputs)
	{
		Vector3 vector = Vector3.ClampMagnitude(new Vector3(inputs.MoveAxisRight, 0f, inputs.MoveAxisForward), 1f);
		Quaternion cameraRotation = inputs.CameraRotation;
		Vector3 normalized = Vector3.ProjectOnPlane(cameraRotation * Vector3.forward, motor.CharacterUp).normalized;
		if (normalized.sqrMagnitude == 0f)
		{
			normalized = Vector3.ProjectOnPlane(cameraRotation * Vector3.up, motor.CharacterUp).normalized;
		}
		Quaternion quaternion = Quaternion.LookRotation(normalized, motor.CharacterUp);
		_moveInputVector = quaternion * vector;
		_lookInputRotation = Quaternion.Euler(0f, inputs.RotateAxisY, 0f);
		if (inputs.JumpDown)
		{
			_jumpedFromLadder = CurrentCharacterState == CharacterState.Ladder;
			TransitionToState(CharacterState.Default);
			_timeSinceJumpRequested = 0f;
			_jumpRequested = true;
			if (_jumpedFromLadder)
			{
				_jumpConsumed = false;
				_timeSinceLastAbleToJump = 0f;
			}
		}
		Lean = inputs.Lean;
		_inputRun = inputs.Run;
		switch (CurrentCharacterState)
		{
		case CharacterState.Default:
			if (CheckForLadderOrSeat())
			{
				break;
			}
			if (inputs.CrouchDown)
			{
				_shouldBeCrouching = true;
				if (!_isCrouching)
				{
					_isCrouching = true;
					SetCrouched(crouched: true);
				}
			}
			else if (inputs.CrouchUp)
			{
				_shouldBeCrouching = false;
			}
			break;
		case CharacterState.Seated:
			if (_attachState == AttachState.Stable && vector.sqrMagnitude > 0.001f && _seatStickyRemaining <= 0f)
			{
				_attachState = AttachState.Deanchoring;
				_anchoringTimer = 0f;
			}
			break;
		case CharacterState.Ladder:
			if (_moveInputVector.sqrMagnitude < 0.1f)
			{
				break;
			}
			if (IsMovingInto(_ladder.CapsuleCollider, _moveInputVector, selective: true))
			{
				float x = cameraContainer.transform.localRotation.eulerAngles.x;
				float b = (_inputRun ? ladderSpeedFast : ladderSpeedNormal);
				float num = Mathf.Lerp(0f, b, Mathf.InverseLerp(0f, 1f, _ladderDuration));
				num *= _ladder.SpeedMultiplierForPosition(_ladderLocalPosition);
				Vector3 vector2 = _ladder.transform.up * (Time.fixedDeltaTime * num);
				if (Mathf.Abs(Mathf.DeltaAngle(x, 270f)) < 140f)
				{
					_ladderLocalPosition += vector2;
					if (!_ladder.CheckPositionValid(_ladderLocalPosition, ascending: true))
					{
						TransitionToState(CharacterState.Default);
						Vector3 velocity = motor.TransientRotation * Config.Shared.ladderExitBump;
						AddVelocity(velocity);
					}
				}
				else
				{
					_ladderLocalPosition -= vector2;
					if (!_ladder.CheckPositionValid(_ladderLocalPosition, ascending: false))
					{
						TransitionToState(CharacterState.Default);
					}
				}
			}
			else if (LadderStickyRemaining <= 0f)
			{
				TransitionToState(CharacterState.Default);
			}
			break;
		}
	}

	private bool CheckForLadderOrSeat()
	{
		if (_jumpedFromLadder && _timeSinceJumpRequested < 0.25f)
		{
			return false;
		}
		Collider collider = ProbeForCollider(1 << Layers.Ladder);
		if (collider == null)
		{
			return false;
		}
		Ladder component = collider.gameObject.GetComponent<Ladder>();
		if (component != null && IsMovingInto(collider, _moveInputVector, selective: true))
		{
			GrabLadder(component, immediate: false);
			return true;
		}
		Seat component2 = collider.gameObject.GetComponent<Seat>();
		if (component2 != null && IsMovingInto(collider, _moveInputVector))
		{
			Sit(component2, immediate: false);
			return true;
		}
		return false;
	}

	public void BeforeCharacterUpdate(float deltaTime)
	{
	}

	public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
	{
		currentRotation *= _lookInputRotation;
		Quaternion to = Quaternion.LookRotation(currentRotation * Vector3.forward, Vector3.up);
		currentRotation = Quaternion.RotateTowards(currentRotation, to, maintainUpDegreesPerSecond * deltaTime);
	}

	public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
	{
		if (this == null)
		{
			Debug.LogWarning("UpdateVelocity called on null CharacterController");
			return;
		}
		if (motor == null)
		{
			Debug.LogWarning("UpdateVelocity called on CharacterController with null motor");
			return;
		}
		switch (CurrentCharacterState)
		{
		case CharacterState.Default:
		{
			bool flag = motor.AttachedRigidbody != null;
			if (flag != _hasAttachedRigidbody)
			{
				_hasAttachedRigidbody = flag;
			}
			if (motor.GroundingStatus.IsStableOnGround)
			{
				float magnitude = currentVelocity.magnitude;
				Vector3 groundNormal = motor.GroundingStatus.GroundNormal;
				currentVelocity = motor.GetDirectionTangentToSurface(currentVelocity, groundNormal) * magnitude;
				float num = _moveInputVector.magnitude * (_inputRun ? runMoveSpeed : maxStableMoveSpeed);
				float magnitude2 = currentVelocity.magnitude;
				float num2 = ((num > magnitude2) ? stableMovementAccelSharpness : stableMovementDecelSharpness);
				Vector3 rhs = Vector3.Cross(_moveInputVector, motor.CharacterUp);
				Vector3 b2 = Vector3.Cross(groundNormal, rhs).normalized * _moveInputVector.magnitude * num;
				currentVelocity = Vector3.Lerp(currentVelocity, b2, 1f - Mathf.Exp((0f - num2) * deltaTime));
			}
			else
			{
				if (_moveInputVector.sqrMagnitude > 0f)
				{
					Vector3 vector = _moveInputVector * airAccelerationSpeed * deltaTime;
					Vector3 vector2 = Vector3.ProjectOnPlane(currentVelocity, motor.CharacterUp);
					if (vector2.magnitude < maxAirMoveSpeed)
					{
						vector = Vector3.ClampMagnitude(vector2 + vector, maxAirMoveSpeed) - vector2;
					}
					else if (Vector3.Dot(vector2, vector) > 0f)
					{
						vector = Vector3.ProjectOnPlane(vector, vector2.normalized);
					}
					if (motor.GroundingStatus.FoundAnyGround && Vector3.Dot(currentVelocity + vector, vector) > 0f)
					{
						Vector3 normalized = Vector3.Cross(Vector3.Cross(motor.CharacterUp, motor.GroundingStatus.GroundNormal), motor.CharacterUp).normalized;
						vector = Vector3.ProjectOnPlane(vector, normalized);
					}
					currentVelocity += vector;
				}
				currentVelocity += gravity * deltaTime;
				currentVelocity *= 1f / (1f + drag * deltaTime);
			}
			_jumpedThisFrame = false;
			_timeSinceJumpRequested += deltaTime;
			if (_jumpRequested && !_jumpConsumed && ((allowJumpingWhenSliding ? motor.GroundingStatus.FoundAnyGround : motor.GroundingStatus.IsStableOnGround) || _timeSinceLastAbleToJump <= jumpPostGroundingGraceTime))
			{
				Vector3 vector3 = motor.CharacterUp;
				if (motor.GroundingStatus.FoundAnyGround && !motor.GroundingStatus.IsStableOnGround)
				{
					vector3 = motor.GroundingStatus.GroundNormal;
				}
				motor.ForceUnground();
				currentVelocity += vector3 * jumpUpSpeed - Vector3.Project(currentVelocity, motor.CharacterUp);
				currentVelocity += _moveInputVector * jumpScalableForwardSpeed;
				_jumpRequested = false;
				_jumpConsumed = true;
				_jumpedThisFrame = true;
			}
			if (_internalVelocityAdd.sqrMagnitude > 0f)
			{
				currentVelocity += _internalVelocityAdd;
				_internalVelocityAdd = Vector3.zero;
			}
			break;
		}
		case CharacterState.Seated:
			if (_seat == null)
			{
				Log.Error("State is Seated but seat is null -- AttachedCarChecker bug?");
				TransitionToState(CharacterState.Default);
				break;
			}
			switch (_attachState)
			{
			case AttachState.Stable:
				currentVelocity = motor.GetVelocityForMovePosition(motor.TransientPosition, _seat.FootPosition, deltaTime);
				break;
			case AttachState.Anchoring:
			{
				Vector3 footPosition = _seat.FootPosition;
				Vector3 toPosition2 = Vector3.Lerp(motor.TransientPosition, footPosition, AnchoringParameter);
				currentVelocity = motor.GetVelocityForMovePosition(motor.TransientPosition, toPosition2, deltaTime);
				break;
			}
			case AttachState.Deanchoring:
				break;
			}
			break;
		case CharacterState.Ladder:
			if (!(_ladder == null))
			{
				Vector3 b = _ladder.transform.TransformPoint(_ladderLocalPosition);
				Vector3 toPosition = Vector3.Lerp(motor.TransientPosition, b, deltaTime * 20f);
				currentVelocity = motor.GetVelocityForMovePosition(motor.TransientPosition, toPosition, deltaTime);
			}
			break;
		}
	}

	public void AfterCharacterUpdate(float deltaTime)
	{
		switch (CurrentCharacterState)
		{
		case CharacterState.Default:
			if (_jumpRequested && _timeSinceJumpRequested > jumpPreGroundingGraceTime)
			{
				_jumpRequested = false;
			}
			if (allowJumpingWhenSliding ? motor.GroundingStatus.FoundAnyGround : motor.GroundingStatus.IsStableOnGround)
			{
				if (!_jumpedThisFrame)
				{
					_jumpConsumed = false;
				}
				_timeSinceLastAbleToJump = 0f;
			}
			else
			{
				_timeSinceLastAbleToJump += deltaTime;
			}
			if (_isCrouching && !_shouldBeCrouching)
			{
				SetCrouched(crouched: false);
				if (motor.CharacterOverlap(motor.TransientPosition, motor.TransientRotation, _probedColliders, motor.CollidableLayers, QueryTriggerInteraction.Ignore) > 0)
				{
					SetCrouched(crouched: true);
				}
				else
				{
					_isCrouching = false;
				}
			}
			break;
		case CharacterState.Seated:
		{
			bool flag = _anchoringTimer >= 0.15f;
			switch (_attachState)
			{
			case AttachState.Anchoring:
				if (flag)
				{
					motor.SetMovementCollisionsSolvingActivation(movementCollisionsSolvingActive: true);
					_attachState = AttachState.Stable;
				}
				else
				{
					_anchoringTimer += deltaTime;
					SetCameraSeated(seatedParameter: true);
				}
				break;
			case AttachState.Deanchoring:
				if (flag)
				{
					TransitionToState(CharacterState.Default);
					break;
				}
				_anchoringTimer += deltaTime;
				SetCameraSeated(seatedParameter: false);
				break;
			}
			_seatStickyRemaining -= deltaTime;
			break;
		}
		case CharacterState.Ladder:
			_ladderDuration += deltaTime;
			break;
		}
		if (Lean != _leanLast)
		{
			AnimateCameraContainerPosition();
			_leanLast = Lean;
		}
	}

	private void SetCameraSeated(bool seatedParameter)
	{
		if (_cameraSeated != seatedParameter)
		{
			_cameraSeated = seatedParameter;
			AnimateCameraContainerPosition();
		}
	}

	private void AnimateCameraContainerPosition()
	{
		Vector3 vector = TargetCameraContainerLocalPosition();
		float num = Vector3.Distance(vector, cameraContainer.transform.localPosition);
		Config shared = Config.Shared;
		float time = shared.characterEasingSpeed * num;
		AnimationCurve characterEasing = shared.characterEasing;
		LeanTween.cancel(_cameraContainerMoveId);
		_cameraContainerMoveId = LeanTween.moveLocal(cameraContainer.gameObject, vector, time).setEase(characterEasing).id;
	}

	private Vector3 TargetCameraContainerLocalPosition()
	{
		return new Vector3(Lean switch
		{
			Lean.Off => 0f, 
			Lean.Left => 0f - leanDistance, 
			Lean.Right => leanDistance, 
			_ => 0f, 
		}, _cameraSeated ? eyeHeightSeated : eyeHeightStanding, 0f);
	}

	public void PostGroundingUpdate(float deltaTime)
	{
		if (motor.GroundingStatus.IsStableOnGround && !motor.LastGroundingStatus.IsStableOnGround)
		{
			OnLanded();
		}
		else if (!motor.GroundingStatus.IsStableOnGround && motor.LastGroundingStatus.IsStableOnGround)
		{
			OnLeaveStableGround();
		}
	}

	public bool IsColliderValidForCollisions(Collider coll)
	{
		if (ignoredColliders.Count == 0)
		{
			return true;
		}
		if (ignoredColliders.Contains(coll))
		{
			return false;
		}
		return true;
	}

	public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
	{
	}

	public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
	{
	}

	private void AddVelocity(Vector3 velocity)
	{
		if (CurrentCharacterState == CharacterState.Default)
		{
			_internalVelocityAdd += velocity;
		}
	}

	public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
	{
	}

	protected void OnLanded()
	{
	}

	protected void OnLeaveStableGround()
	{
	}

	public void OnDiscreteCollisionDetected(Collider hitCollider)
	{
	}

	public void Sit([CanBeNull] Seat seat, bool immediate)
	{
		if (seat != null)
		{
			GrabLadder(null, immediate: true);
		}
		if (_seat != null)
		{
			TransitionToState(CharacterState.Default);
		}
		_seat = seat;
		if (_seat != null)
		{
			motor.AttachedRigidbodyOverride = _seat.GetComponentInParent<PhysicsMover>()?.Rigidbody;
			TransitionToState(CharacterState.Seated);
			OnSeatDidChange?.Invoke();
		}
	}

	private void GrabLadder([CanBeNull] Ladder ladder, bool immediate)
	{
		if (ladder != null)
		{
			Sit(null, immediate: true);
		}
		if (_ladder != null)
		{
			TransitionToState(CharacterState.Default);
		}
		_ladder = ladder;
		if (_ladder != null)
		{
			motor.AttachedRigidbodyOverride = _ladder.GetComponentInParent<PhysicsMover>()?.Rigidbody;
			TransitionToState(CharacterState.Ladder);
			OnLadderDidChange?.Invoke();
		}
	}

	private void SetSolvingActivated(bool active)
	{
		motor.SetGroundSolvingActivation(active);
	}

	public MotionSnapshot GetMotionSnapshot()
	{
		Quaternion transientRotation = motor.TransientRotation;
		Quaternion bodyRotation = transientRotation;
		if (_ladder != null)
		{
			bodyRotation = Quaternion.Euler(0f, 180f, 0f) * _ladder.transform.rotation;
		}
		if (_seat != null)
		{
			bodyRotation = _seat.transform.rotation;
		}
		return new MotionSnapshot(motor.TransientPosition, bodyRotation, transientRotation, motor.Velocity);
	}

	public void CarWillBeDestroyed()
	{
		UnsitUnladder();
	}

	public void UnsitUnladder()
	{
		Sit(null, immediate: true);
		GrabLadder(null, immediate: true);
	}
}
