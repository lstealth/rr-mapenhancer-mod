namespace UI.QuickSearch;

public readonly struct QuickSearchItem
{
	public readonly string Name;

	public readonly QuickSearchItemType Type;

	public readonly string SearchString;

	public readonly object Object;

	public readonly QuickSearchAction Actions;

	public QuickSearchItem(string name, QuickSearchItemType type, object o, QuickSearchAction actions)
	{
		Name = name;
		Type = type;
		Object = o;
		SearchString = name.ToLower();
		Actions = actions;
	}
}
