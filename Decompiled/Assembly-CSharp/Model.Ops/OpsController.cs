using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core;
using GalaSoft.MvvmLight.Messaging;
using Game;
using Game.Events;
using Game.Messages;
using Game.State;
using Helpers;
using JetBrains.Annotations;
using Model.Ops.Definition;
using Network;
using Serilog;
using Track;
using Track.Search;
using UI.Common;
using UnityEngine;
using UnityEngine.Pool;

namespace Model.Ops;

public class OpsController : MonoBehaviour, IOpsCarPositionResolver
{
	public class InvalidOpsCarPositionException : Exception
	{
		public string Identifier { get; }

		public InvalidOpsCarPositionException(string identifier)
			: base(identifier)
		{
			Identifier = identifier;
		}
	}

	private class PaymentAnnouncement
	{
		public float LastUpdated;

		public readonly List<IOpsCar> Cars = new List<IOpsCar>();

		public readonly List<(int amount, string name)> SubItems = new List<(int, string)>();

		public int BasePayment;

		public int Total;
	}

	private readonly Dictionary<string, OpsCarPosition> _carPositionLookup = new Dictionary<string, OpsCarPosition>();

	private readonly Dictionary<(Location, Location), float> _distanceInMilesCache = new Dictionary<(Location, Location), float>();

	private readonly Dictionary<OpsCarPosition, Area> _positionToAreaCache = new Dictionary<OpsCarPosition, Area>();

	private readonly Dictionary<Industry, PaymentAnnouncement> _industryToCoalescedPaymentAnnouncements = new Dictionary<Industry, PaymentAnnouncement>();

	private Coroutine _periodicUpdateCoroutine;

	private InterchangeSelector _interchangeSelector;

	[CanBeNull]
	public static OpsController Shared { get; private set; }

	public IEnumerable<Area> Areas => GetComponentsInChildren<Area>();

	public IEnumerable<Interchange> EnabledInterchanges => AllInterchanges.Where((Interchange i) => !i.Disabled && !i.ProgressionDisabled && !i.Industry.ProgressionDisabled);

	public SwitchListController SwitchListController { get; private set; }

	private static TrainController TrainController => TrainController.Shared;

	public Industry[] AllIndustries { get; private set; }

	private IndustryComponent[] AllIndustryComponents { get; set; }

	public Interchange[] AllInterchanges { get; private set; }

	public static string[] ForeignRoads { get; set; }

	public static int InterchangeShuffle => StateManager.Shared.Storage.InterchangeShuffle;

	private void Awake()
	{
		Shared = this;
		SwitchListController = base.gameObject.AddComponent<SwitchListController>();
		SwitchListController.opsController = this;
		base.gameObject.AddComponent<PassengerExpiration>();
		ForeignRoads = ForeignRoadsReader.ReadForeignRoads();
	}

	private void OnEnable()
	{
		RebuildCollections();
		Messenger.Default.Register<IndustriesDidChange>(this, delegate
		{
			RebuildCollections();
		});
		Messenger.Default.Register<TimeDayDidChange>(this, DayDidChange);
		Messenger.Default.Register<TimeMinuteDidChange>(this, delegate
		{
			CheckServiceInterchanges();
		});
		_periodicUpdateCoroutine = StartCoroutine(PeriodicUpdate());
	}

	private void OnDisable()
	{
		Messenger.Default.Unregister(this);
		StopCoroutine(_periodicUpdateCoroutine);
		_periodicUpdateCoroutine = null;
	}

	private void OnDestroy()
	{
		if ((object)Shared == this)
		{
			Shared = null;
		}
	}

	public Industry IndustryForId(string industryId)
	{
		return AllIndustries.FirstOrDefault((Industry i) => i.identifier == industryId);
	}

	private IEnumerator PeriodicUpdate()
	{
		WaitForSecondsRealtime wait = new WaitForSecondsRealtime(0.25f);
		while (true)
		{
			yield return wait;
			AnnounceCoalescedPayments();
		}
	}

	public void PostRestoreProperties()
	{
		try
		{
			CheckLoads();
			CheckWaybills();
			RebuildPopulations();
			CheckServiceInterchanges();
			CheckOneEnabledInterchange();
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Exception during OpsController.PostRestoreProperties");
		}
	}

	private void CheckOneEnabledInterchange()
	{
		if (!EnabledInterchanges.Any())
		{
			Log.Error("No enabled interchanges!");
			ModalAlertController.PresentOkay("No Interchanges Enabled", "Use the Company window Locations tab to enable at least one interchange for proper game operation!");
		}
	}

