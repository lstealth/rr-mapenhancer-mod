using System.Linq;
using TMPro;
using UI.LazyScrollList;
using UnityEngine;
using UnityEngine.UI;

namespace UI.QuickSearch;

public class QuickSearchResultRow : MonoBehaviour, ILazyScrollListCell
{
	[SerializeField]
	private Image highlightedImage;

	[SerializeField]
	private TMP_Text label;

	[SerializeField]
	private TMP_Text smallLabel;

	private static QuickSearchAction[] Actions = new QuickSearchAction[2]
	{
		QuickSearchAction.Inspect,
		QuickSearchAction.JumpTo
	};

	public int ListIndex { get; private set; }

	public RectTransform RectTransform => GetComponent<RectTransform>();

	public void Configure(int listIndex, object data)
	{
		ListIndex = listIndex;
		QuickSearchRow quickSearchRow = (QuickSearchRow)data;
		QuickSearchItem item = quickSearchRow.Result.Item;
		bool highlighted = quickSearchRow.Highlighted;
		string text = item.Type switch
		{
			QuickSearchItemType.PointOfInterest => "Point of Interest", 
			QuickSearchItemType.Location => "Location", 
			QuickSearchItemType.Player => "Player", 
			QuickSearchItemType.Equipment => "Equipment", 
			_ => item.Type.ToString(), 
		};
		label.text = item.Name + " <size=60%>" + text + "</size>";
		if (highlighted)
		{
			string text2 = string.Join(", ", from action in Actions
				where (item.Actions & action) != 0
				select action switch
				{
					QuickSearchAction.Inspect => "[" + QuickSearchController.BindingDisplayStringActivate + "]: Inspect", 
					QuickSearchAction.JumpTo => "[" + QuickSearchController.BindingDisplayStringJumpTo + "]: Jump To", 
					_ => "Unknown", 
				});
			smallLabel.text = text2;
		}
		else
		{
			smallLabel.text = null;
		}
		highlightedImage.enabled = highlighted;
	}
}
