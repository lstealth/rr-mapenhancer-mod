using System;
using Model.Definition;
using Model.Definition.Components;
using RollingStock.Steam;

namespace Model.ComponentBuilders;

[ComponentBuilder]
public class WhistleComponentBuilder : IComponentBuilder
{
	public Type ComponentType => typeof(WhistleComponent);

	public void Build(ComponentBuilderContext ctx, Component component)
	{
		_Build(ctx, (WhistleComponent)component);
	}

	private void _Build(ComponentBuilderContext ctx, WhistleComponent component)
	{
		ctx.InstantiatePrefab<WhistleController>("whistle", ctx.GameObject.transform).Configure(component);
	}
}
