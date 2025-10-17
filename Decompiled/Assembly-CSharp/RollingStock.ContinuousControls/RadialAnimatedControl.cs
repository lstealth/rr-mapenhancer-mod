using System;
using System.Collections;
using Helpers;
using Helpers.Animation;
using Model.Definition.Components;
using UI;
using UnityEngine;

namespace RollingStock.ContinuousControls;

[RequireComponent(typeof(Collider))]
public class RadialAnimatedControl : ContinuousControl
{
	public enum Axis
	{
		X,
		Y,
		Z
	}

	public AnimationClip animationClip;

	public Animator animator;

	public Axis rotationAxis = Axis.Y;

	public Axis handleAxis = Axis.Z;

	[Range(-359f, 359f)]
	public float rotationStart;

	[Range(-180f, 180f)]
	public float rotationExtent = 90f;

	public float radius = 1f;

	[Tooltip("Maintain a zero state by default.")]
	public bool momentary;

	[Tooltip("Shift-clicking causes the control to toggle between 0 and 1, or the nearest value if in between.")]
	public bool shiftActivateToggles;

	[Tooltip("If momentary, the value that the control returns to upon release.")]
	public float homePosition;

	private PlayableHandle _clipPlayable;

	private Vector3 _mouseDownAngleVector;

	private float _mouseDownAngle;

	private float _mouseDownValue;

	private float _animationValue;

	private bool _useAngleManipulation;

	private Vector3 _cameraLocalControlPosition;

	private Vector3 _cameraLocalControlUp;

	private Vector3 _cameraLocalControlHandle;

	private float _activatedAt;

	private float _deactivatedAt;

	private Camera _camera;

	private Vector3 _debugPlanePoint;

	private Coroutine _activeCoroutine;

	private Coroutine _animateToValueCoroutine;

	public ControlPurpose ControlComponentPurpose { get; set; }

	private void Awake()
	{
		if (base.gameObject.layer != ObjectPicker.LayerClickable)
		{
			Debug.LogWarning(base.name + "'s layer is not Clickable. Fixing.");
			base.gameObject.layer = ObjectPicker.LayerClickable;
		}
		_camera = Camera.main;
	}

	private void OnEnable()
	{
		this.CheckAnimationClip(animationClip);
		if (_clipPlayable == null)
		{
			_clipPlayable = animator.PlayableGraphAdapter().AddPlayable(animationClip);
		}
		_animationValue = value;
		UpdateAnimation();
	}

	private void OnDestroy()
	{
		_clipPlayable?.Dispose();
		_clipPlayable = null;
	}

	private void OnDrawGizmos()
	{
		if (_isActive)
		{
			Vector3 position = base.transform.position;
			Gizmos.color = Color.green;
			Gizmos.DrawLine(position, position + _debugPlanePoint);
		}
	}

	protected override void ValueDidChange()
	{
		if (_animateToValueCoroutine == null && base.isActiveAndEnabled)
		{
			_animateToValueCoroutine = StartCoroutine(AnimateToValue());
		}
	}

	private IEnumerator AnimateToValue()
	{
		while (Mathf.Abs(_animationValue - value) > 0.001f)
		{
			_animationValue = Mathf.Lerp(_animationValue, value, 20f * Time.deltaTime);
			UpdateAnimation();
			yield return null;
		}
		_animateToValueCoroutine = null;
	}

	private void StopAnimateToValueIfNeeded()
	{
		if (_animateToValueCoroutine != null)
		{
			StopCoroutine(_animateToValueCoroutine);
			_animateToValueCoroutine = null;
		}
	}

	private IEnumerator ActiveCoroutine()
	{
		if (_camera == null)
		{
			_camera = Camera.main;
		}
		StopAnimateToValueIfNeeded();
		while (_isActive)
		{
			yield return new WaitForFixedUpdate();
			float? num = CalculateParameter();
			if (num.HasValue)
			{
				float num2 = Snap(num.Value);
				_animationValue = Mathf.Lerp(_animationValue, num2, 20f * Time.deltaTime);
				if (Mathf.Abs(value - num2) > 1E-05f)
				{
					value = num2;
					UserChangedValue();
				}
			}
			UpdateAnimation();
		}
	}

	private IEnumerator DeactivateMomentaryCoroutine()
	{
		StopAnimateToValueIfNeeded();
		while (!_isActive && Time.realtimeSinceStartup - _deactivatedAt < 0.5f)
		{
			if (Mathf.Abs(value - homePosition) > 0.001f)
			{
				value = Mathf.Lerp(value, homePosition, 40f * Time.deltaTime);
				if (Mathf.Abs(value) < ChangeThreshold)
				{
					value = homePosition;
				}
				UserChangedValue(Math.Abs(value - homePosition) < 0.001f);
			}
			_animationValue = Mathf.Lerp(_animationValue, value, 5f * Time.deltaTime);
			UpdateAnimation();
			yield return new WaitForFixedUpdate();
		}
	}

