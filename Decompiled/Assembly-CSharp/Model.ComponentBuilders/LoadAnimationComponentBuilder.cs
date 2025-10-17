using System;
using System.Linq;
using Model.Definition;
using Model.Definition.Components;
using RollingStock.Controls;
using UnityEngine;

namespace Model.ComponentBuilders;

[ComponentBuilder]
public class LoadAnimationComponentBuilder : IComponentBuilder
{
	public Type ComponentType => typeof(LoadAnimationComponent);

	public void Build(ComponentBuilderContext ctx, Model.Definition.Component component)
	{
		_Build(ctx, (LoadAnimationComponent)component);
	}

	private void _Build(ComponentBuilderContext ctx, LoadAnimationComponent component)
	{
		CarLoadAnimator carLoadAnimator = ctx.GameObject.AddComponent<CarLoadAnimator>();
		carLoadAnimator.animator = ctx.AnimatorGameObject.GetComponent<Animator>();
		carLoadAnimator.animationClip = ctx.Resolve(component.Animation);
		carLoadAnimator.carLoadGameObject = ctx.Resolve(component.LoadObject)?.gameObject;
		carLoadAnimator.slot = component.SlotIndex;
		carLoadAnimator.loadIdentifiers = component.LoadIdentifier.Split(",").ToList();
	}
}
