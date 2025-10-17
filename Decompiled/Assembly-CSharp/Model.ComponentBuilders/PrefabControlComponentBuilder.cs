using System;
using Model.Definition;
using Model.Definition.Components;
using UnityEngine;

namespace Model.ComponentBuilders;

[ComponentBuilder]
public class PrefabControlComponentBuilder : IComponentBuilder
{
	public Type ComponentType => typeof(PrefabControlComponent);

	public void Build(ComponentBuilderContext ctx, Model.Definition.Component component)
	{
		_Build(ctx, (PrefabControlComponent)component);
	}

	private void _Build(ComponentBuilderContext ctx, PrefabControlComponent component)
	{
		switch (component.Prefab)
		{
		case PrefabControlPrefab.BrakeStand26L:
			InstantiatePrefab("brakestand-26");
			break;
		case PrefabControlPrefab.BrakeStand6Train:
			InstantiatePrefab("brakestand-6train");
			break;
		case PrefabControlPrefab.BrakeStand6Locomotive:
			InstantiatePrefab("brakestand-6loco");
			break;
		case PrefabControlPrefab.BrakeStand6CutOut:
			InstantiatePrefab("brakestand-6cutout");
			break;
		case PrefabControlPrefab.HandbrakeWheel:
			ctx.InstantiatePrefab<UnityEngine.Component>("handbrake-wheel", ctx.GameObject.transform);
			break;
		case PrefabControlPrefab.SightGlass:
			ctx.InstantiatePrefab<UnityEngine.Component>("sight-glass", ctx.GameObject.transform);
			break;
		case PrefabControlPrefab.HandbrakeTender:
			ctx.InstantiatePrefab<UnityEngine.Component>("handbrake-tender", ctx.GameObject.transform);
			break;
		case PrefabControlPrefab.HandbrakeWheelFlatTop:
			ctx.InstantiatePrefab<UnityEngine.Component>("handbrake-wheelflattop", ctx.GameObject.transform);
			break;
		case PrefabControlPrefab.HeadlightControlBidirectionalDimming:
			InstantiatePrefab("headlight-control-bi-dim");
			break;
		case PrefabControlPrefab.HeadlightControlUnidirectional:
			InstantiatePrefab("headlight-control-uni");
			break;
		case PrefabControlPrefab.Tricocks01:
			InstantiatePrefab("tricocks01");
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
		UnityEngine.Component InstantiatePrefab(string prefabName)
		{
			return ctx.InstantiatePrefab<UnityEngine.Component>(prefabName, ctx.GameObject.transform);
		}
	}
}
