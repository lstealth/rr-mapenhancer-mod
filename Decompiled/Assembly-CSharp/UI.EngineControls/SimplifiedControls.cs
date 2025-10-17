using System.Collections;
using Game.Messages;
using Model;
using RollingStock;
using UnityEngine;

namespace UI.EngineControls;

public class SimplifiedControls : EngineControlSetBase
{
	[SerializeField]
	private CarControlSlider simpleSlider;

	[SerializeField]
	private CarControlSlider directionSlider;

	private Coroutine _reverserCoroutine;

	private const float SimpleDeadZone = 0.15f;

	private const float BrakePow = 1.5f;

	private void Awake()
	{
		directionSlider.minValue = -1f;
		directionSlider.maxValue = 1f;
		directionSlider.wholeNumbers = true;
	}

	private void OnEnable()
	{
		_reverserCoroutine = StartCoroutine(UpdateReverserCoroutine());
	}

	private void OnDisable()
	{
		StopCoroutine(_reverserCoroutine);
		_reverserCoroutine = null;
	}

	private IEnumerator UpdateReverserCoroutine()
	{
		WaitForSeconds wait = new WaitForSeconds(1f);
		while (true)
		{
			if (base.Locomotive != null)
			{
				UpdateReverserForSimplified();
			}
			yield return wait;
		}
	}

	protected override void UpdateControls()
	{
		BaseLocomotive locomotive = base.Locomotive;
		if (!(locomotive == null))
		{
			simpleSlider.SetValueUnlessDragging(SimpleValue());
			directionSlider.value = DirectionValue();
			LocomotiveControlAdapter locomotiveControl = locomotive.locomotiveControl;
			if (simpleSlider.value > 0.15f)
			{
				simpleSlider.handleText.SetText("{0}%", Mathf.RoundToInt(locomotiveControl.AbstractThrottle * 100f));
			}
			else if (simpleSlider.value < -0.15f)
			{
				simpleSlider.handleText.text = "B";
			}
			else
			{
				simpleSlider.handleText.text = "N";
			}
			directionSlider.handleText.text = ((directionSlider.value < 0f) ? "R" : "F");
		}
	}

	public void SimpleDidChange(float sliderValue)
	{
		UpdateForSimpleValues(sliderValue, directionSlider.value);
	}

	public void DirectionDidChange(float floatValue)
	{
		ChangeValue(PropertyChange.Control.Reverser, floatValue);
		UpdateForSimpleValues(simpleSlider.value, floatValue);
		UpdateReverserForSimplified();
	}

	private void UpdateForSimpleValues(float sliderValue, float directionValue)
	{
		float num = SimplifiedSliderValueToNormalized(sliderValue);
		float value;
		float value2;
		float value3;
		if (num > 0.001f)
		{
			value = num;
			value2 = 0f;
			value3 = 0f;
		}
		else
		{
			value = 0f;
			value3 = (value2 = Mathf.Pow(0f - num, 1.5f));
		}
		ChangeValue(PropertyChange.Control.Throttle, value);
		ChangeValue(PropertyChange.Control.LocomotiveBrake, value2);
		ChangeValue(PropertyChange.Control.TrainBrake, value3);
	}

	private float SimplifiedSliderValueToNormalized(float sliderValue)
	{
		if (Mathf.Abs(sliderValue) < 0.15f)
		{
			simpleSlider.SetValueWithoutNotify(0f);
			return 0f;
		}
		return (sliderValue > 0f) ? Mathf.InverseLerp(0.15f, 1f, sliderValue) : (0f - Mathf.InverseLerp(-0.15f, -1f, sliderValue));
	}

	private void UpdateReverserForSimplified()
	{
		if (!(SimplifiedSliderValueToNormalized(simpleSlider.value) < 0.001f))
		{
			float num = Mathf.Sign(directionSlider.value);
			float num2 = base.Locomotive.CutoffSettingForVelocity(base.Locomotive.velocity);
			num2 = (float)Mathf.CeilToInt(num2 * 10f) / 10f;
			ChangeValue(PropertyChange.Control.Reverser, num * num2);
		}
	}

	private float SimpleValue()
	{
		float throttle = base.Locomotive.ControlHelper.Throttle;
		if (throttle > 0.001f)
		{
			return Mathf.Lerp(0.15f, 1f, throttle);
		}
		float locomotiveBrake = base.Locomotive.ControlHelper.LocomotiveBrake;
		if (locomotiveBrake > 0.001f)
		{
			return Mathf.Lerp(-0.15f, -1f, Mathf.Pow(locomotiveBrake, 2f / 3f));
		}
		return 0f;
	}

	private float DirectionValue()
	{
		return (!(base.Locomotive.ControlHelper.Reverser < 0f)) ? 1 : (-1);
	}
}
