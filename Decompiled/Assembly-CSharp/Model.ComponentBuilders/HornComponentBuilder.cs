using System;
using Audio;
using Model.Definition;
using Model.Definition.Components;

namespace Model.ComponentBuilders;

[ComponentBuilder]
public class HornComponentBuilder : IComponentBuilder
{
	public Type ComponentType => typeof(HornComponent);

	public void Build(ComponentBuilderContext ctx, Component component)
	{
		_Build(ctx, (HornComponent)component);
	}

	private void _Build(ComponentBuilderContext ctx, HornComponent component)
	{
		ctx.InstantiatePrefab<HornPlayer>("horn", ctx.GameObject.transform);
		_ = component.DefaultHornIdentifier;
	}
}
