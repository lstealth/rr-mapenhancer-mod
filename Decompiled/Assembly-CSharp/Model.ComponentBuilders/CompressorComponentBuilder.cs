using System;
using Audio;
using Game.Messages;
using KeyValue.Runtime;
using Model.Definition;
using Model.Definition.Components;
using RollingStock.Controls;

namespace Model.ComponentBuilders;

[ComponentBuilder]
public class CompressorComponentBuilder : IComponentBuilder
{
	public Type ComponentType => typeof(CompressorComponent);

	public void Build(ComponentBuilderContext ctx, Component component)
	{
		_Build(ctx, (CompressorComponent)component);
	}

	private void _Build(ComponentBuilderContext ctx, CompressorComponent component)
	{
		switch (component.Style)
		{
		case CompressorStyle.Steam:
		{
			IntegerLoopingPlayer player = ctx.InstantiatePrefab<IntegerLoopingPlayer>("compressor-steam", ctx.GameObject.transform);
			player.AudioSourceName = "CompressorSteam";
			player.priority = 11;
			player.mixerGroup = AudioController.Group.LocomotiveCompressor;
			player.PrepareSources();
			ctx.ObserveProperty(PropertyChange.Control.Compressor, delegate(Value value)
			{
				player.play = value.BoolValue;
			});
			break;
		}
		}
		if (component.Animation != null && ctx.TryResolve(component.Animation, out var animationClip))
		{
			KeyValueBoolAnimator keyValueBoolAnimator = ctx.AnimatorGameObject.AddComponent<KeyValueBoolAnimator>();
			keyValueBoolAnimator.animationClip = animationClip;
			keyValueBoolAnimator.speed = component.AnimationSpeed;
			keyValueBoolAnimator.key = PropertyChange.KeyForControl(PropertyChange.Control.Compressor);
		}
	}
}
