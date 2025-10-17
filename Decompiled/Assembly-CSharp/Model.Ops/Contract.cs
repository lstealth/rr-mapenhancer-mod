using System.Collections.Generic;
using KeyValue.Runtime;
using UnityEngine;

namespace Model.Ops;

public readonly struct Contract
{
	public readonly int Tier;

	public const int TimelyDeliveryMaxDays = 2;

	private const string TierKey = "tier";

	public float Percent => ContractExtensions.NumbersForTier(Tier).percent;

	public float SpeedBonus => ContractExtensions.NumbersForTier(Tier).speedBonus;

	public Value PropertyValue => Value.Dictionary(new Dictionary<string, Value> { 
	{
		"tier",
		Value.Int(Tier)
	} });

	public Contract(int tier)
	{
		Tier = tier;
	}

	public override string ToString()
	{
		return $"Tier {Tier}";
	}

	public int TimelyDeliveryBonus(int days, int basePayment)
	{
		float num = Tier switch
		{
			2 => 4f, 
			3 => 6f, 
			4 => 8f, 
			5 => 10f, 
			_ => 0f, 
		};
		return Mathf.RoundToInt((float)basePayment * (days switch
		{
			0 => num, 
			1 => num / 2f, 
			2 => num / 4f, 
			_ => 0f, 
		} / 100f));
	}

	public static Contract? FromPropertyValue(Value value)
	{
		if (value.Type != ValueType.Dictionary)
		{
			return null;
		}
		int tier = (value["tier"].IsNull ? 1 : value["tier"].IntValue);
		return new Contract(tier);
	}
}
