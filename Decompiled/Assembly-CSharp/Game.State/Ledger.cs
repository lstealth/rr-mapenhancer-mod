using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using GalaSoft.MvvmLight.Messaging;
using Serilog;

namespace Game.State;

public class Ledger
{
	public enum Category
	{
		Bank,
		Freight,
		Passenger,
		Fuel,
		Loan,
		Equipment,
		WagesRepair,
		Progression,
		WagesAI,
		RepairSupplies
	}

	public struct Entry
	{
		public GameDateTime Date { get; set; }

		public int Amount { get; set; }

		public Category Category { get; set; }

		public EntityReference? Payee { get; set; }

		public string Memo { get; set; }

		public int Count { get; set; }

		public Entry(GameDateTime date, int amount, Category category, EntityReference? payee, string memo, int count)
		{
			Date = date;
			Amount = amount;
			Category = category;
			Payee = payee;
			Memo = memo;
			Count = count;
		}

		public Entry(SerializableLedgerEntry e)
		{
			Date = new GameDateTime(e.Date);
			Amount = e.Amount;
			Category = e.Category;
			Payee = (e.Payee.HasValue ? new EntityReference?(new EntityReference(e.Payee.Value)) : ((EntityReference?)null));
			Memo = e.Memo;
			Count = e.Count;
		}
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct ChangedEvent
	{
	}

	private readonly List<Entry> _entries = new List<Entry>();

	private int _startingBalance;

	public void Record(int amount, Category category, EntityReference? payee, string memo, int count, GameDateTime now)
	{
		StateManager.AssertIsHost();
		_entries.Add(new Entry(now, amount, category, payee, memo, count));
		Messenger.Default.Send(default(ChangedEvent));
	}

	public void Clear()
	{
		_entries.Clear();
		Messenger.Default.Send(default(ChangedEvent));
	}

	public IReadOnlyList<Entry> EntriesBetween(GameDateTime start, GameDateTime end, out int startBalance, out int endBalance)
	{
		double totalSeconds = start.TotalSeconds;
		double totalSeconds2 = end.TotalSeconds;
		List<Entry> list = new List<Entry>();
		int num = (startBalance = _startingBalance);
		endBalance = 0;
		if (_entries.Count == 0)
		{
			return list;
		}
		foreach (Entry entry in _entries)
		{
			num += entry.Amount;
			double totalSeconds3 = entry.Date.TotalSeconds;
			if (totalSeconds3 >= totalSeconds && totalSeconds3 < totalSeconds2)
			{
				list.Add(entry);
				endBalance = num;
			}
			if (totalSeconds3 < totalSeconds)
			{
				startBalance = num;
			}
		}
		if (list.Count == 0)
		{
			endBalance = num;
		}
		return list;
	}

	public void PopulateForSave(List<SerializableLedgerEntry> entries)
	{
		entries.Clear();
		entries.AddRange(_entries.Select((Entry e) => new SerializableLedgerEntry(e)));
	}

	public void Load(List<SerializableLedgerEntry> entries, int startingBalance = 0)
	{
		if (entries == null)
		{
			entries = new List<SerializableLedgerEntry>();
		}
		_entries.Clear();
		_startingBalance = startingBalance;
		_entries.AddRange(entries.Select((SerializableLedgerEntry e) => new Entry(e)));
		Log.Information("Loaded {count} ledger entries.", _entries.Count);
	}

	public void ReconcileIfNeeded(int expectedBalance)
	{
		int num = _entries.Sum((Entry e) => e.Amount) + _startingBalance;
		if (num != expectedBalance)
		{
			Log.Information("Adding initial balance entry to ledger; {actual} vs {expected}", num, expectedBalance);
			_entries.Add(new Entry(GameDateTime.Zero, expectedBalance - num, Category.Bank, null, "Balance Correction", 0));
		}
	}
}