	private float? CalculateParameter()
	{
		if (_useAngleManipulation)
		{
			if (rotationExtent == 0f)
			{
				return null;
			}
			float? num = MousePositionAngle();
			if (!num.HasValue)
			{
				return null;
			}
			float num2 = Mathf.DeltaAngle(_mouseDownAngle, num.Value) / rotationExtent;
			float num3 = _mouseDownValue + num2;
			if (Mathf.RoundToInt(rotationExtent) != 360 && ((num3 > 0.9f && value < 0.1f) || (num3 < 0.1f && value > 0.9f)))
			{
				return null;
			}
			return Mathf.Clamp01(num3);
		}
		float num4 = ((rotationExtent == 0f) ? 1f : rotationExtent);
		if (!MousePositionHandleVector(out var angleVector))
		{
			return null;
		}
		float num5 = Vector3.SignedAngle(angleVector, _mouseDownAngleVector, _cameraLocalControlUp);
		return Mathf.Clamp01(_mouseDownValue + num5 / num4);
	}

	private void UpdateAnimation()
	{
		if (!(animationClip == null) && _clipPlayable != null && (double)Mathf.Abs(_clipPlayable.Time / animationClip.length - _animationValue) > 0.001)
		{
			_clipPlayable.Time = _animationValue * animationClip.length;
		}
	}

	public override void Activate(PickableActivateEvent evt)
	{
		base.Activate(evt);
		_activatedAt = Time.realtimeSinceStartup;
		_cameraLocalControlPosition = _camera.transform.InverseTransformPoint(base.transform.position);
		_cameraLocalControlUp = _camera.transform.InverseTransformDirection(RotationVector());
		_cameraLocalControlHandle = _camera.transform.InverseTransformDirection(HandleVector());
		_useAngleManipulation = UseAngleManipulation();
		if (_useAngleManipulation)
		{
			float? num = MousePositionAngle();
			if (num.HasValue)
			{
				_mouseDownAngle = num.Value;
			}
			else
			{
				Debug.LogError("Failed to get mouse down angle vector in Activate");
			}
		}
		else if (!MousePositionHandleVector(out _mouseDownAngleVector))
		{
			Debug.LogError("Failed to get mouse down angle vector in Activate");
		}
		_mouseDownValue = value;
		_activeCoroutine = StartCoroutine(ActiveCoroutine());
	}

	public override void Deactivate()
	{
		base.Deactivate();
		_deactivatedAt = Time.realtimeSinceStartup;
		if (_activeCoroutine != null)
		{
			StopCoroutine(_activeCoroutine);
		}
		_activeCoroutine = null;
		if (momentary)
		{
			StartCoroutine(DeactivateMomentaryCoroutine());
		}
		else if (shiftActivateToggles && _deactivatedAt - _activatedAt < 0.125f && GameInput.IsShiftDown)
		{
			float num = ((Mathf.Abs(value - 0f) < 0.001f) ? 1f : ((!(Mathf.Abs(value - 1f) < 0.001f)) ? Mathf.Round(Mathf.Clamp01(value)) : 0f));
			value = num;
			UserChangedValue();
			ValueDidChange();
		}
	}

	private bool UseAngleManipulation()
	{
		Vector3 normalized = (_camera.transform.position - base.transform.position).normalized;
		return Mathf.Abs(Vector3.Dot(RotationVector(), normalized)) > 0.1f;
	}

	private float? MousePositionAngle()
	{
		Transform obj = _camera.transform;
		Vector3 vector = obj.TransformPoint(_cameraLocalControlPosition);
		Vector3 vector2 = obj.TransformDirection(_cameraLocalControlUp);
		Vector3 vector3 = obj.TransformDirection(_cameraLocalControlHandle);
		Plane plane = new Plane(vector2, vector);
		Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
		Vector3 velocity = _camera.velocity;
		if (velocity.magnitude < 50f)
		{
			ray.origin += velocity * Time.fixedDeltaTime;
		}
		if (!plane.Raycast(ray, out var enter))
		{
			return null;
		}
		Vector3 point = ray.GetPoint(enter);
		_debugPlanePoint = point - vector;
		Vector3 normalized = (point - vector).normalized;
		return Vector3.SignedAngle(vector3, normalized, vector2) + rotationStart;
	}

	private bool MousePositionHandleVector(out Vector3 angleVector)
	{
		Transform transform = _camera.transform;
		Vector3 center = transform.TransformPoint(_cameraLocalControlPosition);
		transform.TransformDirection(_cameraLocalControlUp);
		transform.TransformDirection(_cameraLocalControlHandle);
		Sphere s = new Sphere
		{
			center = center,
			radius = radius
		};
		Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
		if (!Intersections.Intersects(ray, s, out var distance, out var _))
		{
			angleVector = Vector3.zero;
			return false;
		}
		angleVector = transform.InverseTransformPoint(ray.GetPoint(distance)).normalized;
		return true;
	}

	public static Vector3 VectorForAxis(Axis theAxis, Transform transform)
	{
		return theAxis switch
		{
			Axis.X => transform.right, 
			Axis.Y => transform.up, 
			Axis.Z => transform.forward, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
	}

	private Vector3 RotationVector()
	{
		return VectorForAxis(rotationAxis, base.transform);
	}

	private Vector3 HandleVector()
	{
		return VectorForAxis(handleAxis, base.transform);
	}
}
