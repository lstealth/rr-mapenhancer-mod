using System;
using Model.Definition;
using Model.Definition.Components;
using RollingStock;
using Serilog;
using UnityEngine;

namespace Model.ComponentBuilders;

[ComponentBuilder]
public class LoadTargetComponentBuilder : IComponentBuilder
{
	public Type ComponentType => typeof(LoadTargetComponent);

	public void Build(ComponentBuilderContext ctx, Model.Definition.Component component)
	{
		_Build(ctx, (LoadTargetComponent)component);
	}

	private void _Build(ComponentBuilderContext ctx, LoadTargetComponent component)
	{
		CarLoadTarget carLoadTarget = ctx.GameObject.AddComponent<CarLoadTarget>();
		if (component.Radius < 0.3f)
		{
			Log.Debug("Component {componentName} radius {radius} below minimum {min}", component.Name, component.Radius, 0.3f);
		}
		carLoadTarget.radius = Mathf.Max(component.Radius, 0.3f);
		carLoadTarget.slotIndex = component.SlotIndex;
	}
}
