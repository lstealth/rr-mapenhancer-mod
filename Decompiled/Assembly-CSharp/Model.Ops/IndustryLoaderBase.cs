using Model.Ops.Definition;
using Serilog;
using UnityEngine;

namespace Model.Ops;

public abstract class IndustryLoaderBase : IndustryComponent
{
	public Load load;

	public float productionRate = 1f;

	public float maxStorage = 1f;

	[Tooltip("True if this component should order empties to load.")]
	public bool orderEmpties = true;

	public override void OrderCars(IIndustryContext ctx)
	{
		if (orderEmpties && base.Industry.ShouldOrderCars())
		{
			float contractMultiplier = base.Industry.GetContractMultiplier();
			float num = maxStorage;
			float portionOfDayUntilNextRegularService = ctx.PortionOfDayUntilNextRegularService;
			float num2 = ctx.AvailableCapacityInCars(carTypeFilter, load);
			int num3 = ctx.NumberOfCarsOnOrderEmpties(carTypeFilter);
			float num4 = ctx.QuantityInStorage(load);
			float nominalQuantityPerCarLoad = load.NominalQuantityPerCarLoad;
			float num5 = PredictQuantityProducedInNextDay(contractMultiplier) * portionOfDayUntilNextRegularService;
			float num6 = num4 + num5;
			if (num4 > num * 0.9f && num3 == 0)
			{
				num6 = Mathf.Max(nominalQuantityPerCarLoad, num6);
			}
			Log.Debug("IndustryLoaderBase {ic} OrderCars {emptyCapacity} vs {shippableQuantity}", this, num2, num6);
			for (; num2 < num6; num2 += nominalQuantityPerCarLoad)
			{
				ctx.OrderEmpty(carTypeFilter, null);
			}
		}
	}

	private float PredictQuantityProducedInNextDay(float multiplier)
	{
		if (productionRate > 0.001f)
		{
			return productionRate * multiplier;
		}
		FormulaicIndustryComponent componentInChildren = base.Industry.GetComponentInChildren<FormulaicIndustryComponent>();
		if (componentInChildren != null)
		{
			foreach (FormulaicIndustryComponent.Term outputTerm in componentInChildren.outputTerms)
			{
				if (outputTerm.load == load)
				{
					return outputTerm.unitsPerDay * multiplier;
				}
			}
		}
		Debug.LogWarning("Can't predict quantity produced", this);
		return 0f;
	}

	public override bool AcceptsCarsWithLoad(Load checkLoad)
	{
		return checkLoad == load;
	}
}
