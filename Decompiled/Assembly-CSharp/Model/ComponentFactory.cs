using System;
using System.Collections.Generic;
using Model.ComponentBuilders;
using Model.Definition;
using Serilog;

namespace Model;

public static class ComponentFactory
{
	private static Dictionary<Type, IComponentBuilder> _builders;

	private static void PrepareBuildersIfNeeded()
	{
		if (_builders != null)
		{
			return;
		}
		_builders = new Dictionary<Type, IComponentBuilder>();
		Type[] types = typeof(ComponentFactory).Assembly.GetTypes();
		foreach (Type type in types)
		{
			if (type.GetCustomAttributes(typeof(ComponentBuilderAttribute), inherit: true).Length != 0)
			{
				Register((IComponentBuilder)Activator.CreateInstance(type));
			}
		}
		static void Register(IComponentBuilder builder)
		{
			_builders[builder.ComponentType] = builder;
		}
	}

	public static void BuildComponent(Component component, ComponentBuilderContext ctx)
	{
		PrepareBuildersIfNeeded();
		Type type = component.GetType();
		if (_builders.TryGetValue(type, out var value))
		{
			value.Build(ctx, component);
		}
		else if (type.BaseType != null && _builders.TryGetValue(type.BaseType, out value))
		{
			value.Build(ctx, component);
		}
		else
		{
			Log.Warning("No builder for {type}", type);
		}
	}
}
