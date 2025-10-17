using System;
using Character;
using Helpers;
using Model.Definition;
using Model.Definition.Components;
using UnityEngine;

namespace Model.ComponentBuilders;

[ComponentBuilder]
public class LadderComponentBuilder : IComponentBuilder
{
	public Type ComponentType => typeof(LadderComponent);

	public void Build(ComponentBuilderContext ctx, Model.Definition.Component component)
	{
		_Build(ctx, (LadderComponent)component);
	}

	private void _Build(ComponentBuilderContext ctx, LadderComponent component)
	{
		ctx.GameObject.layer = Layers.Ladder;
		ctx.GameObject.AddComponent<CapsuleCollider>();
		ctx.GameObject.AddComponent<Ladder>().height = component.Height;
	}
}
