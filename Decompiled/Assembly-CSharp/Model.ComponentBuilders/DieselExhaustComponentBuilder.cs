using System;
using Model.Definition;
using Model.Definition.Components;
using UnityEngine;

namespace Model.ComponentBuilders;

[ComponentBuilder]
public class DieselExhaustComponentBuilder : IComponentBuilder
{
	public Type ComponentType => typeof(DieselExhaustComponent);

	public void Build(ComponentBuilderContext ctx, Model.Definition.Component component)
	{
		_Build(ctx, (DieselExhaustComponent)component);
	}

	private void _Build(ComponentBuilderContext ctx, DieselExhaustComponent component)
	{
		ctx.InstantiatePrefab<UnityEngine.Component>("diesel-exhaust", ctx.GameObject.transform);
	}
}
