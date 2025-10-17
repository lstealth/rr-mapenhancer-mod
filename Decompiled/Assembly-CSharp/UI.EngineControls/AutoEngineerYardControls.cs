using Model.AI;
using TMPro;
using UnityEngine;

namespace UI.EngineControls;

public class AutoEngineerYardControls : AutoEngineerControlSetBase
{
	[SerializeField]
	private CarControlSlider directionSlider;

	[SerializeField]
	private TMP_Text directionLabel;

	protected override void UpdateControls()
	{
		Orders orders = base.OrdersHelper.Orders;
		directionLabel.text = (orders.Forward ? "F" : "R");
	}

	public override void OnOrdersDidChange(Orders orders)
	{
		base.OnOrdersDidChange(orders);
		directionSlider.SetValueWithoutNotify(orders.Forward ? 1 : 0);
	}

	public void HandleDirectionDidChange(float value)
	{
		AutoEngineerOrdersHelper ordersHelper = base.OrdersHelper;
		bool? forward = value > 0f;
		ordersHelper.SetOrdersValue(null, forward);
	}

	public void HandleSetClearanceCars(float cars)
	{
		AutoEngineerOrdersHelper ordersHelper = base.OrdersHelper;
		float? distance = cars * 12.2f;
		ordersHelper.SetOrdersValue(null, null, null, distance);
	}
}
