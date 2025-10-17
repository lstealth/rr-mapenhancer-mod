using System;
using Game.Messages;
using Game.State;
using Model;
using Model.AI;
using Track;
using UnityEngine;

namespace UI.EngineControls;

public readonly struct AutoEngineerOrdersHelper
{
	public const int SpeedInc = 5;

	private readonly Car _locomotive;

	private readonly AutoEngineerPersistence _persistence;

	public Orders Orders => _persistence.Orders;

	public AutoEngineerMode Mode => Orders.Mode;

	public AutoEngineerOrdersHelper(Car locomotive, AutoEngineerPersistence persistence)
	{
		_locomotive = locomotive;
		_persistence = persistence;
	}

	private void SendAutoEngineerCommand(AutoEngineerMode mode, bool forward, int maxSpeedMph, float? distance, OrderWaypoint? maybeWaypoint)
	{
		StateManager.ApplyLocal(new AutoEngineerCommand(_locomotive.id, mode, forward, maxSpeedMph, distance, maybeWaypoint?.LocationString, maybeWaypoint?.CoupleToCarId));
	}

	public void SetOrdersValue(AutoEngineerMode? mode = null, bool? forward = null, int? maxSpeedMph = null, float? distance = null, (Location location, string couple)? maybeWaypoint = null)
	{
		Orders orders = _persistence.Orders;
		float num = _locomotive.velocity * 2.23694f;
		if (!maxSpeedMph.HasValue && mode.HasValue && mode.Value != AutoEngineerMode.Off)
		{
			float num2 = Mathf.Abs(num);
			maxSpeedMph = mode switch
			{
				AutoEngineerMode.Road => (num2 > 0.1f) ? (Mathf.CeilToInt(num2 / 5f) * 5) : 0, 
				AutoEngineerMode.Waypoint => (orders.MaxSpeedMph > 0) ? orders.MaxSpeedMph : 35, 
				_ => null, 
			};
		}
		AutoEngineerMode autoEngineerMode = mode ?? Mode;
		int maxSpeedMph2 = Mathf.Min(maxSpeedMph ?? orders.MaxSpeedMph, autoEngineerMode.MaxSpeedMph());
		if (autoEngineerMode == AutoEngineerMode.Yard && distance > 0f)
		{
			maxSpeedMph2 = ((distance > 0f) ? AutoEngineerMode.Yard.MaxSpeedMph() : 0);
		}
		OrderWaypoint? maybeWaypoint2 = orders.Waypoint;
		if (maybeWaypoint.HasValue)
		{
			(Location, string) valueOrDefault = maybeWaypoint.GetValueOrDefault();
			maybeWaypoint2 = new OrderWaypoint(Graph.Shared.LocationToString(valueOrDefault.Item1), valueOrDefault.Item2);
		}
		if (autoEngineerMode != AutoEngineerMode.Waypoint)
		{
			maybeWaypoint2 = null;
		}
		if (!forward.HasValue && !orders.Enabled)
		{
			forward = num >= -0.01f;
		}
		SendAutoEngineerCommand(autoEngineerMode, forward ?? orders.Forward, maxSpeedMph2, distance, maybeWaypoint2);
	}

	public void ClearWaypoint()
	{
		Orders orders = _persistence.Orders;
		if (orders.Mode != AutoEngineerMode.Waypoint)
		{
			throw new Exception("Expected Waypoint mode");
		}
		SendAutoEngineerCommand(AutoEngineerMode.Waypoint, orders.Forward, orders.MaxSpeedMph, null, null);
	}

	public void SetWaypoint(Location location, string coupleToCarId)
	{
		(Location, string)? maybeWaypoint = (location, coupleToCarId);
		SetOrdersValue(null, null, null, null, maybeWaypoint);
	}
}
