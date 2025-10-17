using System.Collections.Generic;
using Game;
using Model.Ops.Definition;

namespace Model.Ops;

public interface IIndustryContext
{
	float DeltaTime { get; }

	GameDateTime Now { get; }

	float PortionOfDayUntilNextRegularService { get; }

	IEnumerable<IOpsCar> CarsAtPosition();

	void OrderAwayEmpty(IOpsCar car, string orderTag = null, bool noPayment = false);

	void OrderAwayLoaded(IOpsCar car, string orderTag = null, bool noPayment = false);

	float QuantityOnOrder(Load load);

	int NumberOfCarsOnOrder(Load load);

	int NumberOfCarsOnOrderForTag(string tag);

	int NumberOfCarsOnOrderEntireIndustry();

	int NumberOfCarsOnOrderEmpties(CarTypeFilter carTypes);

	float AvailableCapacityInCars(CarTypeFilter carTypeFilter, Load load);

	bool OrderLoad(CarTypeFilter carTypeFilter, Load load, string orderTag, bool noPayment, out float quantity);

	void OrderEmpty(CarTypeFilter carTypeFilter, string orderTag, bool noPayment = false);

	void AddToStorage(Load load, float quantity, float maxQuantity);

	void RemoveFromStorage(Load load, float quantity);

	float QuantityInStorage(Load load);

	void AddOrderedCars(List<IOrder> orders, int maxToOrder);

	void RemoveCar(IOpsCar car);

	void MoveToBardo(IOpsCar car);

	void PayWaybill(IOpsCar car, Waybill waybill);

	void PayLoad(Load load, float quantity);

	void RequestIndustriesOrderCars();

	GameDateTime GetDateTime(string key, GameDateTime defaultValue);

	void SetDateTime(string key, GameDateTime dateTime);

	float CounterIncrement(string key, float value);

	float CounterClear(string key);
}
