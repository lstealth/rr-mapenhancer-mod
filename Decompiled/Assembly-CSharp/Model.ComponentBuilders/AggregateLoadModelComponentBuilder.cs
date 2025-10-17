using System;
using Model.Definition;
using Model.Definition.Components;
using RollingStock.LoadModels;

namespace Model.ComponentBuilders;

[ComponentBuilder]
public class AggregateLoadModelComponentBuilder : IComponentBuilder
{
	public Type ComponentType => typeof(AggregateLoadModelComponent);

	public void Build(ComponentBuilderContext ctx, Component component)
	{
		_Build(ctx, (AggregateLoadModelComponent)component);
	}

	private void _Build(ComponentBuilderContext ctx, AggregateLoadModelComponent component)
	{
		ctx.GameObject.AddComponent<AggregateLoadModelController>().Configure(component.Keyframes);
	}
}
