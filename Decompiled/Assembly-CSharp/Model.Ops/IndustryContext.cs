using System;
using System.Collections.Generic;
using System.Linq;
using Game;
using Game.State;
using Helpers;
using JetBrains.Annotations;
using KeyValue.Runtime;
using Model.Database;
using Model.Definition;
using Model.Definition.Data;
using Model.Ops.Definition;
using Network;
using Serilog;
using Track;
using UnityEngine;

namespace Model.Ops;

public readonly struct IndustryContext : IIndustryContext
{
	public enum CarSizePreference
	{
		Small,
		Medium,
		Large,
		ExtraLarge
	}

	private readonly struct OrderedCar
	{
		public readonly CarDescriptor Descriptor;

		public readonly string CarId;

		public OrderedCar(CarDescriptor descriptor, string carId)
		{
			Descriptor = descriptor;
			CarId = carId;
		}
	}

	private readonly TrainController _trainController;

	private readonly OpsController _controller;

	private readonly IKeyValueObject _keyValueObject;

	private readonly Industry _industry;

	private readonly IndustryComponent _industryComponent;

	private readonly CarSizePreference _sizePreference;

	public GameDateTime Now { get; }

	public float DeltaTime { get; }

	public float PortionOfDayUntilNextRegularService
	{
		get
		{
			int interchangeServeHour = StateManager.Shared.Storage.InterchangeServeHour;
			float hours = Now.Hours;
			float num = ((float)interchangeServeHour - hours) / 24f;
			if (num < 0.001f)
			{
				num += 1f;
			}
			return num;
		}
	}

	private IndustryStorageHelper Storage => _industry.Storage;

	private string ComponentStoragePrefix
	{
		get
		{
			if (!_industryComponent.sharedStorage)
			{
				return _industryComponent.subIdentifier;
			}
			return null;
		}
	}

	public IndustryContext(TrainController trainController, OpsController opsController, Industry industry, IndustryComponent industryComponent, IKeyValueObject keyValueObject, CarSizePreference sizePreference, float dt, GameDateTime now)
	{
		_trainController = trainController;
		_controller = opsController;
		_keyValueObject = keyValueObject;
		_industry = industry;
		_industryComponent = industryComponent;
		_sizePreference = sizePreference;
		DeltaTime = dt;
		Now = now;
	}

	public IEnumerable<IOpsCar> CarsAtPosition()
	{
		foreach (Car item in _controller.CarsAtPosition(_industryComponent))
		{
			float num = 0.05f;
			if (!(Mathf.Abs(item.velocity) > num))
			{
				OpsCarAdapter opsCarAdapter = new OpsCarAdapter(item, _controller);
				yield return opsCarAdapter;
			}
		}
	}

	public void OrderAwayEmpty(IOpsCar car, string orderTag, bool noPayment)
	{
		_controller.AddOrderForOutboundEmptyCar(car, _industryComponent, orderTag, noPayment);
	}

	public void OrderAwayLoaded(IOpsCar car, string orderTag, bool noPayment)
	{
		_controller.AddOrderForOutboundLoadedCar(car, _industryComponent, orderTag, noPayment);
	}

	public bool OrderLoad(CarTypeFilter carTypeFilter, Load load, string orderTag, bool noPayment, out float quantity)
	{
		return _controller.AddOrderForInboundCar(carTypeFilter, load, _industryComponent, _industry, orderTag, noPayment, out quantity);
	}

	public void OrderEmpty(CarTypeFilter carTypeFilter, string orderTag, bool noPayment)
	{
		_controller.AddOrderForInboundCar(carTypeFilter, null, _industryComponent, _industry, orderTag, noPayment, out var _);
	}

	public float QuantityOnOrder(Load load)
	{
		return _controller.QuantityOnOrder(_industryComponent, load);
	}

	public int NumberOfCarsOnOrder(Load load)
	{
		return _controller.CountOrdersMatching(_industryComponent, load, null);
	}

	public int NumberOfCarsOnOrderEmpties(CarTypeFilter carTypeFilter)
	{
		return _controller.CountOrdersMatching(_industryComponent, null, carTypeFilter);
	}

	public float AvailableCapacityInCars(CarTypeFilter carTypeFilter, Load load)
	{
		return _controller.AvailableCapacityInCars(_industryComponent, load, carTypeFilter);
	}

	public int NumberOfCarsOnOrderForTag(string tag)
	{
		return _controller.CountOrdersMatching(_industryComponent, tag);
	}

	public int NumberOfCarsOnOrderEntireIndustry()
	{
		return _controller.CountOrdersForIndustry(_industry);
	}

	public void AddToStorage(Load load, float quantity, float maxQuantity)
	{
		Storage.AddToStorage(load, quantity, maxQuantity, ComponentStoragePrefix);
	}

	public void RemoveFromStorage(Load load, float quantity)
	{
		Storage.RemoveFromStorage(load, quantity, ComponentStoragePrefix);
	}

	public float QuantityInStorage(Load load)
	{
		return Storage.QuantityInStorage(load, ComponentStoragePrefix);
	}

	public void AddOrderedCars(List<IOrder> orders, int maxToOrder)
	{
		int num = Mathf.Min(maxToOrder, orders.Select((IOrder o) => o.CarCount).Sum());
		TrackSpan[] trackSpans = _industryComponent.trackSpans;
		List<OrderedCar> list = new List<OrderedCar>();
		System.Random rnd = new System.Random();
		IPrefabStore prefabStore = _trainController.PrefabStore;
		int num2 = 0;
		while (num > 0)
		{
			list.Clear();
			int num3 = 0;
			int num4 = 0;
			while (list.Count < num && num3 < orders.Count)
			{
				IOrder order = orders[num3];
				if (order.CarCount == 0)
				{
					num3++;
					continue;
				}
				try
				{
					if (!(order is Order order2))
					{
						if (order is ReturnFromBardoOrder returnFromBardoOrder)
						{
							CarDescriptor descriptor = _trainController.CarForId(returnFromBardoOrder.CarId).Descriptor();
							list.Add(new OrderedCar(descriptor, returnFromBardoOrder.CarId));
						}
					}
					else
					{
						CarDescriptor descriptor2 = CreateCarDescriptorForOrder(order2, prefabStore, _sizePreference, rnd);
						list.Add(new OrderedCar(descriptor2, null));
					}
				}
				catch (Exception exception)
				{
					Log.Error(exception, "Exception while creating car descriptor for order: {order}", order);
					Debug.LogException(exception);
					num3++;
					continue;
				}
				num4++;
				if (num4 == order.CarCount)
				{
					num3++;
					num4 = 0;
				}
			}
			if (list.Count == 0)
			{
				break;
			}
			ShuffleIfNeeded(list, rnd);
			try
			{
				if (!_trainController.PlaceTrain(trackSpans.ToList(), list.Select((OrderedCar d) => d.Descriptor).ToList(), list.Select((OrderedCar d) => d.CarId).ToList()))
				{
					Log.Information("Couldn't fit {numDescriptors} on {trackSpans}, dropping count from {maximum}", list.Count, trackSpans, num);
					num = Mathf.FloorToInt((float)num * 0.75f);
					continue;
				}
				int count = list.Count;
				num3 = 0;
				for (int num5 = 0; num5 < count; num5++)
				{
					IOrder order3 = orders[num3];
					if (order3.CarCount == 0)
					{
						num5--;
						num3++;
						continue;
					}
					order3.CarCount--;
					orders[num3] = order3;
					if (order3.CarCount <= 0)
					{
						num3++;
					}
				}
				num2++;
			}
			catch (Exception exception2)
			{
				Log.Error(exception2, "Exception while placing cars: {trackSpans}, {orderedCars}", trackSpans, list.Count);
				num = Mathf.FloorToInt((float)num * 0.75f);
			}
		}
	}

	private static void ShuffleIfNeeded<T>(List<T> descriptors, System.Random rnd)
	{
		int interchangeShuffle = OpsController.InterchangeShuffle;
		for (int i = 0; i < interchangeShuffle; i++)
		{
			descriptors.OverhandShuffle(rnd, 1, 8);
		}
	}

	private CarDescriptor CreateCarDescriptorForOrder(Order order, IPrefabStore prefabStore, CarSizePreference sizePreference, System.Random rnd)
	{
		CarTypeFilter carTypeFilter = order.CarTypeFilter;
		TypedContainerItem<CarDefinition> typedContainerItem = prefabStore.Random(carTypeFilter, sizePreference, rnd);
		if (typedContainerItem == null)
		{
			throw new ArgumentException($"No car for filter: {carTypeFilter}", "carTypeFilter");
		}
		List<LoadSlot> loadSlots = typedContainerItem.Definition.LoadSlots;
		if (loadSlots.Count == 0)
		{
			throw new Exception("Car prototype " + typedContainerItem.Identifier + " has no slots");
		}
		int tons = OrderWeightInTons(typedContainerItem, order.Load);
		int paymentOnArrival = ((!order.NoPayment) ? _controller.PaymentForMove(_industryComponent, order.Destination, tons) : 0);
		int graceDays = _controller.CalculateGraceDays(_industryComponent, order.Destination);
		Waybill waybill = new Waybill(Now, _industryComponent, order.Destination, paymentOnArrival, completed: false, order.Tag, graceDays);
		CarLoadInfo? info = ((order.Load == null) ? ((CarLoadInfo?)null) : new CarLoadInfo?(new CarLoadInfo(order.Load.id, loadSlots[0].MaximumCapacity)));
		(string, Value) tuple = CarExtensions.KeyValueForLoadInfo(0, info);
		string item = tuple.Item1;
		Value item2 = tuple.Item2;
		Dictionary<string, Value> dictionary = new Dictionary<string, Value>
		{
			{ "ops.waybill", waybill.PropertyValue },
			{ item, item2 }
		};
		if (Car.OilFeature)
		{
			float num = Config.Shared.initialOiledDistribution.Evaluate((float)rnd.NextDouble());
			dictionary["oiled"] = num;
		}
		string reportingMark = ReportingMarkForNewCar(typedContainerItem);
		return new CarDescriptor(typedContainerItem, new CarIdent(reportingMark, null), null, null, flipped: false, dictionary);
	}

	private string ReportingMarkForNewCar(TypedContainerItem<CarDefinition> typedContainerItem)
	{
		string playerRoad = StateManager.Shared.RailroadMark;
		return OpsController.ForeignRoads.Where((string mark) => mark != playerRoad).ToArray().RandomElement();
	}

	private static int OrderWeightInTons(TypedContainerItem<CarDefinition> definitionInfo, [CanBeNull] Load load)
	{
		if (load == null)
		{
			return 0;
		}
		List<LoadSlot> loadSlots = definitionInfo.Definition.LoadSlots;
		if (loadSlots.Count == 0)
		{
			Log.Error("Car prototype has no slot definitions: {carPrototype}", definitionInfo.Identifier);
			return 0;
		}
		LoadSlot loadSlot = loadSlots[0];
		if (!loadSlot.LoadRequirementsMatch(load))
		{
			Log.Error("Car prototype slot definition 0 does not match load: {carPrototype}, {load}", definitionInfo.Identifier, load);
			return 0;
		}
		return Mathf.CeilToInt(load.Pounds(loadSlot.MaximumCapacity) / 2000f);
	}

	public void RemoveCar(IOpsCar car)
	{
		_trainController.RemoveCar(car.Id);
	}

	public void MoveToBardo(IOpsCar car)
	{
		string identifier = _industryComponent.Identifier;
		_trainController.MoveToBardo(car.Id, identifier);
	}

	public void PayWaybill(IOpsCar car, Waybill waybill)
	{
		GameDateTime now = Now;
		int paymentOnArrival = waybill.PaymentOnArrival;
		int num = waybill.ConditionFineForCarCondition(car.Condition);
		int num2 = 0;
		if (_industry.HasActiveContract(now))
		{
			int days = Mathf.FloorToInt(now.DaysSince(waybill.Created));
			num2 = _industry.Contract.Value.TimelyDeliveryBonus(days, paymentOnArrival);
		}
		int num3 = paymentOnArrival + num2 - num;
		if (num3 != 0)
		{
			_industry.ApplyToBalance(num3, Ledger.Category.Freight, null, 1, quiet: true);
			_controller.AddCoalescedPaymentAnnouncement(car, _industry, paymentOnArrival, new(int, string)[2]
			{
				(num2, "timely"),
				(-num, "damage")
			}, num3);
		}
	}

	public void PayLoad(Load load, float units)
	{
		int num = Mathf.RoundToInt(load.payPerQuantity * units);
		if (num != 0)
		{
			_industry.ApplyToBalance(num, Ledger.Category.Freight, null, Mathf.RoundToInt(units / load.NominalQuantityPerCarLoad), quiet: true);
			string text = $"Payment from {_industryComponent.DisplayName} for delivery of {load.QuantityString(units)}: {num:C0}";
			Log.Debug(text);
			Multiplayer.Broadcast(text);
		}
	}

	public void RequestIndustriesOrderCars()
	{
		_controller.RequestIndustriesOrderCars();
	}

	public GameDateTime GetDateTime(string key, GameDateTime defaultValue)
	{
		Value value = _keyValueObject[key];
		if (value.IsNull)
		{
			return defaultValue;
		}
		return new GameDateTime(value.FloatValue);
	}

	public void SetDateTime(string key, GameDateTime dateTime)
	{
		_keyValueObject[key] = Value.Float((float)dateTime.TotalSeconds);
	}

	public float CounterIncrement(string key, float value)
	{
		float num = _keyValueObject[key].FloatValue + value;
		_keyValueObject[key] = Value.Float(num);
		return num;
	}

	public float CounterClear(string key)
	{
		float floatValue = _keyValueObject[key].FloatValue;
		_keyValueObject[key] = Value.Null();
		return floatValue;
	}
}
