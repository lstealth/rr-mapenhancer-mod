using System;
using Model.Definition;
using Model.Definition.Components;
using RollingStock;

namespace Model.ComponentBuilders;

[ComponentBuilder]
public class DetailModelComponentBuilder : IComponentBuilder
{
	public Type ComponentType => typeof(DetailModelComponent);

	public void Build(ComponentBuilderContext ctx, Component component)
	{
		_Build(ctx, (DetailModelComponent)component);
	}

	private void _Build(ComponentBuilderContext ctx, DetailModelComponent component)
	{
		ctx.GameObject.AddComponent<DetailModelController>().Configure(component);
	}
}
