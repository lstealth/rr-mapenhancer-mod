using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Game;
using Game.DailyReport;
using Game.Events;
using Game.Messages;
using Game.State;
using UI.Builder;
using UnityEngine;

namespace UI.CompanyWindow;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct FinancePanelBuilder
{
	private static float _lastRequestedEntries;

	public static void Build(UIPanelBuilder builder)
	{
		StateManager stateManager = StateManager.Shared;
		builder.VStack(delegate(UIPanelBuilder builder2)
		{
			builder2.RebuildOnEvent<BalanceDidChange>();
			builder2.HStack(delegate(UIPanelBuilder builder3)
			{
				builder3.AddField("Balance", stateManager.GetBalance().ToString("C0")).Width(300f);
				BuildLoanSection(builder3, stateManager);
			});
			builder2.Spacer(8f);
			BuildLedgerScrollContent(builder2);
		});
	}

	private static void BuildLoanSection(UIPanelBuilder builder, StateManager stateManager)
	{
		LoanManager loanManager = stateManager.LoanManager;
		if (loanManager == null)
		{
			return;
		}
		builder.VStack(delegate(UIPanelBuilder uIPanelBuilder)
		{
			int loanAmount = loanManager.LoanAmount;
			int approvedAmount = loanManager.ApprovedLoanAmount();
			uIPanelBuilder.AddField("Current Loan", $"{loanAmount:C0} (max {approvedAmount:C0})");
			var (num, arg) = loanManager.NextInterestPaymentInfo();
			if (num > 0)
			{
				uIPanelBuilder.AddField("Interest Payment", $"{num:C0} in {arg} (10%)");
			}
			if (LoanManager.CanRequestLoanChange)
			{
				uIPanelBuilder.ButtonStrip(delegate
				{
					int approvedIncrease = approvedAmount - loanAmount;
					AddLoanButton((approvedIncrease >= 5000 || approvedIncrease <= 1000) ? 5000 : approvedIncrease);
					AddLoanButton((approvedIncrease >= 1000 || approvedIncrease == 0) ? 1000 : approvedIncrease);
					AddPayButton((loanAmount >= 1000 || loanAmount == 0) ? 1000 : loanAmount);
					AddPayButton((loanAmount >= 5000 || loanAmount <= 1000) ? 5000 : loanAmount);
				});
			}
			void AddPayButton(int increment)
			{
				P_1.builder.AddButtonCompact($"Pay {increment:C0}", delegate
				{
					loanManager.RequestLoanDelta(-increment);
				}).Disable(loanAmount < increment);
			}
		});
		void AddLoanButton(int increment)
		{
			P_1.builder.AddButtonCompact($"Loan {increment:C0}", delegate
			{
				loanManager.RequestLoanDelta(increment);
			}).Disable(P_1.approvedIncrease < increment);
		}
	}

	private static void BuildLedgerScrollContent(UIPanelBuilder builder)
	{
		builder.RebuildOnEvent<LedgerRequestResponseReceived>();
		GameDateTime end = TimeWeather.Now.AddingSeconds(1f);
		int startBalance;
		int endBalance;
		IOrderedEnumerable<Ledger.Entry> orderedEnumerable = from e in GroupEntries(RequestLedgerEntries(end.AddingDays(-2f).WithHours(0f), end, out startBalance, out endBalance))
			orderby e.Date.TotalSeconds descending
			select e;
		int num = -1;
		int num2 = endBalance;
		List<object> list = new List<object>();
		foreach (Ledger.Entry item in orderedEnumerable)
		{
			string category = DailyReportGenerator.StringForCategory(item.Category);
			object obj = item.Payee?.Text();
			string memo = item.Memo;
			if (obj == null)
			{
				obj = memo;
			}
			string payeeMemo = (string)obj;
			string s = num2.ToString("C0");
			if (item.Date.Day != num)
			{
				list.Add(new LedgerRow.Info(item.Date.DayString()));
				num = item.Date.Day;
			}
			list.Add(new LedgerRow.Info(Mono(item.Date.TimeString()), category, payeeMemo, Mono(item.Amount.ToString("C0")), Mono(s)));
			num2 -= item.Amount;
		}
		builder.LazyScrollList(list, "ledger");
		static string Mono(string text)
		{
			return "<style=\"LedgerMono\">" + text + "</style>";
		}
	}

	private static List<Ledger.Entry> GroupEntries(IReadOnlyList<Ledger.Entry> rawEntries)
	{
		List<Ledger.Entry> list = new List<Ledger.Entry>();
		string text = "";
		Ledger.Entry? entry = null;
		foreach (Ledger.Entry rawEntry in rawEntries)
		{
			string text2 = KeyForEntry(rawEntry);
			if (text2 == text && entry.HasValue && rawEntry.Date.TotalSeconds - entry.Value.Date.TotalSeconds < 120.0 && rawEntry.Category != Ledger.Category.Equipment)
			{
				Ledger.Entry value = entry.Value;
				value.Amount += rawEntry.Amount;
				value.Count += rawEntry.Amount;
				entry = value;
				continue;
			}
			if (entry.HasValue)
			{
				list.Add(entry.Value);
				entry = null;
			}
			entry = rawEntry;
			text = text2;
		}
		if (entry.HasValue)
		{
			list.Add(entry.Value);
		}
		return list;
		static string KeyForEntry(Ledger.Entry e)
		{
			return $"{e.Category}-{e.Payee?.Id}-{e.Memo}";
		}
	}

	private static IReadOnlyList<Ledger.Entry> RequestLedgerEntries(GameDateTime start, GameDateTime end, out int startBalance, out int endBalance)
	{
		StateManager shared = StateManager.Shared;
		float unscaledTime = Time.unscaledTime;
		bool flag = _lastRequestedEntries < unscaledTime - 5f;
		if (!StateManager.IsHost && flag)
		{
			StateManager.ApplyLocal(new LedgerRequest((float)start.TotalSeconds, (float)end.TotalSeconds));
			_lastRequestedEntries = unscaledTime;
		}
		return shared.Ledger.EntriesBetween(start, end, out startBalance, out endBalance);
	}
}
