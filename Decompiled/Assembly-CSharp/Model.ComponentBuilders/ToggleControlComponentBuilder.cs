using System;
using Game.Messages;
using Model.Definition;
using Model.Definition.Components;
using RollingStock.Controls;

namespace Model.ComponentBuilders;

[ComponentBuilder]
public class ToggleControlComponentBuilder : IComponentBuilder
{
	public Type ComponentType => typeof(ToggleControlComponent);

	public void Build(ComponentBuilderContext ctx, Component component)
	{
		_Build(ctx, (ToggleControlComponent)component);
	}

	private void _Build(ComponentBuilderContext ctx, ToggleControlComponent component)
	{
		PropertyChange.Control? control = ControlForPurpose(component.Purpose);
		if (control.HasValue)
		{
			KeyValuePickableToggle keyValuePickableToggle = ctx.Resolve(component.TargetColliderObject).gameObject.AddComponent<KeyValuePickableToggle>();
			keyValuePickableToggle.key = PropertyChange.KeyForControl(control.Value);
			keyValuePickableToggle.displayTitle = component.Title;
			keyValuePickableToggle.displayMessageTrue = component.MessageTrue;
			keyValuePickableToggle.displayMessageFalse = component.MessageFalse;
			keyValuePickableToggle.maxPickDistance = 50f;
		}
	}

	private static PropertyChange.Control? ControlForPurpose(ControlPurpose purpose)
	{
		return purpose switch
		{
			ControlPurpose.NotSet => null, 
			ControlPurpose.CylinderCock => PropertyChange.Control.CylinderCock, 
			ControlPurpose.LocomotiveBrake => PropertyChange.Control.LocomotiveBrake, 
			ControlPurpose.TrainBrake => PropertyChange.Control.TrainBrake, 
			ControlPurpose.Reverser => PropertyChange.Control.Reverser, 
			ControlPurpose.Throttle => PropertyChange.Control.Throttle, 
			ControlPurpose.Whistle => PropertyChange.Control.Horn, 
			ControlPurpose.Bell => PropertyChange.Control.Bell, 
			ControlPurpose.TrainBrakeCutOut => PropertyChange.Control.CutOut, 
			_ => throw new ArgumentOutOfRangeException("purpose", purpose, null), 
		};
	}
}
