using System;
using Audio;
using Model.Definition;
using Model.Definition.Components;
using RollingStock;
using UnityEngine;

namespace Model.ComponentBuilders;

[ComponentBuilder]
public class BellComponentBuilder : IComponentBuilder
{
	public Type ComponentType => typeof(BellComponent);

	public void Build(ComponentBuilderContext ctx, Model.Definition.Component component)
	{
		_Build(ctx, (BellComponent)component);
	}

	private void _Build(ComponentBuilderContext ctx, BellComponent component)
	{
		string name = ((ctx.GameObject.GetComponentInParent<Car>()?.CarType == "LD") ? "bell-diesel" : "bell-steam");
		Bell bell = ctx.InstantiatePrefab<Bell>(name, ctx.GameObject.transform);
		bell.player.mixerGroup = AudioController.Group.LocomotiveBell;
		bell.animationClip = ctx.Resolve(component.Animation);
		bell.animator = ctx.AnimatorGameObject.GetComponent<Animator>();
	}
}
