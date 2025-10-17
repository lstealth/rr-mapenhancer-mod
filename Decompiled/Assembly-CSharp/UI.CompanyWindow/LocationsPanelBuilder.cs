using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Game;
using Game.Messages;
using Game.Reputation;
using Game.State;
using Helpers;
using Model.Ops;
using Track;
using UI.Builder;
using UnityEngine;

namespace UI.CompanyWindow;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct LocationsPanelBuilder
{
	private class IndustryDetailBuilder
	{
		private Industry _industry;

		public IndustryDetailBuilder(Industry industry)
		{
			_industry = industry;
		}

		internal void AddTracksSection(UIPanelBuilder builder)
		{
			foreach (IIndustryTrackDisplayable ic in from td in _industry.TrackDisplayables
				group td by td.TrackGrouping() into g
				select g.OrderByDescending((IIndustryTrackDisplayable td) => td.TrackSpans.Sum((TrackSpan ts) => ts.Length)).First())
			{
				string locationName = ic.ShortName(_industry);
				builder.AddLocationField(locationName, ic, delegate
				{
					CameraSelector.shared.JumpTo(ic);
				});
			}
		}

		internal void AddContractSection(UIPanelBuilder builder)
		{
			if (!_industry.usesContract)
			{
				return;
			}
			UIPanelBuilder contractPanelBuilder = builder;
			contractPanelBuilder.AddObserver(_industry.ObserveContract(delegate
			{
				contractPanelBuilder.Rebuild();
			}, callInitial: false));
			contractPanelBuilder.AddObserver(_industry.ObserveNextContract(delegate
			{
				contractPanelBuilder.Rebuild();
			}, callInitial: false));
			Contract? maybeContract = _industry.Contract;
			if (maybeContract.HasValue)
			{
				Contract contract = maybeContract.Value;
				builder.AddSection("Contract", delegate(UIPanelBuilder uIPanelBuilder)
				{
					uIPanelBuilder.AddField("Tier", $"Tier {contract.Tier}");
					int num = contract.TimelyDeliveryBonus(0, 100);
					int num2 = contract.TimelyDeliveryBonus(1, 100);
					int num3 = contract.TimelyDeliveryBonus(2, 100);
					uIPanelBuilder.AddField("Bonus", (num > 0) ? $"{num}-{num2}-{num3}% for timely delivery" : "(Available at higher tiers.)").Tooltip("Timely Delivery Bonus", "A percentage bonus is awarded for fulfilling waybills on the same, first, or second day.");
					List<string> list = (from kv in _industry.PerformanceHistory
						orderby kv.Key
						select kv.Value * 100f into percent
						select $"{percent:F0}%").ToList();
					if (list.Count == 0)
					{
						uIPanelBuilder.AddField("Performance", "Not yet reported at this tier").Tooltip("No Performance Data", "Calculated prior to interchange service after first delivery to customer.");
					}
					else
					{
						uIPanelBuilder.AddField("Performance", string.Join(", ", list));
					}
				});
			}
			string sectionTitle = (maybeContract.HasValue ? "Change Contract" : "Establish Contract");
			builder.AddSection(sectionTitle, delegate(UIPanelBuilder builder2)
			{
				int currentContractTier = maybeContract?.Tier ?? 0;
				Contract? nextContract = _industry.NextContract;
				List<Contract> list = _industry.AvailableContracts();
				if (list.All((Contract c) => c.Tier != currentContractTier))
				{
					list.Add(new Contract(currentContractTier));
				}
				RectTransform control = builder2.AddDropdownIntPicker(list.Select((Contract c) => c.Tier).ToList(), nextContract?.Tier ?? currentContractTier, delegate(int num2)
				{
					string text2 = ((currentContractTier == 0) ? "No Contract" : "Terminate Contract");
					string text3 = ((num2 == 0) ? text2 : $"Tier {num2}");
					if (num2 == currentContractTier)
					{
						text3 += " (no change)";
					}
					return text3;
				}, StateManager.CheckAuthorizedToSendMessage(new ModifyContract(null, 0)), delegate(int tier2)
				{
					StateManager.ApplyLocal(new ModifyContract(_industry.identifier, tier2));
				});
				builder2.AddField("Available Tiers", control);
				if (nextContract.HasValue)
				{
					int tier = nextContract.Value.Tier;
					string text = ((tier == 0) ? "Terminate Contract" : $"Tier {tier}");
					builder2.AddField("", "Tomorrow: " + text).Tooltip("Contract Change", "Contract change will take effect at midnight.");
					int tierChangeComponent;
					int ageComponent;
					int num = _industry.PenaltyForChange(tier, _industry.PerformanceHistory.Count + 1, out tierChangeComponent, out ageComponent);
					if (num > 0)
					{
						StringBuilder stringBuilder = new StringBuilder($"{num:C0} ({tierChangeComponent:C0} tier change");
						if (ageComponent > 0)
						{
							stringBuilder.Append($" + {ageComponent:C0} contract age");
						}
						stringBuilder.Append(")");
						builder2.AddField("Tier Change Penalty", stringBuilder.ToString()).Tooltip("Tier Change Penalty", "A penalty is paid to your customer when lowering service tiers.\nThe penalty is made up of a cost per tier and a fee for recent contracts which diminishes each day.");
					}
				}
			});
		}

