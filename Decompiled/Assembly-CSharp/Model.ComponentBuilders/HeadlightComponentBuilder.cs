using System;
using Effects;
using Model.Definition;
using Model.Definition.Components;

namespace Model.ComponentBuilders;

[ComponentBuilder]
public class HeadlightComponentBuilder : IComponentBuilder
{
	public Type ComponentType => typeof(HeadlightComponent);

	public void Build(ComponentBuilderContext ctx, Component component)
	{
		_Build(ctx, (HeadlightComponent)component);
	}

	private void _Build(ComponentBuilderContext ctx, HeadlightComponent component)
	{
		HeadlightController headlightController = ctx.InstantiatePrefab<HeadlightController>("headlight", ctx.GameObject.transform);
		headlightController.LightEnabled = component.LightEnabled;
		headlightController.Direction = ((!component.Forward) ? HeadlightController.HeadlightDirection.Reverse : HeadlightController.HeadlightDirection.Forward);
	}
}
