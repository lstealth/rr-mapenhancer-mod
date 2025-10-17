using System;
using System.Collections.Generic;
using System.Linq;
using Analytics;
using Core;
using Game;
using Game.Events;
using Game.State;
using Network;
using Serilog;
using Track;
using UI.Builder;
using UnityEngine;

namespace Model.Ops;

public class Interchange : IndustryComponent
{
	public enum NextServiceStyle
	{
		Daily,
		Extra
	}

	[NonSerialized]
	public readonly List<IOrder> Orders = new List<IOrder>();

	private InterchangedIndustryLoader[] Loaders => base.Industry.GetComponentsInChildren<InterchangedIndustryLoader>();

	public bool Disabled
	{
		get
		{
			return base.Industry.Storage.InterchangeDisabled;
		}
		set
		{
			base.Industry.Storage.SetInterchangeDisabled(value);
		}
	}

	private GameDateTime? LastServiced
	{
		get
		{
			return base.Industry.Storage.InterchangeLastServiced;
		}
		set
		{
			base.Industry.Storage.InterchangeLastServiced = value;
		}
	}

	private GameDateTime? ExtraScheduled
	{
		get
		{
			return base.Industry.Storage.InterchangeExtraScheduled;
		}
		set
		{
			base.Industry.Storage.InterchangeExtraScheduled = value;
		}
	}

	private static int ServeHour => StateManager.Shared.Storage.InterchangeServeHour;

	private int NumberOfCarsOrdered => Orders.Sum((IOrder order) => order.CarCount);

	public override void OrderCars(IIndustryContext ctx)
	{
	}

	public override void Service(IIndustryContext ctx)
	{
	}

	public GameDateTime GetNextServiceTime(GameDateTime now, out NextServiceStyle style, bool dailyOnly = false)
	{
		GameDateTime gameDateTime = LastServiced ?? new GameDateTime(0, ServeHour);
		GameDateTime gameDateTime2 = now.WithHours(ServeHour);
		GameDateTime gameDateTime3 = gameDateTime2.AddingDays(1f);
		GameDateTime gameDateTime4 = ((gameDateTime < gameDateTime2) ? gameDateTime2 : gameDateTime3);
		style = NextServiceStyle.Daily;
		if (dailyOnly)
		{
			return gameDateTime4;
		}
		GameDateTime? extraScheduled = ExtraScheduled;
		if (extraScheduled.HasValue)
		{
			GameDateTime valueOrDefault = extraScheduled.GetValueOrDefault();
			if (valueOrDefault < gameDateTime4)
			{
				gameDateTime4 = valueOrDefault;
				style = NextServiceStyle.Extra;
			}
		}
		return gameDateTime4;
	}

	internal void PrepareToService()
	{
		Orders.Clear();
	}

	public void ServeInterchange(IIndustryContext ctx)
	{
		ServeInterchangedIndustryLoaders(ctx.Now);
		int num = 0;
		foreach (IOpsCar item in EnumerateCars(ctx))
		{
			Waybill? waybill = item.Waybill;
			if (waybill.HasValue)
			{
				Waybill value = waybill.Value;
				if (value.Destination.Equals(this) && !item.IsOwnedByPlayer)
				{
					ctx.RemoveCar(item);
				}
				else if (value.Destination.Equals(this) && value.Tag == "sell" && item.IsOwnedByPlayer)
				{
					SellAndRemove(item);
				}
				else
				{
					num++;
				}
			}
		}
		int num2 = CalculateCapacity();
		int num3 = Mathf.Max(0, num2 - num);
		Log.Debug("Interchange: Capacity {capacity}, count {carsInInterchange} -> {maxToOrder}", num2, num, num3);
		int numberOfCarsOrdered = NumberOfCarsOrdered;
		ctx.AddOrderedCars(Orders, num3);
		int numberOfCarsOrdered2 = NumberOfCarsOrdered;
		for (int num4 = Orders.Count - 1; num4 >= 0; num4--)
		{
			IOrder order = Orders[num4];
			if (order.CarCount <= 0)
			{
				if (order is ReturnFromBardoOrder returnFromBardoOrder)
				{
					NotifyFilledReturnFromBardoOrder(returnFromBardoOrder.CarId);
				}
				Orders.RemoveAt(num4);
			}
		}
		if (numberOfCarsOrdered > 0)
		{
			Hyperlink hyperlink = Hyperlink.To(base.Industry);
			string message = ((numberOfCarsOrdered == numberOfCarsOrdered2) ? $"{hyperlink} received no cars as it is at capacity." : ((numberOfCarsOrdered2 <= 0) ? string.Format("{0} received {1}.", hyperlink, numberOfCarsOrdered.Pluralize("car")) : string.Format("{0} received {1}; interchange is at capacity.", hyperlink, (numberOfCarsOrdered - numberOfCarsOrdered2).Pluralize("car"))));
			Multiplayer.Broadcast(message);
		}
		LastServiced = ctx.Now;
		global::Analytics.Analytics.Post("InterchangeServed", new Dictionary<string, object>
		{
			{
				"id",
				base.Industry.identifier
			},
			{ "capacity", num2 },
			{ "pendingBefore", numberOfCarsOrdered },
			{ "pendingAfter", numberOfCarsOrdered2 },
			{ "unfulfilled", Orders.Count }
		});
	}

	private void ServeInterchangedIndustryLoaders(GameDateTime now)
	{
		InterchangedIndustryLoader[] loaders = Loaders;
		foreach (InterchangedIndustryLoader obj in loaders)
		{
			IndustryContext industryContext = obj.CreateContext(now, 0f);
			obj.ServeInterchange(industryContext, this);
		}
	}

