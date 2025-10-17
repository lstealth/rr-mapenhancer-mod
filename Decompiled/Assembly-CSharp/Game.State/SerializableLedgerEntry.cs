using MessagePack;

namespace Game.State;

[MessagePackObject(false)]
public struct SerializableLedgerEntry
{
	[Key("date")]
	public int Date;

	[Key("amt")]
	public int Amount;

	[Key("cat")]
	public Ledger.Category Category;

	[Key("payee")]
	public SerializableEntityReference? Payee;

	[Key("memo")]
	public string Memo;

	[Key("count")]
	public int Count { get; set; }

	public SerializableLedgerEntry(Ledger.Entry entry)
	{
		Date = (int)entry.Date.TotalSeconds;
		Amount = entry.Amount;
		Category = entry.Category;
		Payee = (entry.Payee.HasValue ? new SerializableEntityReference?(new SerializableEntityReference(entry.Payee.Value)) : ((SerializableEntityReference?)null));
		Memo = entry.Memo;
		Count = entry.Count;
	}

	public SerializableLedgerEntry(int date, int amount, Ledger.Category category, SerializableEntityReference? payee, string memo, int count)
	{
		Date = date;
		Amount = amount;
		Category = category;
		Payee = payee;
		Memo = memo;
		Count = count;
	}
}
