using System;
using System.Collections.Generic;
using System.Linq;
using Game.Progression;
using KeyValue.Runtime;
using Serilog;
using UnityEngine;

namespace Model.Ops;

public class ProgressionIndustryComponent : IndustryComponent
{
	private const string IndRecvKey = "indRecv";

	private readonly Dictionary<string, Section.Delivery> _deliveries = new Dictionary<string, Section.Delivery>();

	private Action _onComplete;

	private IKeyValueObject _keyValueObject;

	public void Configure(Section section, int deliveryPhaseIndex, Section.DeliveryPhase phase, Action onComplete, IKeyValueObject keyValueObject)
	{
		_deliveries.Clear();
		for (int i = 0; i < phase.deliveries.Length; i++)
		{
			Section.Delivery value = phase.deliveries[i];
			string key = OrderTag(section, deliveryPhaseIndex, i);
			_deliveries[key] = value;
		}
		_onComplete = onComplete;
		_keyValueObject = keyValueObject;
	}

	public override void Service(IIndustryContext ctx)
	{
		foreach (IOpsCar item in EnumerateCars(ctx, requireWaybill: true).ToList())
		{
			Waybill? waybill = item.Waybill;
			if (!waybill.HasValue)
			{
				continue;
			}
			string text = waybill.Value.Tag;
			if (!_deliveries.TryGetValue(text, out var value))
			{
				continue;
			}
			bool num = value.direction == Section.Delivery.Direction.LoadToIndustry;
			float num2 = IndustryComponent.RateToValue(value.load.NominalQuantityPerCarLoad * 3f, ctx.DeltaTime);
			if (num)
			{
				item.Unload(value.load, num2);
				if (item.QuantityOfLoad(value.load).quantity < 0.01f)
				{
					ctx.OrderAwayEmpty(item, null, noPayment: true);
					IncrementReceived(ctx, text);
				}
				continue;
			}
			item.Load(value.load, num2);
			var (num3, num4) = item.QuantityOfLoad(value.load);
			if (Mathf.Abs(num3 - num4) < 0.01f)
			{
				ctx.OrderAwayLoaded(item, null, noPayment: true);
				IncrementReceived(ctx, text);
			}
		}
		CheckForCompletion(ctx);
	}

	public override void OrderCars(IIndustryContext ctx)
	{
		foreach (KeyValuePair<string, Section.Delivery> delivery2 in _deliveries)
		{
			delivery2.Deconstruct(out var key, out var value);
			string orderTag = key;
			Section.Delivery delivery = value;
			int num = CountOrdered(ctx, orderTag);
			int num2 = CountReceived(ctx, orderTag);
			int num3 = delivery.count - (num + num2);
			while (num3 > 0)
			{
				if (delivery.direction == Section.Delivery.Direction.LoadToIndustry)
				{
					if (ctx.OrderLoad(delivery.carTypeFilter, delivery.load, orderTag, noPayment: true, out var quantity))
					{
						num3 -= Mathf.CeilToInt(quantity / delivery.load.NominalQuantityPerCarLoad);
					}
				}
				else
				{
					ctx.OrderEmpty(delivery.carTypeFilter, orderTag, noPayment: true);
					num3--;
				}
			}
		}
	}

	private void CheckForCompletion(IIndustryContext ctx)
	{
		if (_deliveries.Count == 0)
		{
			return;
		}
		foreach (var (orderTag, delivery2) in _deliveries)
		{
			if (CountReceived(ctx, orderTag) < delivery2.count)
			{
				return;
			}
		}
		OrderAwayLeftovers(ctx);
		_onComplete();
	}

	private void OrderAwayLeftovers(IIndustryContext ctx)
	{
		foreach (IOpsCar item in EnumerateCars(ctx, requireWaybill: true))
		{
			Log.Information("OrderAway {car} - leftover", item);
			ctx.OrderAwayEmpty(item, null, noPayment: true);
		}
	}

	private void IncrementReceived(IIndustryContext ctx, string orderTag)
	{
		Dictionary<string, Value> dictionary = new Dictionary<string, Value>(_keyValueObject["indRecv"].DictionaryValue);
		int num = 0;
		if (dictionary.TryGetValue(orderTag, out var value))
		{
			num = value.IntValue;
		}
		dictionary[orderTag] = Value.Int(num + 1);
		_keyValueObject["indRecv"] = Value.Dictionary(dictionary);
	}

	private int CountReceived(IIndustryContext ctx, string orderTag)
	{
		if (_keyValueObject["indRecv"].DictionaryValue.TryGetValue(orderTag, out var value))
		{
			return value.IntValue;
		}
		return 0;
	}

	private static string OrderTag(Section section, int phaseIndex, int deliveryIndex)
	{
		return $"{section.identifier}.{phaseIndex}.{deliveryIndex}";
	}

	private int CountOrdered(IIndustryContext ctx, string orderTag)
	{
		return ctx.NumberOfCarsOnOrderForTag(orderTag);
	}
}
