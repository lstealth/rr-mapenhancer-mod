using System;
using Game.Messages;
using KeyValue.Runtime;
using Model.Definition;
using Model.Definition.Components;
using RollingStock;
using UnityEngine;

namespace Model.ComponentBuilders;

[ComponentBuilder]
public class DerailedEffectComponentBuilder : IComponentBuilder
{
	public Type ComponentType => typeof(DerailedEffectComponent);

	public void Build(ComponentBuilderContext ctx, Model.Definition.Component component)
	{
		_Build(ctx, (DerailedEffectComponent)component);
	}

	private void _Build(ComponentBuilderContext ctx, DerailedEffectComponent component)
	{
		DerailedParticleController pc0 = InstantiatePrefab();
		DerailedParticleController pc1 = InstantiatePrefab();
		float num = component.Separation / 2f;
		pc0.transform.localPosition = num * Vector3.forward;
		pc1.transform.localPosition = num * Vector3.back;
		ctx.ObserveProperty(PropertyChange.Control.Derailment, delegate(Value value)
		{
			float derailment = Mathf.Abs(value.FloatValue);
			pc0.Derailment = derailment;
			pc1.Derailment = derailment;
		});
		DerailedParticleController InstantiatePrefab()
		{
			return ctx.InstantiatePrefab<DerailedParticleController>("derailment-particles", ctx.GameObject.transform);
		}
	}
}
