using System;
using Effects;
using Model.Definition;
using Model.Definition.Components;
using RollingStock.Controls;
using UnityEngine;

namespace Model.ComponentBuilders;

[ComponentBuilder]
public class LightFixtureComponentBuilder : IComponentBuilder
{
	public Type ComponentType => typeof(LightFixtureComponent);

	public void Build(ComponentBuilderContext ctx, Model.Definition.Component component)
	{
		_Build(ctx, (LightFixtureComponent)component);
	}

	private void _Build(ComponentBuilderContext ctx, LightFixtureComponent component)
	{
		UnityEngine.Component component2 = ctx.InstantiatePrefab<UnityEngine.Component>("light-cab", ctx.GameObject.transform);
		KeyValuePickableToggle componentInChildren = component2.GetComponentInChildren<KeyValuePickableToggle>();
		component2.GetComponentInChildren<BulbController>().onOffKey = (componentInChildren.key = (string.IsNullOrEmpty(component.Key) ? "lamp" : component.Key));
	}
}