	private void SellAndRemove(IOpsCar opsCar)
	{
		TrainController shared = TrainController.Shared;
		if (!shared.TryGetCarForId(opsCar.Id, out var car))
		{
			throw new Exception("Couldn't get car by id " + opsCar.Id);
		}
		int num = EquipmentPurchase.TradeInValueForCar(car);
		string displayName = car.DisplayName;
		shared.RemoveCarSmart(car.id);
		StateManager.Shared.ApplyToBalance(num, Ledger.Category.Equipment, null, displayName, 0, quiet: true);
		Multiplayer.Broadcast($"{car.DisplayName} has been sold for {num:C0}.");
	}

	public void AddOrder(Order order)
	{
		for (int i = 0; i < Orders.Count; i++)
		{
			IOrder order2 = Orders[i];
			if (!(order.Load != order2.Load) && order.CarTypeFilter.Equals(order2.CarTypeFilter) && order.Destination.Equals(order2.Destination))
			{
				order2.CarCount += order.CarCount;
				Orders[i] = order2;
				return;
			}
		}
		Orders.Add(order);
	}

	public void OrderReturnFromBardo(string carId)
	{
		Orders.Add(new ReturnFromBardoOrder(carId));
	}

	private void NotifyFilledReturnFromBardoOrder(string carId)
	{
		InterchangedIndustryLoader[] loaders = Loaders;
		for (int i = 0; i < loaders.Length; i++)
		{
			loaders[i].InterchangeDidFillReturnFromBardoOrder(carId);
		}
	}

	private int CalculateCapacity()
	{
		return Mathf.RoundToInt((float)((IEnumerable<TrackSpan>)trackSpans).Sum((Func<TrackSpan, int>)CarLengths) * 0.7f);
	}

	private int CarLengths(TrackSpan span)
	{
		if (!span.IsValid)
		{
			return 0;
		}
		return Mathf.FloorToInt(span.Length / 15.24f);
	}

	public void ScheduleExtra(GameDateTime? scheduledTime)
	{
		ExtraScheduled = scheduledTime;
	}

	public bool TryGetExtraScheduled(out GameDateTime scheduledTime)
	{
		GameDateTime? extraScheduled = ExtraScheduled;
		if (!extraScheduled.HasValue)
		{
			scheduledTime = default(GameDateTime);
			return false;
		}
		scheduledTime = extraScheduled.Value;
		return true;
	}

	public override void BuildPanel(UIPanelBuilder builder)
	{
		builder.AddSection("Interchange", delegate(UIPanelBuilder uIPanelBuilder)
		{
			int num;
			if (!Disabled)
			{
				OpsController shared = OpsController.Shared;
				num = (((object)shared != null && shared.EnabledInterchanges.Count() > 1) ? 1 : 0);
			}
			else
			{
				num = 1;
			}
			bool interactable = (byte)num != 0;
			RectTransform control = uIPanelBuilder.AddToggle(() => !Disabled, delegate(bool enable)
			{
				Disabled = !enable;
				uIPanelBuilder.Rebuild();
			}, interactable);
			uIPanelBuilder.AddField("Enable", control).Tooltip("Enable Interchange", "Uncheck to turn off service to this interchange. At least one interchange must be enabled on your railroad.");
			if (!Disabled)
			{
				uIPanelBuilder.RebuildOnEvent<TimeAdvanced>();
				uIPanelBuilder.RebuildOnEvent<TimeHourDidChange>();
				uIPanelBuilder.AddObserver(base.Industry.Storage.ObserveInterchangeLastServicedChanged(delegate
				{
					uIPanelBuilder.Rebuild();
				}));
				uIPanelBuilder.AddObserver(base.Industry.Storage.ObserveInterchangeExtraScheduledChanged(delegate
				{
					uIPanelBuilder.Rebuild();
				}));
				NextServiceStyle style;
				GameDateTime nextServiceTime = GetNextServiceTime(TimeWeather.Now, out style, dailyOnly: true);
				string text = "Next Daily Service";
				string message = "Next daily interchange service.";
				uIPanelBuilder.AddField(text, () => $"{nextServiceTime} ({IntervalString(nextServiceTime)})", UIPanelBuilder.Frequency.Periodic).Tooltip(text, message);
				bool canScheduleExtra = base.Industry.Storage.CanScheduleExtra;
				if (TryGetExtraScheduled(out var scheduledTime))
				{
					uIPanelBuilder.AddField("Extra Service", () => "Scheduled for " + scheduledTime.TimeString() + " (" + IntervalString(scheduledTime) + ")", UIPanelBuilder.Frequency.Periodic);
					if (canScheduleExtra)
					{
						uIPanelBuilder.AddField("", uIPanelBuilder.AddButton("Cancel Scheduled", delegate
						{
							ScheduleExtra(null);
							uIPanelBuilder.Rebuild();
						}).RectTransform);
					}
				}
				else if (canScheduleExtra)
				{
					GameDateTime scheduleTime = NextAvailableServiceTime(TimeWeather.Now);
					uIPanelBuilder.AddField("Extra Service", uIPanelBuilder.AddButton("Schedule for " + scheduleTime.TimeString(), delegate
					{
						ScheduleExtra(scheduleTime);
						uIPanelBuilder.Rebuild();
					}).RectTransform);
				}
			}
		});
		static string IntervalString(GameDateTime nextRegular)
		{
			return nextRegular.IntervalString(TimeWeather.Now, GameDateTimeInterval.Style.Short);
		}
	}

	public static GameDateTime NextAvailableServiceTime(GameDateTime now)
	{
		return now.AddingHours(2.5f).RoundingMinutes(5);
	}
}