		internal void AddAdvisoriesSection(UIPanelBuilder builder)
		{
			foreach (var item3 in _industry.EnumerateComponentContexts(0f))
			{
				IndustryComponent item = item3.Item1;
				IndustryContext item2 = item3.Item2;
				if (item.ProgressionDisabled)
				{
					continue;
				}
				List<IndustryComponent.PanelField> fields = item.PanelFields(item2).ToList();
				if (fields.Count == 0)
				{
					continue;
				}
				builder.AddSection(item.ShortName(_industry), delegate(UIPanelBuilder uIPanelBuilder)
				{
					foreach (IndustryComponent.PanelField item4 in fields)
					{
						uIPanelBuilder.AddField(item4.Label, item4.Text).Tooltip(item4.Label, item4.Tooltip);
					}
				});
			}
			Dictionary<string, string> warnings = _industry.Storage.Warnings;
			if (!warnings.Any())
			{
				return;
			}
			builder.AddSection("Advisories", delegate(UIPanelBuilder uIPanelBuilder)
			{
				foreach (var (_, value) in warnings)
				{
					uIPanelBuilder.AddField("Warning", value);
				}
			});
		}

		public void AddOrdersSection(UIPanelBuilder builder)
		{
			foreach (IndustryComponent component in _industry.Components)
			{
				if (!component.ProgressionDisabled)
				{
					component.BuildPanel(builder);
				}
			}
		}

		public void AddPassengerSection(UIPanelBuilder builder)
		{
			PassengerStop passengerStop = _industry.GetComponentInChildren<PassengerStop>();
			if (passengerStop == null)
			{
				return;
			}
			builder.AddSection("Passenger Stop", delegate(UIPanelBuilder uIPanelBuilder)
			{
				GameDateTime? lastServed = passengerStop.LastServed;
				GameDateTime now = TimeWeather.Now;
				ReputationTracker shared = ReputationTracker.Shared;
				uIPanelBuilder.AddField("Status", shared.IncludeInNetwork(passengerStop, now) ? "Active" : "Inactive");
				string value2;
				if (lastServed.HasValue)
				{
					GameDateTime value = lastServed.Value;
					string arg = (value.Day - now.Day) switch
					{
						0 => "Today", 
						1 => "Yesterday", 
						_ => value.IntervalString(now) + " ago", 
					};
					value2 = $"{value} ({arg})";
				}
				else
				{
					value2 = "Never";
				}
				uIPanelBuilder.AddField("Last Served", value2);
			});
		}
	}

	public static void Build(UIPanelBuilder builder, UIState<string> selectedItem)
	{
		List<UIPanelBuilder.ListItem<Industry>> list = OpsController.Shared.Areas.SelectMany((Area area) => (from ind in area.Industries
			where !ind.ProgressionDisabled
			where ind.TrackDisplayables.Any()
			orderby ind.name
			select ind).Select(delegate(Industry ind)
		{
			string identifier = ind.identifier;
			string name = area.name;
			string name2 = ind.name;
			return new UIPanelBuilder.ListItem<Industry>(identifier, ind, name, name2);
		})).ToList();
		if (list.Count > 0 && string.IsNullOrEmpty(selectedItem.Value))
		{
			Vector3 worldCameraPosition = CameraSelector.shared.CurrentCameraPosition.GameToWorld();
			selectedItem.Value = list.OrderBy((UIPanelBuilder.ListItem<Industry> item) => Vector3.SqrMagnitude(item.Value.transform.position - worldCameraPosition)).First().Identifier;
		}
		builder.AddListDetail(list, selectedItem, delegate(UIPanelBuilder uIPanelBuilder, Industry industry)
		{
			uIPanelBuilder.VScrollView(delegate(UIPanelBuilder builder2)
			{
				if (industry == null)
				{
					builder2.AddLabel("Please select a location.");
				}
				else
				{
					IndustryDetailBuilder detailBuilder = new IndustryDetailBuilder(industry);
					builder2.AddTitle(industry.name, industry.GetComponentInParent<Area>().name);
					builder2.AddSection("Tracks", delegate(UIPanelBuilder builder3)
					{
						detailBuilder.AddTracksSection(builder3);
					});
					detailBuilder.AddPassengerSection(builder2);
					detailBuilder.AddContractSection(builder2);
					detailBuilder.AddAdvisoriesSection(builder2);
					detailBuilder.AddOrdersSection(builder2);
					builder2.AddExpandingVerticalSpacer();
				}
			});
		});
	}
}
