using System;
using Helpers;
using Model.Definition;
using Model.Definition.Components;
using RollingStock;
using UnityEngine;

namespace Model.ComponentBuilders;

[ComponentBuilder]
public class SeatComponentBuilder : IComponentBuilder
{
	public Type ComponentType => typeof(SeatComponent);

	public void Build(ComponentBuilderContext ctx, Model.Definition.Component component)
	{
		_Build(ctx, (SeatComponent)component);
	}

	private void _Build(ComponentBuilderContext ctx, SeatComponent component)
	{
		ctx.GameObject.layer = Layers.Ladder;
		SphereCollider sphereCollider = ctx.GameObject.AddComponent<SphereCollider>();
		sphereCollider.radius = 0.2f;
		sphereCollider.isTrigger = true;
		ctx.GameObject.AddComponent<Seat>().priority = component.Priority;
	}
}
