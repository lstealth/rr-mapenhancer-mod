using System;
using System.Linq;
using Game;
using Helpers;
using Serilog;
using UnityEngine;

namespace Model.Ops;

public class TeamTrack : IndustryComponent
{
	[SerializeField]
	public TeamTrackProfile profile;

	[Tooltip("How many cars this team track wants either at it or en route to it.")]
	[SerializeField]
	public float idealCars;

	public override void Service(IIndustryContext ctx)
	{
		foreach (IOpsCar item3 in EnumerateCars(ctx, requireWaybill: true).ToList())
		{
			Waybill? waybill = item3.Waybill;
			if (!waybill.HasValue)
			{
				continue;
			}
			string orderTag = waybill.Value.Tag;
			TeamTrackProfile.Entry? entry = EntryForTag(orderTag);
			if (!entry.HasValue)
			{
				Log.Debug("TeamTrack {id}: Order Away Unknown {car}", base.Identifier, item3);
				ctx.OrderAwayEmpty(item3, orderTag);
				break;
			}
			TeamTrackProfile.Entry value = entry.Value;
			(float quantity, float capacity) tuple = item3.QuantityOfLoad(value.load);
			float item = tuple.quantity;
			float item2 = tuple.capacity;
			float num = IndustryComponent.RateToValue(item2 / value.loadingTime, ctx.DeltaTime);
			if (value.export)
			{
				float num2 = Mathf.Min(item2 - item, num);
				if (num2 + item < item2)
				{
					item3.Load(value.load, num2);
					continue;
				}
				item3.Load(value.load, item2 - item);
				ctx.OrderAwayLoaded(item3, value.tag);
			}
			else
			{
				float quantityToConsume = Mathf.Min(item, num);
				if (num < item)
				{
					item3.Unload(value.load, quantityToConsume);
					continue;
				}
				item3.Unload(value.load, item);
				ctx.OrderAwayLoaded(item3, value.tag);
			}
		}
	}

	private TeamTrackProfile.Entry? EntryForTag(string orderTag)
	{
		foreach (TeamTrackProfile.Entry entry in profile.entries)
		{
			if (entry.tag == orderTag)
			{
				return entry;
			}
		}
		return null;
	}

	public override void OrderCars(IIndustryContext ctx)
	{
		GameDateTime gameDateTime = ctx.GetDateTime("first", GameDateTime.Zero);
		if (gameDateTime == GameDateTime.Zero)
		{
			gameDateTime = ctx.Now;
			ctx.SetDateTime("first", gameDateTime);
		}
		float value = ctx.Now.DaysSince(gameDateTime);
		float num = Mathf.Lerp(0.25f, 1f, Mathf.InverseLerp(0f, 2f, value));
		int num2 = profile.entries.Select((TeamTrackProfile.Entry e) => e.tag).Sum((Func<string, int>)ctx.NumberOfCarsOnOrderForTag);
		float num3 = Mathf.Pow(UnityEngine.Random.Range(0f, 1f), 2f);
		int num4 = Mathf.RoundToInt(idealCars * num3 * num);
		Log.Debug("TeamTrack {id}: OrderCars {onOrder} vs {desired}, mult {multiplier}", base.Identifier, num2, num4, num);
		for (; num2 < num4; num2++)
		{
			TeamTrackProfile.Entry entry = profile.entries.Random();
			if (entry.export)
			{
				ctx.OrderEmpty(entry.carTypeFilter, entry.tag);
			}
			else
			{
				ctx.OrderLoad(entry.carTypeFilter, entry.load, entry.tag, noPayment: false, out var _);
			}
		}
	}
}
