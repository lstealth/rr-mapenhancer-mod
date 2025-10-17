using System;
using Map.Runtime.MapModifiers;
using Map.Runtime.MaskComponents;
using Model.Definition;
using Model.Definition.Components.MapMasks;

namespace Model.ComponentBuilders;

[ComponentBuilder]
public class MapMaskComponentBuilder : IComponentBuilder
{
	public Type ComponentType => typeof(BaseMapMaskComponent);

	public void Build(ComponentBuilderContext ctx, Component component)
	{
		_Build(ctx, (BaseMapMaskComponent)component);
	}

	private void _Build(ComponentBuilderContext ctx, BaseMapMaskComponent component)
	{
		MapMaskBase mapMaskBase;
		if (!(component is CircleMapMaskComponent))
		{
			if (!(component is RectangleMapMaskComponent rectangleMapMaskComponent))
			{
				throw new ArgumentException("Unexpected component type");
			}
			RectangleMapMask rectangleMapMask = ctx.GameObject.AddComponent<RectangleMapMask>();
			rectangleMapMask.sizeX = rectangleMapMaskComponent.Size.x;
			rectangleMapMask.sizeZ = rectangleMapMaskComponent.Size.y;
			mapMaskBase = rectangleMapMask;
		}
		else
		{
			mapMaskBase = ctx.GameObject.AddComponent<CircleMapMask>();
		}
		mapMaskBase.order = component.Order;
		mapMaskBase.radius = component.Radius;
		mapMaskBase.falloff = component.Falloff;
		mapMaskBase.enableMaskModifier = component.EnableObjectMask;
		mapMaskBase.maskName = MaskName.Object;
		mapMaskBase.enableSetHeight = component.EnableSetHeight;
		mapMaskBase.enableCutTrees = component.EnableCutTrees;
	}
}
