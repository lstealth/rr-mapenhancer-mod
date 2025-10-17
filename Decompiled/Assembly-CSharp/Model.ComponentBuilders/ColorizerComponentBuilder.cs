using System;
using System.Linq;
using Helpers;
using Model.Definition;
using Model.Definition.Components;
using RollingStock;
using Serilog;
using UnityEngine;

namespace Model.ComponentBuilders;

[ComponentBuilder]
public class ColorizerComponentBuilder : IComponentBuilder
{
	public Type ComponentType => typeof(ColorizerComponent);

	public void Build(ComponentBuilderContext ctx, Model.Definition.Component component)
	{
		_Build(ctx, (ColorizerComponent)component);
	}

	private void _Build(ComponentBuilderContext ctx, ColorizerComponent component)
	{
		Material material = ctx.Resolve(component.Material);
		if (material == null)
		{
			Log.Warning("{object}: ColorizerComponent: Material {materialName} not found", ctx.ObjectName, component.Material);
			return;
		}
		ctx.CarColorController.palette = component.HexColors.Select(HexColorToColor).ToList();
		ctx.GameObject.AddComponent<CarColorizer>().targetMaterials = new Material[1] { material };
	}

	private static Color HexColorToColor(string arg)
	{
		return ColorHelper.ColorFromHex(arg) ?? Color.gray;
	}
}
