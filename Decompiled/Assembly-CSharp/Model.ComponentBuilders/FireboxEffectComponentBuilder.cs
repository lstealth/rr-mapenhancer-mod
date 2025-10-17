using System;
using Model.Definition;
using Model.Definition.Components;
using UnityEngine;

namespace Model.ComponentBuilders;

[ComponentBuilder]
public class FireboxEffectComponentBuilder : IComponentBuilder
{
	public Type ComponentType => typeof(FireboxEffectComponent);

	public void Build(ComponentBuilderContext ctx, Model.Definition.Component component)
	{
		_Build(ctx, (FireboxEffectComponent)component);
	}

	private void _Build(ComponentBuilderContext ctx, FireboxEffectComponent component)
	{
		ctx.InstantiatePrefab<UnityEngine.Component>("firebox-fire-quad", ctx.GameObject.transform);
	}
}
