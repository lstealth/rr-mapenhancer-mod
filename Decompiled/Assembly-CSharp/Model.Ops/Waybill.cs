using System.Collections.Generic;
using Game;
using KeyValue.Runtime;
using UnityEngine;

namespace Model.Ops;

public struct Waybill
{
	public GameDateTime Created;

	public OpsCarPosition? Origin;

	public OpsCarPosition Destination;

	public int PaymentOnArrival;

	public bool Completed;

	public readonly string Tag;

	public int GraceDays;

	public Value PropertyValue => Value.Dictionary(new Dictionary<string, Value>
	{
		{
			"created",
			Created.KeyValueValue()
		},
		{
			"originId",
			Origin.HasValue ? Value.String(Origin.Value.Identifier) : Value.Null()
		},
		{
			"destId",
			Value.String(Destination.Identifier)
		},
		{
			"paymentOnArrival",
			Value.Int(PaymentOnArrival)
		},
		{
			"completed",
			Value.Bool(Completed)
		},
		{
			"tag",
			string.IsNullOrEmpty(Tag) ? Value.Null() : Value.String(Tag)
		},
		{
			"graceDays",
			(GraceDays > 0) ? Value.Int(GraceDays) : Value.Null()
		}
	});

	public Waybill(GameDateTime created, OpsCarPosition? origin, OpsCarPosition destination, int paymentOnArrival, bool completed, string tag, int graceDays)
	{
		Created = created;
		Origin = origin;
		Destination = destination;
		PaymentOnArrival = paymentOnArrival;
		Completed = completed;
		Tag = tag;
		GraceDays = graceDays;
	}

	public override string ToString()
	{
		return $"Created = {Created}, Origin = {Origin}, Destination = {Destination}, Payment = {PaymentOnArrival}, Completed = {Completed}, Tag = {Tag}, GraceDays = {GraceDays}";
	}

	public static Waybill? FromPropertyValue(Value value, IOpsCarPositionResolver resolver)
	{
		if (value.Type != ValueType.Dictionary)
		{
			return null;
		}
		IReadOnlyDictionary<string, Value> dictionaryValue = value.DictionaryValue;
		GameDateTime created = dictionaryValue["created"].GameDateTime(TimeWeather.Now);
		string stringValue = dictionaryValue["originId"].StringValue;
		string tag = (dictionaryValue.ContainsKey("tag") ? dictionaryValue["tag"].StringValue : null);
		int graceDays = (dictionaryValue.ContainsKey("graceDays") ? dictionaryValue["graceDays"].IntValue : 0);
		return new Waybill(created, string.IsNullOrEmpty(stringValue) ? ((OpsCarPosition?)null) : new OpsCarPosition?(resolver.ResolveOpsCarPosition(stringValue)), resolver.ResolveOpsCarPosition(dictionaryValue["destId"].StringValue), dictionaryValue["paymentOnArrival"].IntValue, dictionaryValue["completed"].BoolValue, tag, graceDays);
	}

	public int ConditionFineForCarCondition(float condition)
	{
		float t = Mathf.InverseLerp(0.95f, 0f, condition);
		return Mathf.FloorToInt((float)PaymentOnArrival * Mathf.Lerp(0f, 0.75f, t));
	}
}
