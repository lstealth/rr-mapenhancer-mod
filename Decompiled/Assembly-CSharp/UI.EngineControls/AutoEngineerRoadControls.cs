using Game.Messages;
using Model.AI;
using TMPro;
using UnityEngine;

namespace UI.EngineControls;

public class AutoEngineerRoadControls : AutoEngineerControlSetBase
{
	[SerializeField]
	private CarControlSlider directionSlider;

	[SerializeField]
	private CarControlSlider maxSpeedSlider;

	[SerializeField]
	private TMP_Text directionLabel;

	[SerializeField]
	private TMP_Text maxSpeedLabel;

	private void Awake()
	{
		maxSpeedSlider.wholeNumbers = true;
		maxSpeedSlider.minValue = 0f;
		maxSpeedSlider.maxValue = SpeedMphToSlider(AutoEngineerMode.Road.MaxSpeedMph());
	}

	private static float SpeedMphToSlider(float mph)
	{
		return Mathf.Round(mph / 5f);
	}

	private static float SliderToSpeedMph(float value)
	{
		return value * 5f;
	}

	protected override void UpdateControls()
	{
		Orders orders = base.OrdersHelper.Orders;
		directionLabel.text = (orders.Forward ? "F" : "R");
		maxSpeedLabel.SetText("{0}", orders.MaxSpeedMph);
	}

	public override void OnOrdersDidChange(Orders orders)
	{
		base.OnOrdersDidChange(orders);
		directionSlider.SetValueWithoutNotify(orders.Forward ? 1 : 0);
		maxSpeedSlider.SetValueWithoutNotify(SpeedMphToSlider(orders.MaxSpeedMph));
	}

	public void HandleDirectionDidChange(float value)
	{
		AutoEngineerOrdersHelper ordersHelper = base.OrdersHelper;
		bool? forward = value > 0f;
		ordersHelper.SetOrdersValue(null, forward);
	}

	public void HandleMaxSpeedDidChange(float value)
	{
		AutoEngineerOrdersHelper ordersHelper = base.OrdersHelper;
		int? maxSpeedMph = Mathf.RoundToInt(SliderToSpeedMph(value));
		ordersHelper.SetOrdersValue(null, null, maxSpeedMph);
	}
}
