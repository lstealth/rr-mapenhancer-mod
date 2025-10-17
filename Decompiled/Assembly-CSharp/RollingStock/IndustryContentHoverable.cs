using System.Text;
using Model.Ops;
using Model.Ops.Definition;
using UnityEngine;

namespace RollingStock;

public class IndustryContentHoverable : MonoBehaviour, IPickable
{
	[SerializeField]
	private string tooltipTitle;

	[SerializeField]
	private Industry industry;

	[Tooltip("If non-null, only this load is shown.")]
	[SerializeField]
	private Load filterLoad;

	public float MaxPickDistance => 40f;

	public int Priority => 0;

	public TooltipInfo TooltipInfo => new TooltipInfo(string.IsNullOrEmpty(tooltipTitle) ? base.name : tooltipTitle, TooltipText());

	public PickableActivationFilter ActivationFilter => PickableActivationFilter.PrimaryOnly;

	private string TooltipText()
	{
		if (industry == null)
		{
			return "<Not Configured>";
		}
		IndustryStorageHelper storage = industry.Storage;
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = false;
		foreach (Load item in storage.Loads())
		{
			if (!(filterLoad != null) || !(item != filterLoad))
			{
				if (flag)
				{
					stringBuilder.Append(", ");
				}
				float quantity = storage.QuantityInStorage(item);
				if (industry.TryGetStorageCapacity(item, out var capacity))
				{
					stringBuilder.Append(TextSprites.PiePercent(quantity, capacity));
					stringBuilder.Append(" ");
				}
				stringBuilder.Append(item.QuantityString(quantity));
				flag = true;
			}
		}
		if (!flag)
		{
			return "Empty";
		}
		return stringBuilder.ToString();
	}

	public void Activate(PickableActivateEvent evt)
	{
	}

	public void Deactivate()
	{
	}
}
