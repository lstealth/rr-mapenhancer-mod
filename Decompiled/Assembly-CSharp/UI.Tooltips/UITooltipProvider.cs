using System;
using UnityEngine;

namespace UI.Tooltips;

public class UITooltipProvider : MonoBehaviour
{
	public Func<TooltipInfo> DynamicTooltipInfo;

	[SerializeField]
	private string tooltipTitle;

	[SerializeField]
	private string tooltipText;

	public TooltipInfo TooltipInfo
	{
		get
		{
			if (DynamicTooltipInfo == null)
			{
				return new TooltipInfo(tooltipTitle, tooltipText);
			}
			return DynamicTooltipInfo();
		}
		set
		{
			tooltipTitle = value.Title;
			tooltipText = value.Text;
		}
	}

	public string Title
	{
		get
		{
			return tooltipTitle;
		}
		set
		{
			tooltipTitle = value;
		}
	}
}
