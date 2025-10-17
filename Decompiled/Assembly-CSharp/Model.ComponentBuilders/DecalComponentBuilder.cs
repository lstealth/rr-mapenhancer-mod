using System;
using Effects.Decals;
using Game.State;
using Helpers;
using KeyValue.Runtime;
using Model.Definition;
using Model.Definition.Components;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Model.ComponentBuilders;

[ComponentBuilder]
public class DecalComponentBuilder : IComponentBuilder
{
	public Type ComponentType => typeof(DecalComponent);

	public void Build(ComponentBuilderContext ctx, Model.Definition.Component component)
	{
		_Build(ctx, (DecalComponent)component);
	}

	private void _Build(ComponentBuilderContext ctx, DecalComponent component)
	{
		DecalProjector decalProjector = ctx.GameObject.AddComponent<DecalProjector>();
		decalProjector.size = component.Size;
		decalProjector.pivot = Vector3.zero;
		decalProjector.drawDistance = Mathf.Max(600f, Mathf.Max(component.Size.x, component.Size.y) * 100f);
		DecalProjectorHelper helper = ctx.GameObject.AddComponent<DecalProjectorHelper>();
		helper.decalRenderer = CanvasDecalRenderer.Shared;
		DecalProjectorHelper decalProjectorHelper = helper;
		decalProjectorHelper.templateName = component.Content switch
		{
			DecalContent.RoadNumber => "Number", 
			DecalContent.Lettering => "Tender", 
			_ => throw new ArgumentOutOfRangeException(), 
		};
		if (!string.IsNullOrEmpty(component.ForceColor))
		{
			Color? color = ColorHelper.ColorFromHex(component.ForceColor);
			if (color.HasValue)
			{
				helper.ForceColor(color.Value);
			}
		}
		Car car = ctx.AnimatorGameObject.GetComponentInParent<Car>();
		switch (component.Content)
		{
		case DecalContent.RoadNumber:
		{
			string text = car.Ident.RoadNumber ?? "";
			if (car.Archetype == CarArchetype.Tender && text.EndsWith("T"))
			{
				text = text.Remove(text.Length - 1);
			}
			helper.text = text;
			break;
		}
		case DecalContent.Lettering:
			ctx.ObserveProperty("lettering.basic", delegate(Value value)
			{
				if (string.IsNullOrEmpty(value.StringValue))
				{
					helper.text = (car.Archetype.IsFreight() ? car.Ident.ReportingMark : StateManager.Shared.RailroadName);
				}
				else
				{
					helper.text = value.StringValue;
				}
				helper.RenderDecal();
			});
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}
}
