using System;
using Model.Definition;
using Model.Definition.Components;
using RollingStock;

namespace Model.ComponentBuilders;

[ComponentBuilder]
public class GaugeComponentBuilder : IComponentBuilder
{
	public Type ComponentType => typeof(GaugeComponent);

	public void Build(ComponentBuilderContext ctx, Component component)
	{
		_Build(ctx, (GaugeComponent)component);
	}

	private void _Build(ComponentBuilderContext ctx, GaugeComponent component)
	{
		switch (component.Style)
		{
		case GaugeStyle.BoilerPressure:
			InstantiatePrefab("gauge-300").RenameId("default", "boiler");
			break;
		case GaugeStyle.DualBrakeCylinderLine:
			InstantiatePrefab("gauge-brake");
			break;
		case GaugeStyle.DualReservoirMainEq:
			InstantiatePrefab("gauge-res");
			break;
		case GaugeStyle.Quadruplex:
			InstantiatePrefab("gauge-quad");
			break;
		case GaugeStyle.Speedometer100:
			InstantiatePrefab("gauge-speedometer-100");
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
		GaugeController InstantiatePrefab(string prefabName)
		{
			return ctx.InstantiatePrefab<GaugeController>(prefabName, ctx.GameObject.transform);
		}
	}
}