	private void DayDidChange(TimeDayDidChange _)
	{
		if (StateManager.IsHost)
		{
			GameDateTime now = TimeWeather.Now.WithHours(0f);
			Industry[] allIndustries = AllIndustries;
			UpdatePerformance(now);
			Industry[] array = allIndustries;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].DailyReceivables(now);
			}
			array = allIndustries;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].RollToNextContract();
			}
			array = allIndustries;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].DailyPayables(now);
			}
		}
	}

	[ContextMenu("Rebuild Populations")]
	public void RebuildPopulations()
	{
		foreach (Area area in Areas)
		{
			PassengerStop componentInChildren = area.GetComponentInChildren<PassengerStop>();
			if (componentInChildren == null || componentInChildren.ProgressionDisabled)
			{
				continue;
			}
			int num = 0;
			GameDateTime now = TimeWeather.Now;
			AnimationCurve industrySpanCarLengthsToEmployees = TrainController.config.industrySpanCarLengthsToEmployees;
			foreach (Industry item in area.Industries.Where((Industry i) => !i.ProgressionDisabled && i.HasActiveContract(now)))
			{
				Contract value = item.Contract.Value;
				float num2 = item.TrackDisplayables.Sum((IIndustryTrackDisplayable td) => td.TrackSpans.Sum((TrackSpan sp) => sp.Length)) / 13f;
				float num3 = industrySpanCarLengthsToEmployees.Evaluate(num2);
				int num4 = Mathf.RoundToInt(num3 * value.Percent);
				Log.Debug("Population: {industry} has {employees}/{maxEmployees} ({spanCarLengths} cars)", item.name, num4, (int)num3, num2);
				num += num4;
			}
			if (num > 0)
			{
				Log.Debug("Population: {area} has {population} population", area.name, num);
			}
			componentInChildren.AdditionalPopulation = num;
		}
	}

	public void CheckWaybills()
	{
		bool isSandbox = StateManager.IsSandbox;
		bool isHost = StateManager.IsHost;
		foreach (Car car in TrainController.Cars)
		{
			try
			{
				car.CheckWaybill(this);
			}
			catch (Exception propertyValue)
			{
				Log.Warning("CheckWaybills: Bad waybill for {car}, got {e}", car, propertyValue);
				if (isHost)
				{
					if (isSandbox || car.IsOwnedByPlayer)
					{
						car.SetWaybillAuto(null, this);
						continue;
					}
					Interchange interchange = EnabledInterchanges.First();
					Waybill value = new Waybill(TimeWeather.Now, null, interchange, 0, completed: false, null, 0);
					car.SetWaybill(value);
				}
			}
		}
	}

	private void CheckLoads()
	{
		CarPrototypeLibrary instance = CarPrototypeLibrary.instance;
		bool isHost = StateManager.IsHost;
		foreach (Car car in TrainController.Cars)
		{
			try
			{
				for (int i = 0; i < car.Definition.LoadSlots.Count; i++)
				{
					CarLoadInfo? loadInfo = car.GetLoadInfo(i);
					if (!loadInfo.HasValue)
					{
						continue;
					}
					Load load = instance.LoadForId(loadInfo.Value.LoadId);
					if (load == null)
					{
						Log.Warning("{car}: Found invalid load in slotIndex {slot}.", car, i);
						if (isHost)
						{
							car.SetLoadInfo(i, null);
						}
						continue;
					}
					Waybill? waybill = car.Waybill;
					if (waybill.HasValue)
					{
						Waybill valueOrDefault = waybill.GetValueOrDefault();
						IndustryComponent industryComponent = IndustryComponentForPosition(valueOrDefault.Destination);
						if (!(industryComponent == null) && !industryComponent.AcceptsCarsWithLoad(load))
						{
							Log.Warning("{car}: {ic} does not accept load {load} in slotIndex {slot}; returning to interchange.", car, industryComponent, load, i);
							OpsCarPosition carPosition = PositionForCar(car) ?? valueOrDefault.Origin ?? valueOrDefault.Destination;
							WaybillCarToInterchange(new OpsCarAdapter(car, this), carPosition, null, noPayment: false);
						}
					}
				}
			}
			catch (Exception exception)
			{
				Log.Error(exception, "CheckLoads: Error while checking load for {car}", car);
			}
		}
	}

	private void RebuildCollections()
	{
		AllIndustries = base.transform.GetComponentsInChildren<Industry>();
		AllIndustryComponents = base.transform.GetComponentsInChildren<IndustryComponent>();
		AllInterchanges = base.transform.GetComponentsInChildren<Interchange>();
		_carPositionLookup.Clear();
		Industry[] allIndustries = AllIndustries;
		for (int i = 0; i < allIndustries.Length; i++)
		{
			IndustryComponent[] componentsInChildren = allIndustries[i].GetComponentsInChildren<IndustryComponent>();
			foreach (IndustryComponent industryComponent in componentsInChildren)
			{
				_carPositionLookup[industryComponent.Identifier] = industryComponent;
			}
		}
		_positionToAreaCache.Clear();
	}

	public void RemoveCar(string carId)
	{
		try
		{
			SwitchListController.RemoveCar(carId);
		}
		catch (Exception ex)
		{
			Debug.LogError(ex);
			Log.Error(ex, "Exception while removing car {carId}", carId);
		}
	}

	public void RequestOps(IPlayer sender, RequestOps request)
	{
		switch (request.command)
		{
		case Game.Messages.RequestOps.Command.Sweep:
			Sweep(request.query);
			Multiplayer.Broadcast(sender.Name + " swept ops.");
			break;
		default:
			throw new ArgumentOutOfRangeException("request", request, null);
		case Game.Messages.RequestOps.Command.Step:
			break;
		}
	}

	public IOpsCar CarForId(string carId)
	{
		Car car = TrainController.CarForId(carId);
		if (car == null)
		{
			return null;
		}
		return new OpsCarAdapter(car, this);
	}

	public IEnumerable<IOpsCar> CarsInArea(Area area)
	{
		Graph graph = TrainController.graph;
		foreach (Car car in TrainController.Cars)
		{
			if (!car.IsInBardo)
			{
				bool flag;
				try
				{
					flag = area.Contains(car.GetCenterPosition(graph));
				}
				catch (Exception exception)
				{
					Log.Error(exception, "Error checking whether car {car} is in area {area}", car, area);
					continue;
				}
				if (flag)
				{
					yield return new OpsCarAdapter(car, this);
				}
			}
		}
	}

	[CanBeNull]
	public Area AreaForCarPosition(OpsCarPosition position)
	{
		if (!_positionToAreaCache.TryGetValue(position, out var value))
		{
			foreach (Area area in Areas)
			{
				if (area.Contains(position))
				{
					_positionToAreaCache[position] = area;
					value = area;
					return value;
				}
			}
		}
		return value;
	}

	public Area ClosestArea(Car car)
	{
		try
		{
			return ClosestAreaForGamePosition(car.GetCenterPosition(TrainController.graph));
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Error finding closest area for car {car}", car);
			return null;
		}
	}

	[CanBeNull]
	public Area ClosestAreaForGamePosition(Vector3 position)
	{
		Area result = null;
		float num = float.MaxValue;
		foreach (Area area in Areas)
		{
			float num2 = Vector3.Distance(position, WorldTransformer.WorldToGame(area.transform.position));
			num2 -= area.radius;
			if (!(num2 > num))
			{
				result = area;
				num = num2;
			}
		}
		return result;
	}

	public void AddOrderForOutboundEmptyCar(IOpsCar car, OpsCarPosition carPosition, string orderTag, bool noPayment)
	{
		WaybillCarToInterchange(car, carPosition, orderTag, noPayment);
	}

	public void AddOrderForOutboundLoadedCar(IOpsCar car, OpsCarPosition carPosition, string orderTag, bool noPayment)
	{
		WaybillCarToInterchange(car, carPosition, orderTag, noPayment);
	}

	private void WaybillCarToInterchange(IOpsCar car, OpsCarPosition carPosition, string orderTag, bool noPayment)
	{
		Interchange interchange = InterchangeForPosition(carPosition, car.Waybill?.Origin);
		int paymentOnArrival = ((!noPayment) ? PaymentForMove(carPosition, interchange, car.WeightInTons) : 0);
		int graceDays = CalculateGraceDays(carPosition, interchange);
		CarExtensions.SetWaybill(waybill: new Waybill(TimeWeather.Now, carPosition, interchange, paymentOnArrival, completed: false, orderTag, graceDays), car: ModelCarForCar(car));
	}

	public bool AddOrderForInboundCar(CarTypeFilter carTypeFilter, [CanBeNull] Load load, OpsCarPosition position, Industry industry, string orderTag, bool noPayment, out float quantity)
	{
		Car car = FindExistingCarForInboundOrder(carTypeFilter, load, out quantity);
		if (car != null)
		{
			int tons = Mathf.CeilToInt(car.Weight / 2000f);
			int paymentOnArrival = ((!noPayment) ? PaymentForMove(car.OpsLocation, (Location)position, tons) : 0);
			int graceDays = CalculateGraceDays(car.OpsLocation, (Location)position);
			Waybill value = new Waybill(TimeWeather.Now, PositionForCar(car), position, paymentOnArrival, completed: false, orderTag, graceDays);
			car.SetWaybill(value);
			return true;
		}
		if (load != null && !load.importable)
		{
			return false;
		}
		Interchange interchange = InterchangeForPosition(position, null);
		quantity = NominalQuantityForOrder(carTypeFilter, load) * 1f;
		Order order = new Order(carTypeFilter, load, position, 1, orderTag, noPayment);
		interchange.AddOrder(order);
		return true;
	}

	private float NominalQuantityForOrder(CarTypeFilter carTypeFilter, [CanBeNull] Load load)
	{
		if (load == null)
		{
			return 0f;
		}
		return load.NominalQuantityPerCarLoad;
	}

	[CanBeNull]
	private Car FindExistingCarForInboundOrder(CarTypeFilter carTypeFilter, [CanBeNull] Load load, out float quantity)
	{
		quantity = 0f;
		return null;
	}

	private bool LoadMatches(Car car, [CanBeNull] Load load, out float quantity)
	{
		quantity = 0f;
		CarLoadInfo? loadInfo = car.GetLoadInfo(0);
		if (load == null)
		{
			return !loadInfo.HasValue;
		}
		if (!loadInfo.HasValue)
		{
			return false;
		}
		quantity = loadInfo.Value.Quantity;
		return loadInfo.Value.LoadId == load.id;
	}

	public OpsCarPosition ResolveOpsCarPosition(string opsCarPositionIdentifier)
	{
		if (_carPositionLookup.TryGetValue(opsCarPositionIdentifier, out var value))
		{
			return value;
		}
		throw new InvalidOpsCarPositionException(opsCarPositionIdentifier);
	}

	public float QuantityOnOrder(OpsCarPosition destination, Load load)
	{
		float num = (from order in AllInterchanges.SelectMany((Interchange interchange) => interchange.Orders)
			where order.Load == load && order.Destination.Equals(destination)
			select order).Sum((IOrder order) => (float)order.CarCount * NominalQuantityForOrder(order.CarTypeFilter, load));
		float num2 = CarsWaybilledTo(destination, (CarTypeFilter)null).Sum((Car car) => LoadQuantityMatching(car, load));
		return num + num2;
	}

	public float AvailableCapacityInCars(IndustryComponent destination, Load load, CarTypeFilter carTypeFilter)
	{
		float num = (float)(from order in AllInterchanges.SelectMany((Interchange interchange) => interchange.Orders)
			where order.Load == null && order.Destination.Equals(destination) && order.CarTypeFilter.Overlaps(carTypeFilter)
			select order).Sum((IOrder order) => order.CarCount) * load.NominalQuantityPerCarLoad;
		float num2 = CarsWaybilledTo(destination, carTypeFilter).Sum(delegate(Car car)
		{
			float maximumCapacity = car.Definition.LoadSlots[0].MaximumCapacity;
			CarLoadInfo? loadInfo = car.GetLoadInfo(0);
			if (!loadInfo.HasValue)
			{
				return maximumCapacity;
			}
			CarLoadInfo value = loadInfo.Value;
			return (value.LoadId == load.id) ? (maximumCapacity - value.Quantity) : 0f;
		});
		return num + num2;
	}

	public int CountOrdersMatching(OpsCarPosition destination, [CanBeNull] Load load, CarTypeFilter carTypeFilter)
	{
		int num = (from order in AllInterchanges.SelectMany((Interchange interchange) => interchange.Orders)
			where order.Load == load && order.Destination.Equals(destination) && order.CarTypeFilter.Overlaps(carTypeFilter)
			select order).Sum((IOrder order) => order.CarCount);
		int num2 = CarsWaybilledTo(destination, carTypeFilter).Sum((Car car) => CarsMatching(car, load));
		return num + num2;
	}

	public int CountOrdersMatching(OpsCarPosition destination, string orderTag)
	{
		int num = (from order in AllInterchanges.SelectMany((Interchange interchange) => interchange.Orders)
			where order.Tag == orderTag && order.Destination.Equals(destination)
			select order).Sum((IOrder order) => order.CarCount);
		int num2 = CarsWaybilledTo(destination, orderTag).Count();
		return num + num2;
	}

	public int CountOrdersForIndustry(Industry industry)
	{
		HashSet<OpsCarPosition> destinations = industry.Components.Select((Func<IndustryComponent, OpsCarPosition>)((IndustryComponent ic) => ic)).ToHashSet();
		int num = (from order in AllInterchanges.SelectMany((Interchange interchange) => interchange.Orders)
			where destinations.Contains(order.Destination)
			select order).Sum((IOrder order) => order.CarCount);
		int num2 = TrainController.Cars.Count(delegate(Car car)
		{
			Waybill? waybill = car.GetWaybill(this);
			return waybill.HasValue && destinations.Contains(waybill.Value.Destination);
		});
		return num + num2;
	}

	private IEnumerable<Car> CarsWaybilledTo(OpsCarPosition destination, [CanBeNull] CarTypeFilter carTypeFilter)
	{
		return TrainController.Cars.Where(delegate(Car car)
		{
			if (carTypeFilter != null && !carTypeFilter.Matches(car.CarType))
			{
				return false;
			}
			Waybill? waybill = car.GetWaybill(this);
			return waybill.HasValue && waybill.Value.Destination.Equals(destination);
		});
	}

	private IEnumerable<Car> CarsWaybilledTo(OpsCarPosition destination, string orderTag)
	{
		bool filterTag = !string.IsNullOrEmpty(orderTag);
		return TrainController.Cars.Where(delegate(Car car)
		{
			Waybill? waybill = car.GetWaybill(this);
			if (!waybill.HasValue)
			{
				return false;
			}
			Waybill value = waybill.Value;
			return value.Destination.Equals(destination) && (!filterTag || value.Tag == orderTag);
		});
	}

	private float LoadQuantityMatching(Car car, [CanBeNull] Load load)
	{
		if (load == null)
		{
			float result = NominalQuantityForOrder(null, load);
			CarLoadInfo? loadInfo = car.GetLoadInfo(0);
			if (!loadInfo.HasValue)
			{
				return result;
			}
			if (!(loadInfo.Value.Quantity < 0.001f))
			{
				return 0f;
			}
			return result;
		}
		return (from slotIndex in Enumerable.Range(0, car.Definition.LoadSlots.Count)
			select car.GetLoadInfo(slotIndex)).Sum(delegate(CarLoadInfo? carLoadInfo)
		{
			if (!carLoadInfo.HasValue)
			{
				return 0f;
			}
			return (carLoadInfo.Value.LoadId == load.id) ? carLoadInfo.Value.Quantity : 0f;
		});
	}

	private int CarsMatching(Car car, [CanBeNull] Load load)
	{
		if (load == null)
		{
			CarLoadInfo? loadInfo = car.GetLoadInfo(0);
			if (!loadInfo.HasValue)
			{
				return 1;
			}
			if (!(loadInfo.Value.Quantity < 0.001f))
			{
				return 0;
			}
			return 1;
		}
		return (from slotIndex in Enumerable.Range(0, car.Definition.LoadSlots.Count)
			select car.GetLoadInfo(slotIndex)).Sum(delegate(CarLoadInfo? carLoadInfo)
		{
			if (!carLoadInfo.HasValue)
			{
				return 0;
			}
			return (carLoadInfo.Value.LoadId == load.id && carLoadInfo.Value.Quantity > 0.001f) ? 1 : 0;
		});
	}

	private Interchange InterchangeForPosition(OpsCarPosition position, OpsCarPosition? origin)
	{
		if (_interchangeSelector == null)
		{
			_interchangeSelector = new InterchangeSelector();
		}
		Interchange[] array = EnabledInterchanges.ToArray();
		if (origin.HasValue)
		{
			Interchange[] array2 = array;
			foreach (Interchange interchange in array2)
			{
				if (interchange.Identifier == origin.Value.Identifier)
				{
					return interchange;
				}
			}
		}
		return _interchangeSelector.InterchangeForPosition(position, array);
	}

	public int PaymentForMove(OpsCarPosition start, OpsCarPosition end, int tons)
	{
		return PaymentForMove((Location)start, (Location)end, tons);
	}

	private int PaymentForMove(Location start, Location end, int tons)
	{
		int num = Mathf.CeilToInt(Mathf.Pow(DistanceInMiles(start, end), 0.5f) * 4f);
		int num2 = Mathf.CeilToInt((float)tons * 0.25f);
		return 50 + num + num2;
	}

	public int CalculateGraceDays(OpsCarPosition start, OpsCarPosition end)
	{
		return CalculateGraceDays((Location)start, (Location)end);
	}

	private int CalculateGraceDays(Location start, Location end)
	{
		float num = DistanceInMiles(start, end);
		if (!(num > 40f))
		{
			if (num > 20f)
			{
				return 1;
			}
			return 0;
		}
		return 2;
	}

	private float DistanceInMiles(Location locA, Location locB)
	{
		if (_distanceInMilesCache.TryGetValue((locA, locB), out var value))
		{
			return value;
		}
		value = ((!TrainController.graph.TryFindDistance(locA, locB, out var totalDistance, out var _)) ? 0f : (totalDistance * 0.0006213712f));
		_distanceInMilesCache[(locA, locB)] = value;
		return value;
	}

	private Vector3 PointForCarPosition(OpsCarPosition position)
	{
		return WorldTransformer.WorldToGame(IndustryComponentForPosition(position).transform.position);
	}

	private IndustryComponent IndustryComponentForPosition(OpsCarPosition position)
	{
		IndustryComponent[] allIndustryComponents = AllIndustryComponents;
		foreach (IndustryComponent industryComponent in allIndustryComponents)
		{
			if (industryComponent.Identifier == position.Identifier)
			{
				return industryComponent;
			}
		}
		return null;
	}

	public string NameForPosition(OpsCarPosition position)
	{
		return IndustryComponentForPosition(position).DisplayName;
	}

	public Vector3 PointForPosition(OpsCarPosition position)
	{
		return position.Spans.Aggregate(Vector3.zero, (Vector3 current, TrackSpan span) => current + span.GetCenterPoint()) * (1f / (float)position.Spans.Length);
	}

	public OpsCarPosition? PositionForCar(IOpsCar opsCar)
	{
		Car car = TrainController.CarForId(opsCar.Id);
		return PositionForCar(car);
	}

	public OpsCarPosition? PositionForCar(Car car)
	{
		IndustryComponent[] allIndustryComponents = AllIndustryComponents;
		foreach (IndustryComponent industryComponent in allIndustryComponents)
		{
			foreach (Car item in CarsAtPosition(industryComponent))
			{
				if (item == car)
				{
					return industryComponent;
				}
			}
		}
		return null;
	}

	public IEnumerable<Car> CarsAtPosition(OpsCarPosition position)
	{
		TrainController trainController = TrainController;
		List<Car> cars = CollectionPool<List<Car>, Car>.Get();
		TrackSpan[] spans = position.Spans;
		foreach (TrackSpan span in spans)
		{
			trainController.GetCarsOnSpan(span, cars);
			foreach (Car item in cars)
			{
				yield return item;
			}
		}
		CollectionPool<List<Car>, Car>.Release(cars);
	}

	private Car ModelCarForCar(IOpsCar opsCar)
	{
		return TrainController.CarForId(opsCar.Id);
	}

	public string Sweep(string query)
	{
		if (query == "*")
		{
			return SweepAll();
		}
		Car car = TrainController.CarForString(query);
		if (car == null)
		{
			return "Car not found: " + query;
		}
		if (Sweep(car))
		{
			return "Swept " + car.DisplayName;
		}
		return null;
	}

	private string SweepAll()
	{
		int num = 0;
		foreach (Car car in TrainController.Cars)
		{
			try
			{
				if (Sweep(car))
				{
					num++;
				}
			}
			catch (Exception exception)
			{
				Log.Error(exception, "Error sweeping {car}", car);
			}
		}
		return $"Swept {num} cars.";
	}

	public bool Sweep(Car car)
	{
		Waybill? waybill = car.GetWaybill(this);
		if (!waybill.HasValue)
		{
			return false;
		}
		Waybill value = waybill.Value;
		if (value.Completed)
		{
			return false;
		}
		bool num = MoveCarTo(car, value.Destination);
		if (!num)
		{
			Console.Log($"Couldn't sweep {Hyperlink.To(car)} to {value.Destination.DisplayName}.");
		}
		return num;
	}

	private bool MoveCarTo(Car car, OpsCarPosition position)
	{
		TrainController trainController = TrainController;
		TrackSpan[] spans = position.Spans;
		foreach (TrackSpan span in spans)
		{
			if (trainController.FindOpenSpaceFromLower(span, car.carLength, (Car _) => true, out var location, out var car2))
			{
				if (car2 != null)
				{
					trainController.MoveCarCoupleTo(car, location, car2);
				}
				else
				{
					trainController.MoveCar(car, location);
				}
				return true;
			}
		}
		return false;
	}

	public void AppendCarInfo(Car car, StringBuilder sb)
	{
		Waybill? waybill = car.GetWaybill(this);
		if (waybill.HasValue)
		{
			Waybill value = waybill.Value;
			sb.AppendFormat("Waybill to {0}, pay {1:C}", value.Destination.Identifier, value.PaymentOnArrival);
		}
	}

	public void RequestIndustriesOrderCars()
	{
		List<Area> list = Areas.ToList();
		list.Shuffle();
		List<Industry> list2 = (from i in list.SelectMany((Area a) => a.Industries)
			where !i.ProgressionDisabled
			select i).ToList();
		System.Random rnd = new System.Random();
		int interchangeShuffle = InterchangeShuffle;
		for (int num = 0; num < interchangeShuffle; num++)
		{
			list2.OverhandShuffle(rnd, 2, 4);
		}
		foreach (Industry item in list2)
		{
			item.OrderCars();
		}
	}

	public void PopulateSnapshotForSave(ref Snapshot snapshot)
	{
		SwitchListController.PopulateSnapshot(ref snapshot);
	}

	public void RestoreSwitchLists(Dictionary<string, SwitchList> switchLists)
	{
		SwitchListController.RestoreSwitchLists(switchLists);
	}

	public IEnumerable<(Car Car, Waybill Waybill)> GetOpenWaybills()
	{
		return from car in TrainController.Cars
			select (Car: car, Waybill: car.GetWaybill(this)) into tuple
			where tuple.Waybill.HasValue
			select (Car: tuple.Car, Waybill: tuple.Waybill.Value) into tuple
			where !tuple.Waybill.Completed
			select tuple;
	}

	private void UpdatePerformance(GameDateTime now)
	{
		RebuildPopulations();
		IEnumerable<Waybill> enumerable = from tuple in GetOpenWaybills()
			select tuple.Waybill into w
			where w.PaymentOnArrival > 0
			select w;
		Dictionary<Industry, List<float>> industryToWaybillAges = new Dictionary<Industry, List<float>>();
		foreach (Waybill item in enumerable)
		{
			float totalDays = now.TotalDays;
			GameDateTime created = item.Created;
			float ageInDays = totalDays - created.TotalDays - (float)item.GraceDays;
			Record(item.Destination);
			if (item.Origin.HasValue)
			{
				Record(item.Origin.Value);
			}
			void Record(OpsCarPosition industryPosition)
			{
				Industry industry = IndustryComponentForPosition(industryPosition).Industry;
				if (!industryToWaybillAges.TryGetValue(industry, out var value2))
				{
					industryToWaybillAges.Add(industry, value2 = new List<float>());
				}
				value2.Add(ageInDays);
			}
		}
		List<float> list = new List<float>();
		foreach (Industry item2 in AllIndustries.Where((Industry i) => !i.ProgressionDisabled))
		{
			if (!industryToWaybillAges.TryGetValue(item2, out var value))
			{
				value = list;
			}
			item2.UpdatePerformance(value, now);
		}
	}

	public void RewriteWaybills(string fromPrefix, string toPrefix)
	{
		foreach (Car car in TrainController.Cars)
		{
			Waybill? waybill = car.GetWaybill(this);
			if (waybill.HasValue && waybill.Value.Origin.HasValue && waybill.Value.Origin.Value.Identifier.StartsWith(fromPrefix))
			{
				Waybill value = waybill.Value;
				Waybill waybill2 = value;
				waybill2.Origin = ResolveRewritten(value.Origin.Value.Identifier);
				Log.Information("Rewrite waybill origin for {car}: {waybill} -> {rewritten}", car, value, waybill2);
				car.SetWaybill(waybill2);
			}
			if (waybill.HasValue && waybill.Value.Destination.Identifier.StartsWith(fromPrefix))
			{
				Waybill value2 = waybill.Value;
				Waybill waybill3 = value2;
				waybill3.Destination = ResolveRewritten(value2.Destination.Identifier);
				Log.Information("Rewrite waybill dest for {car}: {waybill} -> {rewritten}", car, value2, waybill3);
				car.SetWaybill(waybill3);
			}
			OpsCarPosition? autoDestination = car.GetAutoDestination(AutoDestinationType.Empty, this);
			if (autoDestination.HasValue && autoDestination.Value.Identifier.StartsWith(fromPrefix))
			{
				OpsCarPosition opsCarPosition = ResolveRewritten(autoDestination.Value.Identifier);
				Log.Information("Rewrite autodest empty for {car}: {waybill} -> {rewritten}", car, autoDestination.Value, opsCarPosition);
				car.SetAutoDestination(AutoDestinationType.Empty, opsCarPosition);
			}
			autoDestination = car.GetAutoDestination(AutoDestinationType.Load, this);
			if (autoDestination.HasValue && autoDestination.Value.Identifier.StartsWith(fromPrefix))
			{
				OpsCarPosition opsCarPosition2 = ResolveRewritten(autoDestination.Value.Identifier);
				Log.Information("Rewrite autodest load for {car}: {waybill} -> {rewritten}", car, autoDestination.Value, opsCarPosition2);
				car.SetAutoDestination(AutoDestinationType.Load, opsCarPosition2);
			}
		}
		OpsCarPosition ResolveRewritten(string identifier)
		{
			return ResolveOpsCarPosition(identifier.Replace(fromPrefix, toPrefix));
		}
	}

	public void ReturnWaybillsFrom(Industry industry)
	{
		string value = industry.identifier + ".";
		foreach (Car car in TrainController.Cars)
		{
			Waybill? waybill = car.GetWaybill(this);
			if (!waybill.HasValue)
			{
				continue;
			}
			try
			{
				OpsCarPosition? autoDestination = car.GetAutoDestination(AutoDestinationType.Empty, this);
				if (autoDestination.HasValue && autoDestination.Value.Identifier.StartsWith(value))
				{
					Log.Information("Clear autodest empty for {car}: {dest}", car, autoDestination.Value);
					car.SetAutoDestination(AutoDestinationType.Empty, null);
				}
				autoDestination = car.GetAutoDestination(AutoDestinationType.Load, this);
				if (autoDestination.HasValue && autoDestination.Value.Identifier.StartsWith(value))
				{
					Log.Information("Clear autodest load for {car}: {dest}", car, autoDestination.Value);
					car.SetAutoDestination(AutoDestinationType.Load, null);
				}
				Waybill value2 = waybill.Value;
				if (value2.Destination.Identifier.StartsWith(value))
				{
					if (car.IsOwnedByPlayer)
					{
						car.SetWaybillAuto(null, this);
						continue;
					}
					Interchange interchange = InterchangeForPosition(industry.Components.First(), value2.Origin);
					car.SetWaybill(new Waybill(TimeWeather.Now, null, interchange, 0, completed: false, null, 0));
				}
			}
			catch (Exception exception)
			{
				Log.Error(exception, "Exception in ReturnWaybillsFrom");
			}
		}
	}

	private void CheckServiceInterchanges()
	{
		if (!StateManager.IsHost)
		{
			return;
		}
		GameDateTime now = TimeWeather.Now;
		List<Interchange> list = new List<Interchange>();
		Interchange[] allInterchanges = AllInterchanges;
		foreach (Interchange interchange in allInterchanges)
		{
			if (!(interchange.GetNextServiceTime(now, out var style) > now))
			{
				if (style == Interchange.NextServiceStyle.Extra)
				{
					interchange.ScheduleExtra(null);
				}
				list.Add(interchange);
			}
		}
		if (!list.Any())
		{
			return;
		}
		EnsureConsistency();
		Log.Debug("Serving interchanges: {interchanges}", list);
		foreach (Interchange item in list)
		{
			item.PrepareToService();
		}
		try
		{
			RequestIndustriesOrderCars();
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Exception from RequestIndustriesOrderCars");
		}
		foreach (Interchange item2 in list)
		{
			IndustryContext industryContext = item2.CreateContext(now, 0f);
			item2.ServeInterchange(industryContext);
			if (item2.Orders.Count > 0)
			{
				Interchange.NextServiceStyle style2;
				GameDateTime nextServiceTime = item2.GetNextServiceTime(now, out style2, dailyOnly: true);
				GameDateTime gameDateTime = Interchange.NextAvailableServiceTime(industryContext.Now);
				Hyperlink hyperlink = Hyperlink.To(item2.Industry);
				if (Interchange.NextAvailableServiceTime(gameDateTime) <= nextServiceTime)
				{
					item2.ScheduleExtra(gameDateTime);
					Multiplayer.Broadcast($"{hyperlink} scheduled to be served again at {gameDateTime.TimeString()}.");
				}
				else
				{
					Multiplayer.Broadcast($"{hyperlink} will be served again at its daily time of {nextServiceTime.TimeString()}.");
				}
			}
		}
	}

	public bool TryDecodeBardo(string bardoId, out OpsCarPosition opsCarPosition, out string returnTo, out string consignee)
	{
		IndustryComponent industryComponent = AllIndustryComponents.FirstOrDefault((IndustryComponent ic) => ic.Identifier == bardoId);
		if (industryComponent == null)
		{
			opsCarPosition = default(OpsCarPosition);
			returnTo = null;
			consignee = null;
			return false;
		}
		opsCarPosition = industryComponent;
		if (industryComponent is InterchangedIndustryLoader interchangedIndustryLoader)
		{
			returnTo = interchangedIndustryLoader.InterchangeName;
			consignee = interchangedIndustryLoader.name;
		}
		else
		{
			returnTo = (consignee = industryComponent.DisplayName);
		}
		return true;
	}

	public bool TryGetIndustryComponent<TIndustryComponent>(string id, out TIndustryComponent output)
	{
		IndustryComponent[] allIndustryComponents = AllIndustryComponents;
		foreach (IndustryComponent industryComponent in allIndustryComponents)
		{
			if (!(industryComponent.Identifier != id))
			{
				if (industryComponent is TIndustryComponent val)
				{
					output = val;
					return true;
				}
				throw new ArgumentException($"Found IC {industryComponent} but type mismatched");
			}
		}
		output = default(TIndustryComponent);
		return false;
	}

	public void AddCoalescedPaymentAnnouncement(IOpsCar car, Industry industry, int basePayment, (int amount, string name)[] subItems, int total)
	{
		if (!_industryToCoalescedPaymentAnnouncements.TryGetValue(industry, out var value))
		{
			_industryToCoalescedPaymentAnnouncements.Add(industry, value = new PaymentAnnouncement());
		}
		value.LastUpdated = Time.unscaledTime;
		value.Cars.Add(car);
		value.BasePayment += basePayment;
		value.Total += total;
		for (int i = 0; i < subItems.Length; i++)
		{
			(int, string) item = subItems[i];
			bool flag = false;
			for (int j = 0; j < value.SubItems.Count; j++)
			{
				if (value.SubItems[j].name == item.Item2)
				{
					(int, string) value2 = value.SubItems[j];
					value2.Item1 += item.Item1;
					value.SubItems[j] = value2;
					flag = true;
				}
			}
			if (!flag)
			{
				value.SubItems.Add(item);
			}
		}
	}

	private void AnnounceCoalescedPayments()
	{
		HashSet<Industry> hashSet = new HashSet<Industry>();
		float unscaledTime = Time.unscaledTime;
		foreach (var (industry2, paymentAnnouncement2) in _industryToCoalescedPaymentAnnouncements)
		{
			if (unscaledTime - paymentAnnouncement2.LastUpdated < 1f)
			{
				continue;
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("Payment for delivery of ");
			if (paymentAnnouncement2.Cars.Count > 1)
			{
				stringBuilder.Append(paymentAnnouncement2.Cars.Count.Pluralize("car"));
			}
			else
			{
				stringBuilder.Append(Hyperlink.To(paymentAnnouncement2.Cars[0]));
			}
			stringBuilder.Append($" to {Hyperlink.To(industry2)}: {paymentAnnouncement2.BasePayment:C0}");
			foreach (var subItem in paymentAnnouncement2.SubItems)
			{
				var (num, _) = subItem;
				if (num >= 0)
				{
					if (num != 0)
					{
						stringBuilder.Append($" + {subItem.amount:C0} {subItem.name}");
					}
				}
				else
				{
					stringBuilder.Append($" - {-subItem.amount:C0} {subItem.name}");
				}
			}
			if (paymentAnnouncement2.Total != paymentAnnouncement2.BasePayment)
			{
				stringBuilder.Append($" = {paymentAnnouncement2.Total:C0}");
			}
			string text = stringBuilder.ToString();
			Log.Debug(text);
			Multiplayer.Broadcast(text);
			hashSet.Add(industry2);
		}
		foreach (Industry item in hashSet)
		{
			_industryToCoalescedPaymentAnnouncements.Remove(item);
		}
	}

	private void EnsureConsistency()
	{
		Industry[] allIndustries = AllIndustries;
		for (int i = 0; i < allIndustries.Length; i++)
		{
			foreach (IndustryComponent component in allIndustries[i].Components)
			{
				try
				{
					component.EnsureConsistency();
				}
				catch (Exception exception)
				{
					Log.Error(exception, "Exception in EnsureConsistency on {ic}", component);
				}
			}
		}
	}

	public bool CanWaybillTo(Car car, OpsCarPosition destination)
	{
		IndustryComponent industryComponent = IndustryComponentForPosition(destination);
		if (industryComponent == null)
		{
			return false;
		}
		return industryComponent.carTypeFilter.Matches(car.CarType);
	}

	public bool TryGetActiveContract(OpsCarPosition position, out Contract contract)
	{
		IndustryComponent industryComponent = IndustryComponentForPosition(position);
		if (industryComponent == null)
		{
			contract = default(Contract);
			return false;
		}
		return industryComponent.Industry.TryGetActiveContract(out contract);
	}
}
