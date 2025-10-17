using System;
using Model.Definition;
using Model.Definition.Components;
using RollingStock.Controls;
using Serilog;

namespace Model.ComponentBuilders;

[ComponentBuilder]
public class ToggleAnimationComponentBuilder : IComponentBuilder
{
	public Type ComponentType => typeof(ToggleAnimationComponent);

	public void Build(ComponentBuilderContext ctx, Component component)
	{
		_Build(ctx, (ToggleAnimationComponent)component);
	}

	private void _Build(ComponentBuilderContext ctx, ToggleAnimationComponent component)
	{
		KeyValuePickableToggle keyValuePickableToggle = ctx.Resolve(component.TargetColliderObject).gameObject.AddComponent<KeyValuePickableToggle>();
		keyValuePickableToggle.key = component.Key;
		keyValuePickableToggle.displayTitle = component.Title;
		keyValuePickableToggle.displayMessageTrue = component.MessageTrue;
		keyValuePickableToggle.displayMessageFalse = component.MessageFalse;
		keyValuePickableToggle.maxPickDistance = 50f;
		if (!ctx.TryResolve(component.Animation, out var animationClip))
		{
			Log.Error("Couldn't resolve animation: {anim}", component.Animation);
		}
		KeyValueBoolAnimator keyValueBoolAnimator = ctx.AnimatorGameObject.AddComponent<KeyValueBoolAnimator>();
		keyValueBoolAnimator.animationClip = animationClip;
		keyValueBoolAnimator.key = component.Key;
		keyValueBoolAnimator.speed = component.Speed;
	}
}
