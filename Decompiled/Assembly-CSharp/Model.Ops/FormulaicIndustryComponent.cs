using System;
using System.Collections.Generic;
using System.Linq;
using Model.Ops.Definition;
using Serilog;
using Track;
using UnityEngine;

namespace Model.Ops;

public class FormulaicIndustryComponent : IndustryComponent
{
	[Serializable]
	public class Term
	{
		public Load load;

		public float unitsPerDay = 1f;
	}

	public List<Term> inputTerms = new List<Term>();

	public List<Term> outputTerms = new List<Term>();

	private HashSet<IndustryComponent> _otherComponents;

	protected override void ValidateIndustryComponent()
	{
		if (inputTerms.Count == 0)
		{
			Log.Error("Formulaic Industry Component has no inputs: {identifier}", base.Identifier);
		}
		if (outputTerms.Count == 0)
		{
			Log.Error("Formulaic Industry Component has no outputs: {identifier}", base.Identifier);
		}
	}

	public override void Service(IIndustryContext ctx)
	{
		float contractMultiplier = base.Industry.GetContractMultiplier();
		float num = IndustryComponent.RateToValue(contractMultiplier, ctx.DeltaTime);
		HashSet<string> hashSet = new HashSet<string>();
		float a = float.MaxValue;
		foreach (Term outputTerm in outputTerms)
		{
			float num2 = MaxStorageForLoad(outputTerm.load) - ctx.QuantityInStorage(outputTerm.load);
			float num3 = outputTerm.unitsPerDay * num;
			float num4 = ((num3 == 0f) ? 0f : Mathf.Clamp01(num2 / num3));
			a = Mathf.Min(a, num4);
			if (IsZero(num4))
			{
				hashSet.Add(outputTerm.load.description);
			}
		}
		float b = float.MaxValue;
		foreach (Term inputTerm in inputTerms)
		{
			float num5 = ctx.QuantityInStorage(inputTerm.load);
			float num6 = inputTerm.unitsPerDay * num;
			float num7 = ((num6 == 0f) ? 0f : Mathf.Clamp01(num5 / num6));
			a = Mathf.Min(a, num7);
			if (IsZero(num7))
			{
				hashSet.Add(inputTerm.load.description);
			}
		}
		float num8 = Mathf.Min(a, b);
		if (IsZero(num8) && !IsZero(contractMultiplier))
		{
			SetWarning("Production Stopped: " + string.Join(", ", hashSet.OrderBy((string s) => s)));
			return;
		}
		SetWarning(null);
		foreach (Term inputTerm2 in inputTerms)
		{
			float quantity = num8 * num * inputTerm2.unitsPerDay;
			ctx.RemoveFromStorage(inputTerm2.load, quantity);
		}
		foreach (Term outputTerm2 in outputTerms)
		{
			float maxQuantity = MaxStorageForLoad(outputTerm2.load);
			float quantity2 = num8 * num * outputTerm2.unitsPerDay;
			ctx.AddToStorage(outputTerm2.load, quantity2, maxQuantity);
		}
		static bool IsZero(float m)
		{
			return m < 0.001f;
		}
	}

	public override void OrderCars(IIndustryContext ctx)
	{
	}

	private float MaxStorageForLoad(Load load)
	{
		if (_otherComponents == null || _otherComponents.Count == 0)
		{
			_otherComponents = (from c in GetComponentsInChildren<IndustryComponent>()
				where c != this
				select c).ToHashSet();
		}
		return FindMaxStorage(load, _otherComponents).maxStorage;
	}

	public static (float maxStorage, float unloadLoadRate, float spotMeters) FindMaxStorage(Load load, HashSet<IndustryComponent> components)
	{
		foreach (IndustryComponent component in components)
		{
			if (!(component is IndustryLoader industryLoader))
			{
				if (!(component is IndustryUnloader industryUnloader))
				{
					if (component is TeleportLoadingIndustry teleportLoadingIndustry && teleportLoadingIndustry.load == load)
					{
						return (maxStorage: teleportLoadingIndustry.maxStorage, unloadLoadRate: 86400f * teleportLoadingIndustry.carLoadPeriod, spotMeters: teleportLoadingIndustry.trackSpans.Sum((TrackSpan span) => span.Length));
					}
				}
				else if (industryUnloader.load == load)
				{
					return (maxStorage: industryUnloader.maxStorage, unloadLoadRate: industryUnloader.carUnloadRate, spotMeters: industryUnloader.trackSpans.Sum((TrackSpan span) => span.Length));
				}
			}
			else if (industryLoader.load == load)
			{
				return (maxStorage: industryLoader.maxStorage, unloadLoadRate: industryLoader.carLoadRate, spotMeters: industryLoader.trackSpans.Sum((TrackSpan span) => span.Length));
			}
		}
		return (maxStorage: 0f, unloadLoadRate: 0f, spotMeters: 0f);
	}

	private void SetWarning(string warningOrNull)
	{
		if (base.Industry.Storage != null)
		{
			base.Industry.Storage.SetWarning(subIdentifier, warningOrNull);
		}
	}
}
