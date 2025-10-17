using Game.Messages;
using Model;
using Model.Definition;
using Model.Physics;
using RollingStock;
using TMPro;
using UI.Tooltips;
using UnityEngine;

namespace UI.EngineControls;

public class ManualControls : EngineControlSetBase
{
	[SerializeField]
	private CarControlSlider throttleSlider;

	[SerializeField]
	private CarControlSlider reverserSlider;

	[SerializeField]
	private CarControlSlider locomotiveBrakeSlider;

	[SerializeField]
	private CarControlSlider trainBrakeSlider;

	private bool _isDiesel;

	protected override void UpdateControls()
	{
		BaseLocomotive locomotive = base.Locomotive;
		LocomotiveControlAdapter locomotiveControl = locomotive.locomotiveControl;
		throttleSlider.SetValueUnlessDragging(locomotiveControl.ThrottleDisplay);
		reverserSlider.SetValueUnlessDragging(locomotiveControl.AbstractReverser);
		locomotiveBrakeSlider.SetValueUnlessDragging(locomotiveControl.LocomotiveBrakeSetting);
		trainBrakeSlider.SetValueUnlessDragging(locomotiveControl.TrainBrakeSetting);
		throttleSlider.handleText.SetText("{0}", locomotiveControl.ThrottleDisplay);
		SetReverserText(locomotiveControl.AbstractReverser, reverserSlider.handleText);
		LocomotiveAirSystem locomotiveAirSystem = locomotive.air as LocomotiveAirSystem;
		trainBrakeSlider.handleText.SetText("{0}", Mathf.RoundToInt(locomotiveAirSystem.BrakeLine.Pressure));
		locomotiveBrakeSlider.handleText.SetText("{0}", Mathf.RoundToInt(locomotiveAirSystem.BrakeCylinder.Pressure));
	}

	protected override void UpdateForLocomotive()
	{
		base.UpdateForLocomotive();
		_isDiesel = base.Locomotive.Archetype == CarArchetype.LocomotiveDiesel;
		reverserSlider.wholeNumbers = _isDiesel;
		throttleSlider.minValue = 0f;
		throttleSlider.maxValue = base.Locomotive.locomotiveControl.ThrottleValueSteps;
		throttleSlider.wholeNumbers = true;
		if (reverserSlider.TryGetComponent<UITooltipProvider>(out var component))
		{
			component.Title = (_isDiesel ? "Reverser" : "Reverser / Cutoff");
		}
	}

	private void SetReverserText(float abstractReverser, TMP_Text text)
	{
		if (Mathf.Abs(abstractReverser) < 0.1f)
		{
			text.text = "N";
			return;
		}
		if (_isDiesel)
		{
			text.text = ((abstractReverser < 0f) ? "R" : "F");
			return;
		}
		int num = Mathf.RoundToInt(abstractReverser * 100f);
		if (num < 0)
		{
			if (num == -100)
			{
				text.SetText("-1<mspace=0.6em>00</mspace>");
			}
			else
			{
				text.SetText("-<mspace=0.6em>{0}</mspace>", -num);
			}
		}
		else if (num == 100)
		{
			text.SetText("1<mspace=0.6em>00</mspace>");
		}
		else
		{
			text.SetText("<mspace=0.6em>{0}</mspace>", num);
		}
	}

	public void LocomotiveBrakeDidChange(float value)
	{
		ChangeValue(PropertyChange.Control.LocomotiveBrake, value);
	}

	public void TrainBrakeDidChange(float value)
	{
		ChangeValue(PropertyChange.Control.TrainBrake, value);
	}

	public void ThrottleDidChange(float floatValue)
	{
		ChangeValue(PropertyChange.Control.Throttle, floatValue / throttleSlider.maxValue);
	}

	public void ReverserDidChange(float floatValue)
	{
		floatValue = Mathf.Round(floatValue * 20f) / 20f;
		ChangeValue(PropertyChange.Control.Reverser, floatValue);
	}
}
