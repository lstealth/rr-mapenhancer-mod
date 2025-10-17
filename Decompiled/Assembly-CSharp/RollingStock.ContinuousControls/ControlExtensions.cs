using System;
using Game.Messages;
using Game.State;

namespace RollingStock.ContinuousControls;

public static class ControlExtensions
{
	public static void ConfigurePropertyChange(this ContinuousControl control, Func<float, PropertyChange> propertyChangeFunc, Func<string> tooltipText = null)
	{
		IGameMessage authorizedMessage = propertyChangeFunc(0f);
		control.OnValueChanged += delegate(float value)
		{
			StateManager.ApplyLocal(propertyChangeFunc(value));
		};
		control.CheckAuthorized = () => StateManager.CheckAuthorizedToSendMessage(authorizedMessage);
		if (tooltipText != null)
		{
			control.tooltipText = tooltipText;
		}
	}
}
