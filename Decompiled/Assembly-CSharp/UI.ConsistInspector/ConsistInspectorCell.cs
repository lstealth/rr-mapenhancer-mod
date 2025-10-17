using Model;
using Model.Physics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.ConsistInspector;

[RequireComponent(typeof(RectTransform))]
public class ConsistInspectorCell : MonoBehaviour
{
	public TMP_Text labelName;

	public TMP_Text labelType;

	public TMP_Text labelDestination;

	public TMP_Text labelContents;

	public TMP_Text brakeInfo;

	public Slider anglecockSliderLeft;

	public Slider anglecockSliderRight;

	public Button cutButtonLeft;

	public Button cutButtonRight;

	private Car _car;

	public Car car
	{
		get
		{
			return _car;
		}
		set
		{
			_car = value;
			CarDidChange();
		}
	}

	private void Update()
	{
		UpdateAir();
	}

	private void UpdateAir()
	{
		if (_car == null)
		{
			brakeInfo.text = "";
			return;
		}
		string text;
		if (_car.air is LocomotiveAirSystem locomotiveAirSystem)
		{
			text = $"BP: {locomotiveAirSystem.BrakeLine.Pressure:F1}\nMR: {locomotiveAirSystem.MainReservoir.Pressure:F1}\nBC: {locomotiveAirSystem.BrakeCylinder.Pressure:F1}";
		}
		else
		{
			CarAirSystem air = _car.air;
			text = $"BP: {air.BrakeLine.Pressure:F1}\nRS: {air.BrakeReservoir.Pressure:F1}\nBC: {air.BrakeCylinder.Pressure:F1}";
		}
		brakeInfo.text = text;
		anglecockSliderLeft.value = _car.EndGearA.AnglecockSetting;
		anglecockSliderRight.value = _car.EndGearB.AnglecockSetting;
	}

	private void CarDidChange()
	{
		labelName.text = _car.DisplayName;
		labelDestination.text = "Destination TBD";
		labelContents.text = "Contents TBD";
		labelType.text = _car.CarType;
		brakeInfo.text = "";
	}

	public void OnAnglecockSliderLeftChanged(float value)
	{
	}

	public void OnAnglecockSliderRightChanged(float value)
	{
	}

	public void OnCutButtonLeft()
	{
	}

	public void OnCutButtonRight()
	{
	}

	public void OnFollowButton()
	{
	}
}
