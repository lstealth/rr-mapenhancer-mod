namespace UI.QuickSearch;

public struct QuickSearchRow
{
	public QuickSearchResult Result;

	public bool Highlighted;

	public QuickSearchRow(QuickSearchResult result, bool highlighted)
	{
		Result = result;
		Highlighted = highlighted;
	}
}
