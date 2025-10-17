using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Core;
using Game.Events;
using Game.Messages;
using Game.Progression;
using Game.State;
using Model.Ops;
using Serilog;
using UI.Builder;
using UnityEngine;

namespace UI.CompanyWindow;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct GoalsPanelBuilder
{
	public static void Build(UIPanelBuilder builder, UIState<string> selectedItem)
	{
		Progression shared = Progression.Shared;
		if (shared == null)
		{
			builder.AddLabel("Milestones not available. Please quit and reload this save.");
			return;
		}
		builder.RebuildOnEvent<ProgressionStateDidChange>();
		List<UIPanelBuilder.ListItem<Section>> data = shared.Sections.OrderBy(SectionIndexForSection).Select(delegate(Section section)
		{
			string text = ((section.Unlocked || section.Available) ? section.displayName : ("<i>" + section.displayName + "</i>"));
			return new UIPanelBuilder.ListItem<Section>(section.identifier, section, SectionNameForSection(section), text);
		}).ToList();
		builder.AddListDetail(data, selectedItem, delegate(UIPanelBuilder builder2, Section section)
		{
			if (section == null)
			{
				builder2.AddLabel("No milestone selected.");
			}
			else
			{
				if (section.Available)
				{
					PopulateAvailable(section, builder2);
				}
				else if (section.Unlocked)
				{
					builder2.AddTitle(section.displayName, "Completed!");
					builder2.AddLabel(section.description);
				}
				else
				{
					builder2.AddTitle(section.displayName, "Not yet available.");
					builder2.AddLabel(section.description);
					builder2.AddSection("Prerequisites", delegate(UIPanelBuilder uIPanelBuilder)
					{
						Section[] prerequisiteSections = section.prerequisiteSections;
						foreach (Section section2 in prerequisiteSections)
						{
							uIPanelBuilder.AddLabel(section2.displayName + " - " + (section2.Unlocked ? "Completed" : "Not Complete"));
						}
					});
				}
				builder2.AddExpandingVerticalSpacer();
			}
		});
		static int SectionIndexForSection(Section section)
		{
			if (section.Unlocked)
			{
				return 3;
			}
			if (!section.Available)
			{
				return 2;
			}
			if (section.PaidCount <= 0)
			{
				return 1;
			}
			return 0;
		}
		static string SectionNameForSection(Section section)
		{
			if (section.Unlocked)
			{
				return "Complete";
			}
			if (!section.Available)
			{
				return "Not Yet Available";
			}
			if (section.PaidCount <= 0)
			{
				return "Available";
			}
			return "In Progress";
		}
	}

	private static void PopulateAvailable(Section section, UIPanelBuilder builder)
	{
		int num = section.deliveryPhases.Length;
		builder.AddTitle(section.displayName, $"{section.FulfilledCount}/{num} Phases Complete");
		builder.AddLabel(section.description);
		if (section.PaidCount == section.FulfilledCount && section.PaidCount < num)
		{
			int nextPhaseIndex = section.PaidCount;
			Section.DeliveryPhase deliveryPhase = section.deliveryPhases[nextPhaseIndex];
			bool flag = deliveryPhase.deliveries.Length != 0;
			builder.AddField("Phase", $"{nextPhaseIndex + 1} of {num}");
			builder.AddField("Status", "Available");
			builder.AddField("Cost", $"{deliveryPhase.cost:C0}");
			if (flag)
			{
				ProgressionIndustryComponent industryComponent = deliveryPhase.industryComponent;
				if (industryComponent == null)
				{
					LogNullIndustryComponent(builder, section, nextPhaseIndex);
					return;
				}
				int number = deliveryPhase.deliveries.Sum((Section.Delivery d) => d.count);
				builder.AddField("Task", "Deliver " + number.Pluralize("car") + " to " + industryComponent.name + ". These cars will be delivered to the interchange the next time the interchange is served.");
			}
			builder.Spacer(48f);
			float discountPercent;
			int num2 = Progression.Shared.CostForPhase(deliveryPhase, out discountPercent);
			string arg = StartButtonText(nextPhaseIndex, num, flag);
			string arg2 = ((discountPercent > 0.001f) ? $" {Mathf.RoundToInt(discountPercent * 100f)}% off" : "");
			string text = $"{arg} ({num2:C0}{arg2})";
			builder.AddButton(text, delegate
			{
				StateManager.ApplyLocal(new ProgressionStartPhase(section.identifier, nextPhaseIndex));
			}).Disable(num2 > StateManager.Shared.Balance);
			builder.RebuildOnEvent<BalanceDidChange>();
			return;
		}
		int num3 = section.PaidCount - 1;
		Section.DeliveryPhase deliveryPhase2 = section.deliveryPhases[num3];
		ProgressionIndustryComponent deliveryIndustry = deliveryPhase2.industryComponent;
		if (deliveryIndustry == null)
		{
			LogNullIndustryComponent(builder, section, num3);
			return;
		}
		builder.AddField("Phase", $"{num3 + 1} of {num}");
		builder.AddField("Status", "Cars Ordered, Waiting for Delivery");
		int number2 = deliveryPhase2.deliveries.Sum((Section.Delivery d) => d.count);
		builder.AddField("Task", "Deliver " + number2.Pluralize("car") + " to " + deliveryIndustry.name + ". These cars will be delivered to the interchange the next time the interchange is served.");
		builder.Spacer(40f);
		builder.AddLocationField(deliveryIndustry.name, deliveryIndustry, delegate
		{
			CameraSelector.shared.JumpTo(deliveryIndustry);
		});
	}

	private static string StartButtonText(int phaseIndex, int phaseCount, bool hasDeliveries)
	{
		string text = (hasDeliveries ? "Start" : "Purchase");
		if (phaseCount <= 1)
		{
			return text;
		}
		string text2 = ((phaseIndex == 0) ? "First" : ((phaseIndex != phaseCount - 1) ? "Next" : "Final"));
		return text + " " + text2 + " Phase";
	}

	private static void LogNullIndustryComponent(UIPanelBuilder builder, Section section, int nextPhaseIndex)
	{
		Log.Error("Null industry on {section} {phase}", section.identifier, nextPhaseIndex);
		builder.AddLabel("(Internal error. Please send player.log.");
	}
}
