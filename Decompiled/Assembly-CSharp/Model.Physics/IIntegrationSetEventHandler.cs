using JetBrains.Annotations;
using UnityEngine;

namespace Model.Physics;

public interface IIntegrationSetEventHandler
{
	uint GenerateIntegrationSetId();

	void IntegrationSetDidCouple(Car car0, Car car1, float deltaVelocity);

	void IntegrationSetCarsDidCollide(Car car0, Car car1, float deltaVelocity, bool isIn);

	void IntegrationSetDidBreakAirHoses(Car car0, Car car1);

	void IntegrationSetRequestsBreakConnections(Car car, Car.LogicalEnd logicalEnd);

	[CanBeNull]
	Car IntegrationSetCheckForCar(Vector3 point);

	void IntegrationSetRequestsReconnect(Car engine, Car tender);
}
