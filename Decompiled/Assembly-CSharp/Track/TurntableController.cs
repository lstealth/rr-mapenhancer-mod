using System;
using Game.Messages;
using Game.State;
using KeyValue.Runtime;
using KinematicCharacterController;
using Model;
using RollingStock.ContinuousControls;
using RollingStock.Controls;
using Serilog;
using UnityEngine;

namespace Track;

[RequireComponent(typeof(KeyValueObject))]
[RequireComponent(typeof(GlobalKeyValueObject))]
public class TurntableController : MonoBehaviour, IMoverController
{
	private const string ControlLeverKey = "controlLever";

	public Turntable turntable;

	[SerializeField]
	private Transform bridgeTransform;

	[SerializeField]
	private ContinuousControl controlLever;

	private float _speed;

	private bool _wasMoving;

	private KeyValueObject _propertyObject;

	private IDisposable _controlLeverObserver;

	private Vector3 _goalPositionOffset;

	private Quaternion _goalRotation;

	private string _warnedCarId;

	private float TargetSpeedPercent
	{
		get
		{
			Value value = _propertyObject["controlLever"];
			if (value.Type == KeyValue.Runtime.ValueType.Null)
			{
				return 0f;
			}
			return Mathf.Lerp(-1f, 1f, value.FloatValue);
		}
	}

	private void Awake()
	{
		_propertyObject = GetComponent<KeyValueObject>();
		_goalPositionOffset = base.transform.InverseTransformPoint(bridgeTransform.position);
	}

	private void Start()
	{
		turntable.InitializeIfNeeded();
		bridgeTransform.gameObject.AddComponent<PhysicsMover>().MoverController = this;
		_goalRotation = base.transform.rotation * Quaternion.Euler(0f, turntable.Angle, 0f);
		controlLever.Value = Mathf.InverseLerp(-1f, 1f, TargetSpeedPercent);
		controlLever.ConfigureSnap(20);
		if (StateManager.IsHost)
		{
			base.gameObject.AddComponent<TurntableTransmitter>().turntable = turntable;
		}
		else
		{
			base.gameObject.AddComponent<TurntableReceiver>().turntableController = this;
		}
	}

	private void OnEnable()
	{
		controlLever.OnValueChanged += ControlLeverOnValueChanged;
		controlLever.tooltipText = TooltipTextForControlLever;
		_controlLeverObserver = _propertyObject.Observe("controlLever", ControlLeverPropertyDidChange);
	}

	private void OnDisable()
	{
		controlLever.OnValueChanged -= ControlLeverOnValueChanged;
		_controlLeverObserver?.Dispose();
	}

	private void FixedUpdate()
	{
		if (!StateManager.IsHost)
		{
			return;
		}
		_speed = Mathf.Lerp(_speed, TargetSpeedPercent * 10f, Time.deltaTime);
		bool flag = Mathf.Abs(_speed) > 0.5f;
		if (turntable.IsLined && flag)
		{
			if (turntable.TryGetCarBlockingMovement(out var car))
			{
				if (car.id != _warnedCarId)
				{
					Log.Warning("Stopped, found car blocking: {car}", car);
					_warnedCarId = car.id;
				}
				_speed = 0f;
				flag = false;
			}
			else
			{
				_warnedCarId = null;
			}
		}
		if (flag && !CanContinueMoving())
		{
			_speed = 0f;
			flag = false;
		}
		float num = turntable.Angle;
		float current = num;
		int? stopIndex = turntable.StopIndex;
		if (flag)
		{
			num += _speed * Time.deltaTime;
			num = Mathf.Repeat(num, 360f);
		}
		else if (stopIndex.HasValue)
		{
			num = Mathf.MoveTowardsAngle(num, turntable.AngleForIndex(stopIndex.Value), Time.deltaTime * 0.5f);
		}
		if (Mathf.Abs(Mathf.DeltaAngle(current, num)) > 0.001f)
		{
			SetAngle(num);
		}
		if (flag != _wasMoving)
		{
			_wasMoving = flag;
			turntable.UpdateSegmentIndex(flag);
		}
	}

	private void OnDrawGizmosSelected()
	{
		if (!(turntable == null))
		{
			Vector3 position = base.transform.position;
			_ = base.transform.forward;
			Gizmos.DrawRay(position, base.transform.up);
			Gizmos.color = (turntable.IsLined ? Color.green : Color.red);
			Quaternion quaternion = Quaternion.Euler(0f, turntable.Angle, 0f) * base.transform.rotation;
			Gizmos.DrawLine(position + quaternion * Vector3.forward * turntable.radius, position + quaternion * Vector3.back * turntable.radius);
		}
	}

	private void ControlLeverOnValueChanged(float value)
	{
		_propertyObject["controlLever"] = Value.Float(value);
	}

	private void ControlLeverPropertyDidChange(Value value)
	{
		controlLever.Value = value.FloatValue;
	}

	private string TooltipTextForControlLever()
	{
		int num = Mathf.RoundToInt(Mathf.Abs(TargetSpeedPercent) * 100f);
		if (num == 0)
		{
			return "Stop";
		}
		return $"{num}%";
	}

	private bool CanContinueMoving()
	{
		float num = Mathf.Sign(_speed);
		float remainder;
		int i = turntable.IndexAndRemainderForAngle(out remainder);
		int subdivisions = turntable.subdivisions;
		if ((int)num == (int)Mathf.Sign(remainder))
		{
			if (num > 0f)
			{
				i = (i + 1) % subdivisions;
			}
			else
			{
				for (i--; i < 0; i += subdivisions)
				{
				}
			}
		}
		if (!CanContinueMoving(TrackSegment.End.A, i))
		{
			return false;
		}
		int index = (i + subdivisions / 2) % subdivisions;
		if (!CanContinueMoving(TrackSegment.End.B, index))
		{
			return false;
		}
		return true;
	}

	private bool CanContinueMoving(TrackSegment.End end, int index)
	{
		Location location = turntable.BridgeLocation(end, 1f);
		TrainController shared = TrainController.Shared;
		Car car = shared.CheckForCarAtLocation(location);
		if (car == null)
		{
			return true;
		}
		Location? location2 = turntable.PitLocation(index, 1f);
		if (!location2.HasValue)
		{
			return true;
		}
		Location value = location2.Value;
		Car car2 = shared.CheckForCarAtLocation(value);
		if (!(car2 == null))
		{
			return car == car2;
		}
		return true;
	}

	public void SetAngle(float angle)
	{
		float angle2 = turntable.Angle;
		turntable.SetAngle(angle);
		float num = Mathf.Abs(angle - angle2);
		float num2 = Mathf.Abs(Mathf.DeltaAngle(angle, angle2));
		if (Mathf.Abs(num - num2) > 90f)
		{
			_goalRotation = base.transform.rotation * Quaternion.Euler(0f, angle - Mathf.DeltaAngle(angle2, angle), 0f);
		}
		_goalRotation = base.transform.rotation * Quaternion.Euler(0f, angle, 0f);
	}

	public void RestoreFromSnapshot(Snapshot.TurntableState state)
	{
		SetAngle(state.Angle);
		turntable.SetStopIndex(state.StopIndex);
	}

	public void UpdateMovement(out Vector3 goalPosition, out Quaternion goalRotation, float deltaTime)
	{
		goalPosition = base.transform.TransformPoint(_goalPositionOffset);
		goalRotation = _goalRotation;
	}
}
