namespace UI.QuickSearch;

public struct QuickSearchResult
{
	public readonly int Score;

	public QuickSearchItem Item;

	public QuickSearchResult(int score, QuickSearchItem item)
	{
		Score = score;
		Item = item;
	}
}
