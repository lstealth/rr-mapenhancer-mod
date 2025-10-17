using System;
using Map.Runtime.MaskComponents;
using Model.Definition;
using Model.Definition.Components;

namespace Model.ComponentBuilders;

[ComponentBuilder]
public class LegacyMapMaskComponentBuilder : IComponentBuilder
{
	public Type ComponentType => typeof(LegacyMapMaskComponent);

	public void Build(ComponentBuilderContext ctx, Component component)
	{
		_Build(ctx, (LegacyMapMaskComponent)component);
	}

	private void _Build(ComponentBuilderContext ctx, LegacyMapMaskComponent component)
	{
		RectangleMapMask rectangleMapMask = ctx.GameObject.AddComponent<RectangleMapMask>();
		rectangleMapMask.order = component.Order;
		rectangleMapMask.sizeX = component.DimensionA;
		rectangleMapMask.sizeZ = component.DimensionB;
		rectangleMapMask.radius = component.Radius;
		rectangleMapMask.falloff = component.Falloff;
		rectangleMapMask.enableMaskModifier = true;
		rectangleMapMask.enableSetHeight = true;
	}
}
