using KinematicCharacterController;
using UnityEngine;

namespace Model;

public class CarMover : IMoverController
{
	private PhysicsMover _physicsMover;

	private Rigidbody _rigidbody;

	private Vector3 _moverPosition;

	private Quaternion _moverRotation = Quaternion.identity;

	private Vector3 _velocity;

	private float _timeLastMoved;

	private bool _physicsMoverEnabled;

	private bool _playerNearby;

	private bool _movedRecently;

	private Transform _bodyTransform;

	public Vector3 Position => _moverPosition;

	public Quaternion Rotation => _moverRotation;

	public string DebugId { get; set; }

	private Vector3 RigidbodyMoverPosition => _moverPosition - _velocity * Time.fixedDeltaTime;

	public void ConfigureForBody(GameObject body)
	{
		_bodyTransform = body.transform;
		_rigidbody = body.AddComponent<Rigidbody>();
		_physicsMover = body.AddComponent<PhysicsMover>();
		_physicsMover.ForceAwake();
		_physicsMoverEnabled = true;
		_physicsMover.MoverController = this;
		_timeLastMoved = Time.time;
		UpdatePhysicsMoverEnabled();
		ApplyMoverPosition(immediate: true);
	}

	public void ClearBody()
	{
		_bodyTransform = null;
		if (_physicsMover != null)
		{
			_physicsMover.MoverController = null;
			Object.Destroy(_physicsMover);
			_physicsMover = null;
			_physicsMoverEnabled = false;
		}
		if (_rigidbody != null)
		{
			Object.Destroy(_rigidbody);
			_rigidbody = null;
		}
	}

	public void Move(Vector3 worldPosition, Quaternion rotation, bool immediate)
	{
		CheckToAwakenMover(Vector3.Distance(worldPosition, (_physicsMover == null) ? _moverPosition : _physicsMover.TransientPosition));
		if (immediate)
		{
			_velocity = Vector3.zero;
		}
		else
		{
			_velocity = (worldPosition - _moverPosition) / Time.fixedDeltaTime;
		}
		_moverPosition = worldPosition;
		_moverRotation = rotation;
		ApplyMoverPosition(immediate);
	}

	private void ApplyMoverPosition(bool immediate)
	{
		if (immediate)
		{
			if (_physicsMoverEnabled)
			{
				_physicsMover.SetPositionAndRotation(_moverPosition, _moverRotation);
			}
			else if (_bodyTransform != null)
			{
				_bodyTransform.SetPositionAndRotation(_moverPosition, _moverRotation);
			}
		}
		else if (!_physicsMoverEnabled)
		{
			if (_rigidbody != null)
			{
				_rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
				_rigidbody.Move(RigidbodyMoverPosition, _moverRotation);
			}
			else if (_bodyTransform != null)
			{
				_bodyTransform.SetPositionAndRotation(_moverPosition, _moverRotation);
			}
		}
	}

	public void SetPlayerNearby(bool playerNearby)
	{
		_playerNearby = playerNearby;
		UpdatePhysicsMoverEnabled();
	}

	public void UpdateMovement(out Vector3 goalPosition, out Quaternion goalRotation, float deltaTime)
	{
		goalPosition = _moverPosition;
		goalRotation = _moverRotation;
	}

	private void UpdatePhysicsMoverEnabled()
	{
		SetPhysicsMoverEnabled(_movedRecently && _playerNearby);
	}

	private void SetPhysicsMoverEnabled(bool physicsMoverEnable)
	{
		if (_physicsMoverEnabled == physicsMoverEnable || _physicsMover == null)
		{
			return;
		}
		if (physicsMoverEnable)
		{
			_physicsMover.enabled = true;
			_physicsMover.Rigidbody = _rigidbody;
			_physicsMover.InitialTickRotation = _moverRotation;
			_physicsMover.SetRotation(_moverRotation);
			SetPhysicsMoverPositionSeamless(_moverPosition);
		}
		else
		{
			_physicsMover.enabled = false;
			if (_rigidbody != null)
			{
				_rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
				_rigidbody.MovePosition(RigidbodyMoverPosition);
				_rigidbody.MoveRotation(_moverRotation);
			}
		}
		_physicsMoverEnabled = physicsMoverEnable;
	}

	private void CheckToAwakenMover(float distanceMoved)
	{
		if (!(distanceMoved < 0.001f))
		{
			_timeLastMoved = Time.time;
			_movedRecently = true;
			UpdatePhysicsMoverEnabled();
		}
	}

	public void CheckForSleepyMover()
	{
		if (Time.time - _timeLastMoved > 1f && _physicsMoverEnabled && !(_physicsMover == null))
		{
			_movedRecently = false;
			UpdatePhysicsMoverEnabled();
		}
	}

	public void WorldDidMove(Vector3 offset)
	{
		_moverPosition += offset;
		if (_physicsMoverEnabled)
		{
			_physicsMover.OffsetSeamless(offset);
		}
		else if (_bodyTransform != null)
		{
			_bodyTransform.position = _moverPosition;
		}
	}

	private void SetPhysicsMoverPositionSeamless(Vector3 newPosition)
	{
		Vector3 initialTickPosition = newPosition - _velocity * Time.fixedDeltaTime;
		_physicsMover.InitialTickPosition = initialTickPosition;
		_physicsMover.SetPosition(newPosition);
		_physicsMover.VelocityUpdate(Time.fixedDeltaTime);
		_physicsMover.SetPosition(newPosition);
	}

	public Car.MotionSnapshot GetMotionSnapshot()
	{
		if (_physicsMoverEnabled && _physicsMover != null)
		{
			return new Car.MotionSnapshot(_physicsMover.TransientPosition, _physicsMover.TransientRotation, _physicsMover.Velocity);
		}
		return new Car.MotionSnapshot(_moverPosition, _moverRotation, Vector3.zero);
	}
}
