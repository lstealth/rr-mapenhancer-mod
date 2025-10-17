using System;
using Audio;
using Model.Definition;
using Model.Definition.Components;

namespace Model.ComponentBuilders;

[ComponentBuilder]
public class DynamoComponentBuilder : IComponentBuilder
{
	public Type ComponentType => typeof(DynamoComponent);

	public void Build(ComponentBuilderContext ctx, Component component)
	{
		_Build(ctx, (DynamoComponent)component);
	}

	private void _Build(ComponentBuilderContext ctx, DynamoComponent component)
	{
		ctx.InstantiatePrefab<DynamoPlayer>("dynamo", ctx.GameObject.transform);
	}
}
