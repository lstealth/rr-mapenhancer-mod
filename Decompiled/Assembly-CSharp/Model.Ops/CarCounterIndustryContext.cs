using System.Collections.Generic;
using System.Linq;
using Game;
using Model.Ops.Definition;
using UnityEngine;

namespace Model.Ops;

public class CarCounterIndustryContext : IIndustryContext
{
	public HashSet<MockCar> Cars = new HashSet<MockCar>();

	public readonly Dictionary<string, int> LoadOrders = new Dictionary<string, int>();

	public readonly Dictionary<string, CarTypeFilter> LoadOrderCarTypeFilters = new Dictionary<string, CarTypeFilter>();

	public Dictionary<string, int> LoadPending = new Dictionary<string, int>();

	public readonly Dictionary<string, int> EmptyOrders = new Dictionary<string, int>();

	public Dictionary<string, int> EmptyPending = new Dictionary<string, int>();

	public readonly HashSet<MockCar> OrderedAway = new HashSet<MockCar>();

	private readonly Dictionary<string, float> _quantities;

	private readonly Dictionary<string, GameDateTime> _dateTimes = new Dictionary<string, GameDateTime>();

	public GameDateTime Now => TimeWeather.Now;

	public float DeltaTime { get; }

	public float PortionOfDayUntilNextRegularService => 1f;

	public CarCounterIndustryContext(float deltaTime, Dictionary<string, float> quantities)
	{
		DeltaTime = deltaTime;
		_quantities = quantities;
	}

	public IEnumerable<IOpsCar> CarsAtPosition()
	{
		return Cars;
	}

	public void OrderAwayEmpty(IOpsCar car, string orderTag, bool noPayment)
	{
		OrderedAway.Add((MockCar)car);
	}

	public void OrderAwayLoaded(IOpsCar car, string orderTag, bool noPayment)
	{
		OrderedAway.Add((MockCar)car);
	}

	public float QuantityOnOrder(Load load)
	{
		return (float)NumberOfCarsOnOrder(load) * load.NominalQuantityPerCarLoad;
	}

	public int NumberOfCarsOnOrder(Load load)
	{
		int value;
		int num = (LoadOrders.TryGetValue(load.id, out value) ? value : 0);
		int value2;
		int num2 = (LoadPending.TryGetValue(load.id, out value2) ? value2 : 0);
		int num3 = Cars.Count((MockCar car) => car.QuantityOfLoad(load).quantity > 0f);
		return num + num2 + num3;
	}

	public int NumberOfCarsOnOrderForTag(string tag)
	{
		return 0;
	}

	public int NumberOfCarsOnOrderEntireIndustry()
	{
		return 0;
	}

	public int NumberOfCarsOnOrderEmpties(CarTypeFilter carTypes)
	{
		int value;
		int num = (EmptyOrders.TryGetValue(carTypes.queryString, out value) ? value : 0);
		int value2;
		int num2 = (EmptyPending.TryGetValue(carTypes.queryString, out value2) ? value2 : 0);
		int count = Cars.Count;
		return num + num2 + count;
	}

	public float AvailableCapacityInCars(CarTypeFilter carTypes, Load load)
	{
		int value;
		float num = (EmptyOrders.TryGetValue(carTypes.queryString, out value) ? ((float)value * load.NominalQuantityPerCarLoad) : 0f);
		int value2;
		float num2 = (EmptyPending.TryGetValue(carTypes.queryString, out value2) ? ((float)value2 * load.NominalQuantityPerCarLoad) : 0f);
		float num3 = Cars.Where((MockCar car) => carTypes.Matches(car.CarType)).Sum(delegate(MockCar car)
		{
			(float quantity, float capacity) tuple = car.QuantityOfLoad(load);
			var (num4, _) = tuple;
			return tuple.capacity - num4;
		});
		return num + num2 + num3;
	}

	public bool OrderLoad(CarTypeFilter carTypeFilter, Load load, string orderTag, bool noPayment, out float quantity)
	{
		if (!LoadOrders.TryGetValue(load.id, out var value))
		{
			value = 0;
		}
		value++;
		quantity = load.NominalQuantityPerCarLoad;
		LoadOrders[load.id] = value;
		LoadOrderCarTypeFilters[load.id] = carTypeFilter;
		return true;
	}

	public void OrderEmpty(CarTypeFilter carTypeFilter, string orderTag, bool noPayment)
	{
		string queryString = carTypeFilter.queryString;
		if (!EmptyOrders.TryGetValue(queryString, out var value))
		{
			value = 0;
		}
		value++;
		EmptyOrders[queryString] = value;
	}

	public void AddToStorage(Load load, float quantity, float maxQuantity)
	{
		float value = ((!_quantities.TryGetValue(load.id, out value)) ? quantity : Mathf.Min(maxQuantity, value + quantity));
		_quantities[load.id] = value;
	}

	public void RemoveFromStorage(Load load, float quantity)
	{
		float value = ((!_quantities.TryGetValue(load.id, out value)) ? 0f : Mathf.Max(0f, value - quantity));
		_quantities[load.id] = value;
	}

	public float QuantityInStorage(Load load)
	{
		if (_quantities.TryGetValue(load.id, out var value))
		{
			return value;
		}
		return 0f;
	}

	public void AddOrderedCars(List<IOrder> orders, int maxToOrder)
	{
	}

	public void RemoveCar(IOpsCar car)
	{
	}

	public void MoveToBardo(IOpsCar car)
	{
	}

	public void PayWaybill(IOpsCar car, Waybill waybill)
	{
	}

	public void PayLoad(Load load, float units)
	{
	}

	public void RequestIndustriesOrderCars()
	{
	}

	public GameDateTime GetDateTime(string key, GameDateTime defaultValue)
	{
		if (!_dateTimes.TryGetValue(key, out var value))
		{
			return value;
		}
		return defaultValue;
	}

	public void SetDateTime(string key, GameDateTime dateTime)
	{
		_dateTimes[key] = dateTime;
	}

	public float CounterIncrement(string key, float value)
	{
		return 0f;
	}

	public float CounterClear(string key)
	{
		return 0f;
	}
}
