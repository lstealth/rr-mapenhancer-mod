using System;
using Helpers;
using Model.Definition;
using Model.Definition.Components;
using RollingStock.ContinuousControls;
using UnityEngine;

namespace Model.ComponentBuilders;

[ComponentBuilder]
public class RadialControlComponentBuilder : IComponentBuilder
{
	public Type ComponentType => typeof(RadialControlComponent);

	public void Build(ComponentBuilderContext ctx, Model.Definition.Component component)
	{
		_Build(ctx, (RadialControlComponent)component);
	}

	private void _Build(ComponentBuilderContext ctx, RadialControlComponent component)
	{
		_BuildCollider(ctx.GameObject, component.Collider).isTrigger = true;
		RadialAnimatedControl radialAnimatedControl = ctx.GameObject.AddComponent<RadialAnimatedControl>();
		radialAnimatedControl.displayName = component.DisplayName;
		radialAnimatedControl.animator = ctx.AnimatorGameObject.GetComponent<Animator>();
		radialAnimatedControl.radius = component.Radius;
		radialAnimatedControl.rotationExtent = component.RotationDegrees;
		radialAnimatedControl.momentary = component.Momentary;
		radialAnimatedControl.homePosition = component.HomePosition;
		radialAnimatedControl.animationClip = ctx.Resolve(component.Animation);
		radialAnimatedControl.rotationAxis = RadialAnimatedControl.Axis.Z;
		radialAnimatedControl.handleAxis = RadialAnimatedControl.Axis.X;
		radialAnimatedControl.ControlComponentPurpose = component.Purpose;
	}

	private static Collider _BuildCollider(GameObject gameObject, ColliderDescriptor descriptor)
	{
		if (descriptor is CapsuleColliderDescriptor capsuleColliderDescriptor)
		{
			CapsuleCollider capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
			capsuleCollider.gameObject.layer = Layers.Clickable;
			capsuleCollider.center = capsuleColliderDescriptor.Center;
			capsuleCollider.direction = (int)capsuleColliderDescriptor.Axis;
			capsuleCollider.radius = capsuleColliderDescriptor.Radius;
			capsuleCollider.height = capsuleColliderDescriptor.Height;
			return capsuleCollider;
		}
		throw new ArgumentException("Unknown collider type");
	}
}
