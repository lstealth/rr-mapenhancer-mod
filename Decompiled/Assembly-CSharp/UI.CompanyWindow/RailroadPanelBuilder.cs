using System.Runtime.InteropServices;
using Game.DailyReport;
using Game.Events;
using Game.Reputation;
using Game.State;
using Markroader;
using UI.Builder;
using UnityEngine;

namespace UI.CompanyWindow;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct RailroadPanelBuilder
{
	public static void Build(UIPanelBuilder builder)
	{
		builder.HStack(delegate(UIPanelBuilder uIPanelBuilder)
		{
			uIPanelBuilder.VStack(delegate(UIPanelBuilder panelBuilder)
			{
				panelBuilder.AddTitle(StateManager.Shared.RailroadName, StateManager.Shared.RailroadMark);
				BuildReputationSection(panelBuilder);
			});
			uIPanelBuilder.VStack(BuildDailyReportSection);
		}, 8f);
	}

	private static void BuildReputationSection(UIPanelBuilder builder)
	{
		builder.RebuildOnEvent<ReputationUpdated>();
		ReputationTracker reputationTracker = ReputationTracker.Shared;
		builder.AddSection("Reputation", delegate(UIPanelBuilder uIPanelBuilder)
		{
			ReputationReport report = reputationTracker.Report;
			if (report.Components.Count == 0)
			{
				uIPanelBuilder.AddLabel("Not yet available.");
				return;
			}
			uIPanelBuilder.AddField("Overall", ReputationTracker.ReputationString(reputationTracker.Reputation)).ChildWidth(0, 220f);
			foreach (ReputationReport.Component component in report.Components)
			{
				uIPanelBuilder.AddField(component.Category, PercentString(component.Score)).Tooltip(component.Category, PercentString(component.Ratio) + " of overall reputation").ChildWidth(0, 220f);
			}
		});
		builder.AddSection("Reputation Effects", delegate(UIPanelBuilder uIPanelBuilder)
		{
			float num = reputationTracker.PhaseDiscount();
			uIPanelBuilder.AddField("Phase Cost Discount", Mathf.RoundToInt(num * 100f) + "%").ChildWidth(0, 220f);
			float num2 = reputationTracker.EquipmentDiscount();
			uIPanelBuilder.AddField("Equipment Discount", Mathf.RoundToInt(num2 * 100f) + "%").ChildWidth(0, 220f);
			float num3 = reputationTracker.RepairBonus();
			uIPanelBuilder.AddField("Repair Speed Bonus", Mathf.RoundToInt(num3 * 100f) + "%").ChildWidth(0, 220f);
			int num4 = reputationTracker.ContractMaxStartTier();
			uIPanelBuilder.AddField("New Contract Tier", (num4 <= 1) ? "Tier 1 (no effect)" : $"Up to Tier {num4}").ChildWidth(0, 220f);
		});
		builder.AddExpandingVerticalSpacer();
		static string PercentString(float value)
		{
			return value.ToString("#0%");
		}
	}

	private static void BuildDailyReportSection(UIPanelBuilder builder)
	{
		DailyReportGenerator shared = DailyReportGenerator.Shared;
		builder.AddObserver(shared.Observe(((UIPanelBuilder)builder).Rebuild));
		string text = shared.LatestReportMarkup;
		if (string.IsNullOrEmpty(text))
		{
			text = "# Daily Report\nDaily reports are compiled at 6pm.";
		}
		string text2 = TMPMarkupRenderer.Render(Parser.Parse(text));
		builder.AddTextArea(text2, delegate(string link)
		{
			Debug.Log("Unhandled link clicked: " + link);
		}).Width(400f);
	}
}
