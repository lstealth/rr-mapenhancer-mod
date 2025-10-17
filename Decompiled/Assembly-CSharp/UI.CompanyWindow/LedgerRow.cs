using TMPro;
using UI.LazyScrollList;
using UnityEngine;

namespace UI.CompanyWindow;

public class LedgerRow : MonoBehaviour, ILazyScrollListCell
{
	public enum RowType
	{
		Divider,
		Entry
	}

	public class Info
	{
		public RowType Type;

		public string Date;

		public string Category;

		public string PayeeMemo;

		public string Amount;

		public string Balance;

		public float ExtraTrailing;

		public Info(string date, string category, string payeeMemo, string amount, string balance)
		{
			Type = RowType.Entry;
			Date = date;
			Category = category;
			PayeeMemo = payeeMemo;
			Amount = amount;
			Balance = balance;
		}

		public Info(string date)
			: this(date, null, null, null, null)
		{
			Type = RowType.Divider;
		}
	}

	[SerializeField]
	private TMP_Text dateLabel;

	[SerializeField]
	private TMP_Text categoryLabel;

	[SerializeField]
	private TMP_Text payeeMemoLabel;

	[SerializeField]
	private TMP_Text amountLabel;

	[SerializeField]
	private TMP_Text balanceLabel;

	private Info _info;

	public int ListIndex { get; private set; }

	public RectTransform RectTransform => GetComponent<RectTransform>();

	public void Configure(int listIndex, object obj)
	{
		ListIndex = listIndex;
		_info = (Info)obj;
		Configure();
	}

	private void Configure()
	{
		dateLabel.alignment = ((_info.Type == RowType.Divider) ? TextAlignmentOptions.CaplineLeft : TextAlignmentOptions.CaplineRight);
		dateLabel.text = _info.Date;
		categoryLabel.text = _info.Category;
		payeeMemoLabel.text = _info.PayeeMemo;
		amountLabel.text = _info.Amount;
		balanceLabel.text = _info.Balance;
	}
}
