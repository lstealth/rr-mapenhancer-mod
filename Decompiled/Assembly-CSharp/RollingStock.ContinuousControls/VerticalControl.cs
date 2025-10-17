using Helpers.Animation;
using UnityEngine;

namespace RollingStock.ContinuousControls;

public class VerticalControl : ContinuousControl
{
	public AnimationClip animationClip;

	public Animator animator;

	[Tooltip("Maintain a zero state by default.")]
	public bool momentary;

	private PlayableHandle _clipPlayable;

	private int _clipPlayablePort;

	private float _mouseDownValue;

	private float _animationValue;

	private Camera _camera;

	private Vector3 _mousePositionAtDown;

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
		_clipPlayable = animator.PlayableGraphAdapter().AddPlayable(animationClip);
	}

	private void OnDisable()
	{
		_clipPlayable?.Dispose();
		_clipPlayable = null;
	}

	private void FixedUpdate()
	{
		if (_isActive)
		{
			if (CalculateParameter(out var newParameter))
			{
				newParameter = Snap(newParameter);
				_animationValue = Mathf.Lerp(_animationValue, newParameter, 20f * Time.deltaTime);
				value = newParameter;
				UserChangedValue();
			}
		}
		else
		{
			if (momentary && Mathf.Abs(value) > 0.001f)
			{
				value = Mathf.Lerp(value, 0f, 40f * Time.deltaTime);
				if (Mathf.Abs(value) < 0.001f)
				{
					value = 0f;
				}
				UserChangedValue();
			}
			_animationValue = Mathf.Lerp(_animationValue, value, 5f * Time.deltaTime);
		}
		UpdateAnimation();
	}

	private bool CalculateParameter(out float newParameter)
	{
		float num = 0f - (Input.mousePosition.y - _mousePositionAtDown.y);
		float num2 = 0.005f;
		newParameter = Mathf.Clamp01(_mouseDownValue + num * num2);
		return true;
	}

	private void UpdateAnimation()
	{
		if ((double)Mathf.Abs(_clipPlayable.Time / animationClip.length - _animationValue) > 0.001)
		{
			_clipPlayable.Time = _animationValue * animationClip.length;
		}
	}

	public override void Activate(PickableActivateEvent evt)
	{
		base.Activate(evt);
		_mousePositionAtDown = Input.mousePosition;
		_mouseDownValue = value;
	}
}
