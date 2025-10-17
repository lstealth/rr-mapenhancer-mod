using System;
using System.Collections.Generic;
using System.Linq;
using Game;
using Game.Reputation;
using Game.State;
using UnityEngine;

namespace Model.Ops;

public static class ContractExtensions
{
	private const int TierMin = 1;

	private const int TierMax = 5;

	public static bool HasActiveContract(this Industry industry, GameDateTime now)
	{
		Contract contract;
		return industry.TryGetActiveContract(out contract);
	}

	public static bool TryGetActiveContract(this Industry industry, out Contract contract)
	{
		if (!industry.usesContract)
		{
			contract = default(Contract);
			return false;
		}
		Contract? contract2 = industry.Contract;
		if (contract2.HasValue)
		{
			Contract valueOrDefault = contract2.GetValueOrDefault();
			contract = valueOrDefault;
			return true;
		}
		contract = default(Contract);
		return false;
	}

	public static float GetContractMultiplier(this Industry industry)
	{
		if (!industry.usesContract)
		{
			return 1f;
		}
		Contract? contract = industry.Contract;
		if (!contract.HasValue)
		{
			return 0f;
		}
		return contract.Value.Percent;
	}

	public static bool ShouldOrderCars(this Industry industry)
	{
		if (industry.usesContract)
		{
			return industry.HasActiveContract(TimeWeather.Now);
		}
		return true;
	}

	public static List<Contract> AvailableContracts(this Industry industry)
	{
		if (StateManager.Shared.GameMode == GameMode.Sandbox)
		{
			return MakeContracts(0, 5);
		}
		Contract? contract = industry.Contract;
		if (contract.HasValue)
		{
			Contract value = contract.Value;
			List<float> list = (from kv in industry.PerformanceHistory
				orderby kv.Key
				select kv.Value).ToList();
			float num = ((list.Count == 0) ? 0f : list.Average());
			int tier = value.Tier;
			int num2 = ((num > 0.9f) ? ((!(num > 0.95f)) ? (tier + 1) : (tier + 2)) : ((!(num > 0.7f)) ? (tier - 1) : tier));
			int num3 = num2;
			if (list.Count < 3)
			{
				num3 = Mathf.Min(num3, tier);
			}
			if (list.Count < 5)
			{
				num3 = Mathf.Min(num3, tier + 1);
			}
			return MakeContracts(0, num3);
		}
		int endTier = ReputationTracker.Shared.ContractMaxStartTier();
		return MakeContracts(0, endTier);
	}

	private static List<Contract> MakeContracts(int startTier, int endTier)
	{
		List<Contract> list = new List<Contract>(1 + endTier - startTier);
		startTier = Mathf.Clamp(startTier, 0, 5);
		endTier = Mathf.Clamp(endTier, 1, 5);
		for (int i = startTier; i <= endTier; i++)
		{
			Contract item = new Contract(i);
			list.Add(item);
		}
		return list;
	}

	public static (float percent, float speedBonus) NumbersForTier(int tier)
	{
		return tier switch
		{
			1 => (percent: 0.24f, speedBonus: 0f), 
			2 => (percent: 0.34f, speedBonus: 0f), 
			3 => (percent: 0.49f, speedBonus: 0f), 
			4 => (percent: 0.7f, speedBonus: 0f), 
			5 => (percent: 1f, speedBonus: 0f), 
			_ => throw new ArgumentException("Invalid tier", "tier"), 
		};
	}

	public static int PenaltyForChange(this Industry industry, int targetTier, int days, out int tierChangeComponent, out int ageComponent)
	{
		int num = industry.Contract?.Tier ?? 0;
		int num2 = 250;
		int num3 = num - targetTier;
		if (num3 <= 0)
		{
			tierChangeComponent = 0;
			ageComponent = 0;
			return 0;
		}
		days = Mathf.Max(1, days);
		tierChangeComponent = num2 * num3;
		ageComponent = num2 * Mathf.Max(6 - days, 1);
		return tierChangeComponent + ageComponent;
	}
}
