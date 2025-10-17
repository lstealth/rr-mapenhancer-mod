using System;
using System.Collections.Generic;
using System.Linq;
using Audio;
using Helpers.Animation;
using Model;
using UnityEngine;

namespace RollingStock;

public class Wheelset : MonoBehaviour, IBrakeAnimator
{
	[SerializeField]
	internal List<Transform> wheels = new List<Transform>();

	[Header("Animation")]
	[SerializeField]
	internal float diameterInInches = 33f;

	[SerializeField]
	internal Animator animator;

	[SerializeField]
	internal AnimationClip applyBrakesAnimationClip;

	private bool _brakeAppliedAnimationState;

	private WheelAudio _wheelAudio;

	private float _localOdometer;

	private PlayableHandle _applyBrakesPlayable;

	private Renderer[] _renderers;

	public bool BrakeApplied
	{
		get
		{
			return _brakeAppliedAnimationState;
		}
		set
		{
			if (value != _brakeAppliedAnimationState)
			{
				_brakeAppliedAnimationState = value;
				BrakeAppliedDidChange();
			}
		}
	}

	private void OnEnable()
	{
		if (applyBrakesAnimationClip != null && animator != null)
		{
			_applyBrakesPlayable = animator.PlayableGraphAdapter().AddPlayable(applyBrakesAnimationClip);
		}
	}

	private void OnDisable()
	{
		_applyBrakesPlayable?.Dispose();
		_applyBrakesPlayable = null;
	}

	public void Configure(WheelClackProfile wheelClackProfile, Car car)
	{
		_wheelAudio = base.gameObject.AddComponent<WheelAudio>();
		_wheelAudio.Configure(wheelClackProfile, wheels, car);
	}

	public void SetLinearOffset(float value)
	{
		_wheelAudio.LinearOffset = value;
	}

	public void Roll(float distance, float velocity)
	{
		_localOdometer += distance;
		float num = diameterInInches * 0.0254f;
		float num2 = MathF.PI * num;
		float num3 = 10f * num2;
		if (_localOdometer > num3)
		{
			_localOdometer -= num3;
		}
		else if (_localOdometer < 0f - num3)
		{
			_localOdometer += num3;
		}
		float x = 360f / num2 * _localOdometer;
		foreach (Transform wheel in wheels)
		{
			wheel.localEulerAngles = new Vector3(x, 0f, 0f);
		}
		_wheelAudio.Roll(distance, velocity);
	}

	private void BrakeAppliedDidChange()
	{
		if (_applyBrakesPlayable != null)
		{
			_applyBrakesPlayable.Time = Mathf.Clamp(_applyBrakesPlayable.Time, 0f, applyBrakesAnimationClip.length);
			_applyBrakesPlayable.Speed = (BrakeApplied ? 1 : (-1));
			_applyBrakesPlayable.Play();
		}
	}

	public void SetVisible(bool visible)
	{
		if (_renderers == null)
		{
			_renderers = (from r in GetComponentsInChildren<Renderer>()
				where r.enabled
				select r).ToArray();
		}
		Renderer[] renderers = _renderers;
		for (int num = 0; num < renderers.Length; num++)
		{
			renderers[num].enabled = visible;
		}
	}

	public float CalculateAxleSpread()
	{
		if (wheels.Count == 0)
		{
			return 2f;
		}
		float num = float.MaxValue;
		float num2 = float.MinValue;
		Transform transform = base.transform;
		foreach (Transform wheel in wheels)
		{
			Vector3 vector = transform.InverseTransformPoint(wheel.transform.position);
			if (vector.z < num)
			{
				num = vector.z;
			}
			if (vector.z > num2)
			{
				num2 = vector.z;
			}
		}
		return num2 - num;
	}
}
