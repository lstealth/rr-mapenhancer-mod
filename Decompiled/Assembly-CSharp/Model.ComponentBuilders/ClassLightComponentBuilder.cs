using System;
using Effects;
using Model.Definition;
using Model.Definition.Components;
using UnityEngine;

namespace Model.ComponentBuilders;

[ComponentBuilder]
public class ClassLightComponentBuilder : IComponentBuilder
{
	public Type ComponentType => typeof(ClassLightComponent);

	public void Build(ComponentBuilderContext ctx, Model.Definition.Component component)
	{
		_Build(ctx, (ClassLightComponent)component);
	}

	private void _Build(ComponentBuilderContext ctx, ClassLightComponent component)
	{
		ClassLightToggle classLightToggle = InstantiatePrefab("class-light", "Class Light L");
		ClassLightToggle classLightToggle2 = InstantiatePrefab("class-light", "Class Light R");
		float num = component.Radius / ctx.GameObject.transform.localScale.x;
		classLightToggle.transform.localPosition = new Vector3(0f - num, 0f, 0f);
		classLightToggle2.transform.localPosition = new Vector3(num, 0f, 0f);
		classLightToggle.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
		classLightToggle2.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
		classLightToggle.transform.localScale = new Vector3(-1f, 1f, 1f);
		classLightToggle.keyBase = "classLight";
		classLightToggle2.keyBase = "classLight";
		ClassLightToggle InstantiatePrefab(string prefabName, string objectName)
		{
			ClassLightToggle classLightToggle3 = ctx.InstantiatePrefab<ClassLightToggle>(prefabName, ctx.GameObject.transform);
			classLightToggle3.name = objectName;
			return classLightToggle3;
		}
	}
}
