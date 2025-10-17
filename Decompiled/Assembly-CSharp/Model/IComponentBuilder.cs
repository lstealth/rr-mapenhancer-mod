using System;
using Model.Definition;

namespace Model;

public interface IComponentBuilder
{
	Type ComponentType { get; }

	void Build(ComponentBuilderContext ctx, Component component);
}
