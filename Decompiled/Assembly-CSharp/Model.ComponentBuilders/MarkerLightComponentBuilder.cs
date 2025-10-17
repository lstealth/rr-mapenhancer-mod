using System;
using Effects;
using Model.Definition;
using Model.Definition.Components;
using UnityEngine;

namespace Model.ComponentBuilders;

[ComponentBuilder]
public class MarkerLightComponentBuilder : IComponentBuilder
{
	public Type ComponentType => typeof(MarkerLightComponent);

	public void Build(ComponentBuilderContext ctx, Model.Definition.Component component)
	{
		_Build(ctx, (MarkerLightComponent)component);
	}

	private void _Build(ComponentBuilderContext ctx, MarkerLightComponent component)
	{
		bool flag = component.End == MarkerLightComponent.CarEnd.Front;
		string text = (flag ? "F" : "R");
		MarkerLamp markerLamp = InstantiatePrefab("marker-lamp", "Marker Lamp L" + text);
		MarkerLamp markerLamp2 = InstantiatePrefab("marker-lamp", "Marker Lamp R" + text);
		float num = component.Radius / ctx.GameObject.transform.localScale.x;
		markerLamp.transform.localPosition = new Vector3(0f - num, 0f, 0f);
		markerLamp2.transform.localPosition = new Vector3(num, 0f, 0f);
		if (flag)
		{
			markerLamp.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
			markerLamp2.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
		}
		else
		{
			markerLamp.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
			markerLamp2.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
		}
		markerLamp.transform.localScale = new Vector3(-1f, 1f, 1f);
		MarkerLampToggle componentInChildren = markerLamp2.GetComponentInChildren<MarkerLampToggle>();
		MarkerLampToggle componentInChildren2 = markerLamp.GetComponentInChildren<MarkerLampToggle>();
		string text2 = (flag ? "f" : "r");
		componentInChildren.keyBase = "marker-" + text2;
		componentInChildren2.keyBase = "marker-" + text2;
		MarkerLamp InstantiatePrefab(string prefabName, string objectName)
		{
			MarkerLamp markerLamp3 = ctx.InstantiatePrefab<MarkerLamp>(prefabName, ctx.GameObject.transform);
			markerLamp3.name = objectName;
			return markerLamp3;
		}
	}
}
