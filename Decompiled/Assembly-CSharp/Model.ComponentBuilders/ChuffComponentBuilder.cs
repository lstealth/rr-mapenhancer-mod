using System;
using Model.Definition;
using Model.Definition.Components;
using RollingStock.Steam;
using UnityEngine;

namespace Model.ComponentBuilders;

[ComponentBuilder]
public class ChuffComponentBuilder : IComponentBuilder
{
	public Type ComponentType => typeof(ChuffComponent);

	public void Build(ComponentBuilderContext ctx, Model.Definition.Component component)
	{
		_Build(ctx, (ChuffComponent)component);
	}

	private void _Build(ComponentBuilderContext ctx, ChuffComponent component)
	{
		ctx.InstantiatePrefab<UnityEngine.Component>("chuff", ctx.GameObject.transform);
		ctx.InstantiatePrefab<SteamChuffParticleController>("smokestack", ctx.GameObject.transform);
	}
}
