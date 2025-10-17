using System;
using Model;

namespace UI.EngineRoster;

public readonly struct RosterRowData
{
	public readonly BaseLocomotive Engine;

	public readonly bool IsFavorite;

	public readonly bool IsSelected;

	public readonly EngineRosterPanel Parent;

	public RosterRowData(BaseLocomotive engine, bool isFavorite, bool isSelected, EngineRosterPanel parent)
	{
		Engine = engine;
		IsFavorite = isFavorite;
		IsSelected = isSelected;
		Parent = parent;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Engine, IsFavorite, IsSelected);
	}
}
