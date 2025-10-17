using System;
using System.Collections.Generic;
using System.Linq;
using Game;
using Game.State;
using Model.Definition.Data;
using Serilog;
using Track;
using Track.Search;
using UnityEngine;

namespace Model.Ops;

public class TeleportLoadingIndustry : IndustryLoaderBase
{
	[Tooltip("Game seconds between loading cars.")]
	public float carLoadPeriod = 600f;

	public TrackSpan[] inputSpans;

	public TrackSpan[] outputSpans;

	public float carLengthFeet = 50f;

	private Graph _graph;

	private TrainController _trainController;

	private readonly List<Vector3> _routePoints = new List<Vector3>();

	private const string LastLoadedKey = "lastLoaded";

	public override bool WantsAutoDestination(AutoDestinationType type)
	{
		if (type == AutoDestinationType.Empty)
		{
			return !orderEmpties;
		}
		return false;
	}

	public override void Service(IIndustryContext ctx)
	{
		float contractMultiplier = base.Industry.GetContractMultiplier();
		float rate = productionRate * contractMultiplier;
		ctx.AddToStorage(load, IndustryComponent.RateToValue(rate, ctx.DeltaTime), maxStorage);
		float num = ctx.QuantityInStorage(load);
		if ((object)_trainController == null)
		{
			_trainController = TrainController.Shared;
		}
		if ((object)_graph == null)
		{
			_graph = _trainController.graph;
		}
		GameDateTime now = ctx.Now;
		GameDateTime dateTime = ctx.GetDateTime("lastLoaded", default(GameDateTime));
		double num2 = now - dateTime;
		if (num2 < (double)carLoadPeriod)
		{
			return;
		}
		while (num2 > (double)carLoadPeriod)
		{
			float num3 = TeleportLoadOneCar(ctx, num);
			if (num3 < load.ZeroThreshold)
			{
				break;
			}
			num -= num3;
			num2 -= (double)carLoadPeriod;
		}
		ctx.SetDateTime("lastLoaded", now);
	}

	private float TeleportLoadOneCar(IIndustryContext ctx, float qtyAvailableToLoad)
	{
		TrackSpan[] array = outputSpans;
		foreach (TrackSpan span in array)
		{
			if (qtyAvailableToLoad < load.NominalQuantityPerCarLoad)
			{
				break;
			}
			if (!_trainController.FindOpenSpaceFromLower(span, carLengthFeet * 0.3048f, (Car c) => c.EnumerateCoupled().All(IsAcceptableAdjacentCutMember), out var location, out var car))
			{
				continue;
			}
			if (!FindEmptyCar(ctx, out var car2))
			{
				return 0f;
			}
			if (car2.QuantityCapacityOfLoad(load).capacity > qtyAvailableToLoad)
			{
				break;
			}
			using (StateManager.TransactionScope())
			{
				if (!TeleportLoad(car2, location, car, qtyAvailableToLoad, out var actuallyLoaded))
				{
					return 0f;
				}
				ctx.OrderAwayLoaded(new OpsCarAdapter(car2, OpsController.Shared));
				ctx.RemoveFromStorage(load, actuallyLoaded);
				return actuallyLoaded;
			}
		}
		return 0f;
	}

	private bool IsAcceptableAdjacentCutMember(Car car)
	{
		if (Mathf.Abs(car.velocity) < 0.01f && carTypeFilter.Matches(car.CarType))
		{
			return car.GetWaybill(OpsController.Shared)?.Origin?.Identifier == base.Identifier;
		}
		return false;
	}

	private bool TeleportLoad(Car car, Location loc, Car adjacentCar, float availableToLoad, out float actuallyLoaded)
	{
		Location start = car.LocationFor(car.ClosestLogicalEndTo(loc, _graph));
		if (!CheckRouteClear(start, loc, new Car[2] { car, adjacentCar }))
		{
			actuallyLoaded = 0f;
			return false;
		}
		Car.LogicalEnd fromEnd = ((!(car.CoupledTo(Car.LogicalEnd.A) == null)) ? Car.LogicalEnd.B : Car.LogicalEnd.A);
		IEnumerable<Car> cut = from c in car.EnumerateCoupled(fromEnd)
			where c != car
			select c;
		ApplyHandbrakesToCut(cut, moved: false);
		car.SetHandbrake(apply: false);
		if (adjacentCar != null)
		{
			_trainController.MoveCarCoupleTo(car, loc, adjacentCar);
		}
		else
		{
			_trainController.MoveCar(car, loc);
		}
		LoadSlot loadSlot = car.Definition.LoadSlots[0];
		float num = UnityEngine.Random.Range(0.95f, 1f);
		actuallyLoaded = Mathf.Min(loadSlot.MaximumCapacity, availableToLoad) * num;
		car.SetLoadInfo(0, new CarLoadInfo(load.id, actuallyLoaded));
		ApplyHandbrakesToCut(car.EnumerateCoupled(Car.End.F), moved: true);
		return true;
	}

	private bool CheckRouteClear(Location start, Location end, Car[] ignoring)
	{
		try
		{
			_graph.FindPoints(start, end, 10f, base.name, _routePoints);
			foreach (Vector3 routePoint in _routePoints)
			{
				Car car = _trainController.CheckForCarAtPoint(routePoint);
				if (!(car == null) && !ignoring.Contains(car))
				{
					Log.Information("Route not clear: found {car}", car);
					return false;
				}
			}
			return true;
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Error checking route");
			return false;
		}
	}

	private void ApplyHandbrakesToCut(IEnumerable<Car> cut, bool moved)
	{
		List<Car> list = cut.ToList();
		int num = list.Count((Car car) => car.air.handbrakeApplied);
		int count = Mathf.Max(0, 3 - num);
		foreach (Car item in list.Where((Car car) => !car.air.handbrakeApplied).Take(count))
		{
			item.SetHandbrake(apply: true);
		}
		if (!moved)
		{
			return;
		}
		TrainController.ConnectCars(list, endAnglecocksOpen: true);
		foreach (Car item2 in list)
		{
			if (item2.SupportsBleed() && item2.air.BrakeCylinder.Pressure > 0.1f)
			{
				item2.SetBleed();
			}
		}
	}

	private bool FindEmptyCar(IIndustryContext ctx, out Car car)
	{
		TrackSpan[] array = inputSpans;
		foreach (TrackSpan inputSpan in array)
		{
			if (FindEmptyCar(ctx, inputSpan, out car))
			{
				return true;
			}
		}
		car = null;
		return false;
	}

	private bool FindEmptyCar(IIndustryContext ctx, TrackSpan inputSpan, out Car car)
	{
		HashSet<IOpsCar> source = EnumerateCars(ctx, requireWaybill: true).ToHashSet();
		Car leadCar = _trainController.CarsOnSpan(inputSpan).FirstOrDefault();
		if (leadCar == null || source.All((IOpsCar c) => c.Id != leadCar.id) || !CanLoadCar(leadCar))
		{
			car = null;
			return false;
		}
		car = leadCar;
		return true;
	}

	private bool CanLoadCar(Car car)
	{
		if (Mathf.Abs(car.velocity) > 0.1f)
		{
			return false;
		}
		if (car.Definition.LoadSlots.Count <= 0 || !car.Definition.LoadSlots[0].LoadRequirementsMatch(load))
		{
			return false;
		}
		CarLoadInfo? loadInfo = car.GetLoadInfo(0);
		if (!loadInfo.HasValue || loadInfo.Value.LoadId == load.id)
		{
			return true;
		}
		return loadInfo.Value.Quantity < 0.1f;
	}
}
