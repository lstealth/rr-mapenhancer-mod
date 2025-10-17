using System;
using Model.Definition;
using Model.Definition.Components;
using RollingStock;

namespace Model.ComponentBuilders;

[ComponentBuilder]
public class LoadModelComponentBuilder : IComponentBuilder
{
	public Type ComponentType => typeof(LoadModelComponent);

	public void Build(ComponentBuilderContext ctx, Component component)
	{
		_Build(ctx, (LoadModelComponent)component);
	}

	private void _Build(ComponentBuilderContext ctx, LoadModelComponent component)
	{
		ctx.GameObject.AddComponent<CarLoadModelController>().Configure(component.SlotIndex, component.LoadIdentifier, component.Models, component.Instances);
	}
}
