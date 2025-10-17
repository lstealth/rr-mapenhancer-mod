using System;
using Effects;
using Model.Definition;
using Model.Definition.Components;

namespace Model.ComponentBuilders;

[ComponentBuilder]
public class CylinderCockComponentBuilder : IComponentBuilder
{
	public Type ComponentType => typeof(CylinderCockComponent);

	public void Build(ComponentBuilderContext ctx, Component component)
	{
		_Build(ctx, (CylinderCockComponent)component);
	}

	private void _Build(ComponentBuilderContext ctx, CylinderCockComponent component)
	{
		ctx.InstantiatePrefab<CylinderCockController>("cyl-cock", ctx.GameObject.transform).Configure(component.Radius, component.ForwardOffset);
	}
}
