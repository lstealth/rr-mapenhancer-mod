using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Game.Messages;
using JetBrains.Annotations;
using Network.Client;
using RollingStock;
using Serilog;
using Track;
using UnityEngine;

namespace Model.Physics;

public class IntegrationSet
{
	protected class Element
	{
		public readonly Car car;

		public float position;

		public float oldPosition;

		public float acceleration;

		public readonly float SlackA;

		public readonly float SlackB;

		public readonly float CarRadius;

		public float SlackStretch;

		public bool SlackStretchDidChangeDirection;

		public float PositionAtLastLocationUpdate;

		public float Velocity => position - oldPosition;

		public Element(Car car)
		{
			this.car = car;
			SlackA = car.CouplerSlack(car.LogicalToEnd(Car.LogicalEnd.A));
			SlackB = car.CouplerSlack(car.LogicalToEnd(Car.LogicalEnd.B));
			CarRadius = car.carLength / 2f;
		}
	}

	private enum BrakeIntegrationPhase
	{
		Velocity,
		Acceleration
	}

	public enum EnumerationCondition
	{
		AirConnected,
		Coupled,
		AirAndCoupled
	}

	public enum PositionInSet
	{
		A,
		Inside,
		B,
		Solo
	}

	public bool Dirty = true;

	public float LastSentTime;

	private readonly IIntegrationSetEventHandler EventHandler;

	public readonly uint Id;

	protected readonly List<Element> _elements;

	protected readonly Graph _graph;

	private float? _lowerBound;

	private float? _upperBound;

	private bool _lastTickPositioned;

	private bool _hasUpdatedBoundsOnce;

	private int _ticksSinceRebuild;

	private const float minCouplerSeparation = 1f;

	public IEnumerable<Car> Cars => _elements.Select((Element e) => e.car);

	private bool ActiveCars { get; }

	public bool IsEmpty => _elements.Count == 0;

	public int NumberOfCars => _elements.Count;

	public virtual bool ShouldSkipTick => AllCarsAtRest();

	public static IntegrationSet Create(uint id, IReadOnlyCollection<Car> cars, Graph graph, bool activeCars, IIntegrationSetEventHandler eventHandler)
	{
		if (activeCars)
		{
			return new IntegrationSet(id, cars, graph, activeCars: true, eventHandler);
		}
		return new RemoteIntegrationSet(id, cars, graph, activeCars: false, eventHandler);
	}

	protected IntegrationSet(uint id, IReadOnlyCollection<Car> cars, Graph graph, bool activeCars, IIntegrationSetEventHandler eventHandler)
	{
		Id = id;
		_elements = cars.Select((Car c) => new Element(c)).ToList();
		_graph = graph;
		ActiveCars = activeCars;
		EventHandler = eventHandler;
		foreach (Element element in _elements)
		{
			element.car.set = this;
		}
		InvalidateCachedCarIndexes();
		SortElements();
		RebuildPositions();
		LogSet("Init");
	}

	private IntegrationSet(uint id, List<Element> elements, Graph graph, bool activeCars, IIntegrationSetEventHandler eventHandler)
	{
		Id = id;
		_elements = elements;
		_graph = graph;
		ActiveCars = activeCars;
		EventHandler = eventHandler;
		foreach (Element element in _elements)
		{
			element.car.set = this;
		}
		InvalidateCachedCarIndexes();
		SortElements();
		RebuildPositions();
		LogSet("Init");
	}

	public bool AllCarsAtRest()
	{
		foreach (Element element in _elements)
		{
			if (!element.car.IsAtRest || element.car.IsOnTurntable)
			{
				return false;
			}
		}
		return true;
	}

	public virtual void Tick(float dt)
	{
		if (_lastTickPositioned || !_hasUpdatedBoundsOnce)
		{
			UpdateBounds();
		}
		UpdateAcceleration();
		float dt2 = dt / 2f;
		ApplyBrakes(dt2, dt, BrakeIntegrationPhase.Acceleration);
		ApplyVerlet(dt);
		for (int i = 0; i < 4; i++)
		{
			IntegrateConstraints(dt);
		}
		ApplyBrakes(dt2, dt, BrakeIntegrationPhase.Velocity);
		RecenterPositionsIfNeeded();
		PositionCars(dt, isInitialPosition: false);
	}

	private void PositionCars(float dt, bool isInitialPosition)
	{
		bool flag = false;
		foreach (Element element in _elements)
		{
			float num = element.position - element.oldPosition;
			num *= 0f - element.car.Orientation;
			element.car.velocity = ((dt == 0f) ? 0f : (num / dt));
			bool flag2 = ShouldPosition(element);
			if (isInitialPosition || flag2)
			{
				flag = true;
				try
				{
					Location wheelBoundsF = _graph.LocationByMoving(element.car.WheelBoundsF, num);
					MovementInfo info = new MovementInfo(dt, Mathf.Abs(num), element.car.NormalizedTractiveEffort);
					element.car.PositionWheelBoundsFront(wheelBoundsF, _graph, info, update: true);
				}
				catch (Exception exception)
				{
					Log.Error(exception, "Exception while positioning car {car}", element.car);
				}
				finally
				{
				}
				if (isInitialPosition)
				{
					continue;
				}
				Dirty = true;
				if (element.SlackStretchDidChangeDirection && Mathf.InverseLerp(0.001f, 0.006f, Mathf.Abs(element.SlackStretch)) > 0.1f)
				{
					bool isIn = element.SlackStretch > 0f;
					Car car = _elements[_elements.IndexOf(element) + 1].car;
					float deltaVelocity = Mathf.Abs(VelocityA(element.car) - VelocityA(car));
					EventHandler.IntegrationSetCarsDidCollide(element.car, car, deltaVelocity, isIn);
				}
			}
			element.SlackStretchDidChangeDirection = false;
		}
		_lastTickPositioned = flag;
		if (flag)
		{
			_ticksSinceRebuild++;
			if (_ticksSinceRebuild > 100)
			{
				RebuildPositions();
			}
		}
	}

	private static float VelocityA(Car car)
	{
		return car.velocity * car.Orientation;
	}

	private static bool ShouldPosition(Element entry)
	{
		if (Mathf.Abs(entry.position - entry.PositionAtLastLocationUpdate) > 0.001f || entry.car.ShouldUpdatePosition())
		{
			entry.PositionAtLastLocationUpdate = entry.position;
			return true;
		}
		return false;
	}

	private void UpdateBounds()
	{
		if (_elements.Count == 0)
		{
			return;
		}
		Element element = _elements[0];
		Location wheelBoundsA = element.car.WheelBoundsA;
		Vector3 position = _graph.GetPosition(element.car.WheelBoundsA);
		List<Element> elements = _elements;
		Element element2 = elements[elements.Count - 1];
		Location wheelBoundsB = element2.car.WheelBoundsB;
		Vector3 position2 = _graph.GetPosition(element2.car.WheelBoundsB);
		_lowerBound = null;
		_upperBound = null;
		if (CheckForEnemyCar(position + (position - element.car.GetCenterPosition(_graph)).normalized * (5f + element.car.WheelInsetA), wheelBoundsA, element.car) != null)
		{
			_lowerBound = element.position - element.CarRadius;
		}
		Vector3 direction = position2 - element2.car.GetCenterPosition(_graph);
		if (CheckForEnemyCar(position2 + direction.normalized * (5f + element.car.WheelInsetB), wheelBoundsB, element2.car) != null)
		{
			_upperBound = element2.position + element2.CarRadius;
		}
		if (!_lowerBound.HasValue)
		{
			TrackSegment.End end = BoundingEnd(wheelBoundsA.segment, position, _graph.GetPosition(element.car.WheelBoundsB));
			TrackNode trackNode = wheelBoundsA.segment.NodeForEnd(end);
			if (_graph.NodeIsDeadEnd(trackNode, out direction))
			{
				float num = ((trackNode.turntable != null) ? 0.01f : (0.5f + element.car.WheelInsetA));
				_lowerBound = element.position + num - (wheelBoundsA.DistanceTo(end) + element.CarRadius);
			}
		}
		if (!_upperBound.HasValue)
		{
			TrackSegment.End end2 = BoundingEnd(wheelBoundsB.segment, position2, _graph.GetPosition(element2.car.WheelBoundsA));
			TrackNode trackNode2 = wheelBoundsB.segment.NodeForEnd(end2);
			if (_graph.NodeIsDeadEnd(trackNode2, out direction))
			{
				float num2 = ((trackNode2.turntable != null) ? 0.01f : (0.5f + element2.car.WheelInsetB));
				_upperBound = element2.position - num2 + (wheelBoundsB.DistanceTo(end2) + element2.CarRadius);
			}
		}
		_hasUpdatedBoundsOnce = true;
	}

	private static TrackSegment.End BoundingEnd(TrackSegment segment, Vector3 locPosition, Vector3 otherPosition)
	{
		Vector3 localPosition = segment.a.transform.localPosition;
		Vector3 localPosition2 = segment.b.transform.localPosition;
		bool flag = (locPosition - localPosition).sqrMagnitude < (otherPosition - localPosition).sqrMagnitude;
		bool flag2 = (locPosition - localPosition2).sqrMagnitude < (otherPosition - localPosition2).sqrMagnitude;
		if (flag && flag2)
		{
			if (!((locPosition - localPosition).sqrMagnitude < (locPosition - localPosition2).sqrMagnitude))
			{
				return TrackSegment.End.B;
			}
			return TrackSegment.End.A;
		}
		if (!flag && !flag2)
		{
			if (!((otherPosition - localPosition).sqrMagnitude < (otherPosition - localPosition2).sqrMagnitude))
			{
				return TrackSegment.End.A;
			}
			return TrackSegment.End.B;
		}
		if (!flag)
		{
			return TrackSegment.End.B;
		}
		return TrackSegment.End.A;
	}

	[CanBeNull]
	private Car CheckForEnemyCar(Vector3 position, Location loc, Car originatingCar)
	{
		Car car = EventHandler.IntegrationSetCheckForCar(position);
		if (car == null)
		{
			return null;
		}
		if (car == originatingCar)
		{
			Log.Error("IntegrationSetCheckForCar({position}) found same car {car} ({integrationSet})", position, car, this);
			return null;
		}
		if (car.set == this)
		{
			Log.Error("IntegrationSetCheckForCar({position}) from {origin} returned {car} which is in this set ({integrationSet})", position, originatingCar, car, this);
			return null;
		}
		if (car.WheelBoundsF.segment == loc.segment || car.WheelBoundsR.segment == loc.segment)
		{
			return null;
		}
		float limit = 10f + Mathf.Max(car.wheelInsetF, car.wheelInsetR, originatingCar.wheelInsetF, originatingCar.wheelInsetR);
		if (_graph.CheckSameRoute(loc, car.WheelBoundsF, limit))
		{
			return null;
		}
		if (_graph.CheckSameRoute(loc, car.WheelBoundsR, limit))
		{
			return null;
		}
		return car;
	}

	private void LogSet(string tag = "")
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < _elements.Count; i++)
		{
			Element element = _elements[i];
			float num = element.car.velocity * element.car.Orientation * 2.23694f;
			stringBuilder.Append($"({element.car.id} {element.car.DisplayName} @ {element.position:F1} {num:F1})");
			if (i + 1 < _elements.Count)
			{
				Element element2 = _elements[i + 1];
				float num2 = Mathf.Abs(element.position + element.CarRadius - (element2.position - element2.CarRadius));
				if (element.car[Car.LogicalEnd.B].IsCoupled)
				{
					stringBuilder.Append($"-<{(num2 - 1f) * 100f:F1}cm>-");
				}
				else
				{
					stringBuilder.Append($"- {num2:F1} -");
				}
			}
		}
		Log.Debug("IntegrationSet {id} {tag} {elements}", Id, tag, stringBuilder.ToString());
	}

	private void LogLocations()
	{
		Log.Debug("IntegrationSet {id}: {locations}", Id, _elements.Select((Element e) => e.car.LocationF));
	}

	private void UpdateAcceleration()
	{
		foreach (Element element in _elements)
		{
			Car car = element.car;
			float orientation = car.Orientation;
			if (ActiveCars)
			{
				float num = car.Weight * 0.453592f;
				float num2 = orientation * car.TractiveForce * 4.44822f;
				float num3 = orientation * car.GravityForce * 4.44822f;
				float num4 = num2 + num3;
				element.acceleration = (0f - num4) / num;
			}
			else
			{
				element.acceleration = orientation * (0f - car.compensatingAcceleration);
			}
		}
	}

	private void ApplyVerlet(float dt)
	{
		foreach (Element element in _elements)
		{
			float position = element.position;
			element.position += element.position - element.oldPosition + element.acceleration * dt * dt;
			element.oldPosition = position;
		}
	}

	private void ApplyBrakes(float dt, float dtForVelocity, BrakeIntegrationPhase phase)
	{
		if (!ActiveCars || dt == 0f)
		{
			return;
		}
		foreach (Element element in _elements)
		{
			float num = element.position - element.oldPosition;
			if (num == 0f && element.acceleration == 0f)
			{
				continue;
			}
			float absVelocity = Mathf.Abs(num / dtForVelocity);
			float num2 = element.car.Weight * 0.453592f;
			float f = num + element.acceleration * dt * dt;
			float num3 = ((phase == BrakeIntegrationPhase.Acceleration) ? Mathf.Sign(f) : Mathf.Sign(num));
			float num4 = CalculateRetardingForce(element, absVelocity);
			float num5 = (0f - num3) * num4 / num2;
			switch (phase)
			{
			case BrakeIntegrationPhase.Acceleration:
				if (Math.Abs(Mathf.Sign(num + (element.acceleration + num5) * dt * dt) - Mathf.Sign(f)) > 0.0001f)
				{
					element.position = element.oldPosition;
					element.acceleration = 0f;
				}
				else
				{
					element.acceleration += num5;
				}
				break;
			case BrakeIntegrationPhase.Velocity:
			{
				float num6 = num5 * dt * dt;
				if ((num6 + num) / num < 0f)
				{
					element.position = element.oldPosition;
				}
				else
				{
					element.position += num6;
				}
				break;
			}
			}
		}
	}

	private float CalculateRetardingForce(Element entry, float absVelocity)
	{
		Car car = entry.car;
		float num = absVelocity * 2.23694f;
		float num2 = car.CalculateBrakingForce(car.air.brakePercent, absVelocity);
		float num3 = absVelocity * 2.23694f;
		float num4 = car.Weight / 2000f;
		float num5 = (1.3f + 29f / num4 + 0.045f * num3 + 0.063f * num3 * num3 / num4) * num4 * 4.44822f;
		float num6 = Mathf.Exp((num - car.maxSpeedMph) / 10f + 7.1f);
		float num7 = car.CalculateCurvatureRetardingForce(absVelocity);
		float num8 = car.CalculateDerailedRetardingForce();
		return num2 + num5 + num6 + num7 + num8;
	}

	private void IntegrateConstraints(float wholeDeltaTime)
	{
		for (int i = 0; i < _elements.Count - 1; i++)
		{
			Element element = _elements[i];
			Element element2 = _elements[i + 1];
			float num = 1f + element.SlackB + element2.SlackA;
			bool flag = AreCoupled(element, element2);
			float num2 = element2.position - element2.CarRadius - (element.position + element.CarRadius);
			float num3 = 0f;
			if (num2 < 1f)
			{
				num3 = 1f - num2;
				if (!AreCoupled(element, element2))
				{
					float num4 = Mathf.Abs(element.Velocity - element2.Velocity) / wholeDeltaTime;
					if (num4 > 0.22351964f)
					{
						Couple(element, element2, num4);
					}
				}
				if (element.SlackStretch < 0f)
				{
					element.SlackStretch = 0f;
					element.SlackStretchDidChangeDirection = true;
				}
				element.SlackStretch += num3;
			}
			else if (flag && num2 > num)
			{
				num3 = num - num2;
				if (element.SlackStretch > 0f)
				{
					element.SlackStretch = 0f;
					element.SlackStretchDidChangeDirection = true;
				}
				element.SlackStretch += num3;
			}
			else if (!flag && AreAirHosesConnected(element, element2) && num2 > 1.5f)
			{
				BreakAirHoses(element, element2);
			}
			float num5 = element.car.Weight / (element.car.Weight + element2.car.Weight);
			element.position -= (1f - num5) * num3;
			element2.position += num5 * num3;
		}
		if (_elements.Count == 0)
		{
			return;
		}
		if (_lowerBound.HasValue)
		{
			Element element3 = _elements[0];
			float num6 = element3.position - element3.CarRadius;
			if (num6 < _lowerBound.Value)
			{
				element3.position += 2f * (_lowerBound.Value - num6);
				EventHandler.IntegrationSetCarsDidCollide(element3.car, null, (_lowerBound.Value - num6) / wholeDeltaTime, isIn: true);
			}
		}
		if (_upperBound.HasValue)
		{
			Element element4 = _elements.Last();
			float num7 = element4.position + element4.CarRadius;
			if (_upperBound.Value < num7)
			{
				element4.position += 2f * (_upperBound.Value - num7);
				EventHandler.IntegrationSetCarsDidCollide(element4.car, null, (_upperBound.Value - num7) / wholeDeltaTime, isIn: true);
			}
		}
	}

	private void RebuildPositions()
	{
		float num = 0f;
		for (int i = 0; i < _elements.Count; i++)
		{
			Element element = _elements[i];
			num = (element.position = num + element.CarRadius);
			float num2 = element.car.Orientation * element.car.velocity;
			element.oldPosition = element.position + num2 * Time.fixedDeltaTime;
			num += element.CarRadius;
			if (i + 1 < _elements.Count)
			{
				Car car = _elements[i + 1].car;
				if (!_graph.TryGetDistanceBetweenSameRoute(element.car.WheelBoundsB, car.WheelBoundsA, out var distance))
				{
					Log.Warning("IS.RebuildPositions: {element} and {nextCar} are not on the same route - couldn't find actual distance", element.car, car);
				}
				float num3 = distance - (element.car.WheelInsetB + car.WheelInsetA);
				if (_graph.GetDistanceBetweenClose(element.car.LocationA, car.LocationA) < num3)
				{
					LogSet("Overlap");
					num3 *= -1f;
				}
				num += num3;
			}
		}
		foreach (Element element2 in _elements)
		{
			element2.car.SetOffsetWithinSet(element2.position);
		}
		Dirty = true;
		_ticksSinceRebuild = 0;
	}

	private void SortElements()
	{
		if (!ActiveCars || _elements.Count == 0)
		{
			return;
		}
		LinkedList<Car> linkedList = new LinkedList<Car>();
		List<Car> list = new List<Car>(_elements.Select((Element e) => e.car));
		Car value = list.Last();
		list.RemoveAt(list.Count - 1);
		linkedList.AddFirst(value);
		Dictionary<string, Vector3> dictionary = new Dictionary<string, Vector3>(_elements.Count);
		foreach (Car item in list)
		{
			dictionary[item.id] = item.GetCenterPosition(_graph);
		}
		while (list.Any())
		{
			Car value2 = linkedList.First.Value;
			Car value3 = linkedList.Last.Value;
			Vector3 centerPosition = value2.GetCenterPosition(_graph);
			Vector3 centerPosition2 = value3.GetCenterPosition(_graph);
			Car car = list.First();
			float num = Mathf.Min((centerPosition - dictionary[car.id]).magnitude, (centerPosition2 - dictionary[car.id]).magnitude);
			foreach (Car item2 in list)
			{
				Vector3 vector = dictionary[item2.id];
				float magnitude = (centerPosition - vector).magnitude;
				float magnitude2 = (centerPosition2 - vector).magnitude;
				float num2 = Mathf.Min(magnitude, magnitude2);
				if (num2 < num)
				{
					car = item2;
					num = num2;
				}
			}
			list.Remove(car);
			Vector3 vector2 = dictionary[car.id];
			Vector3 direction = _graph.GetPositionDirection(car.LocationA).Direction;
			Vector3 direction2 = _graph.GetPositionDirection(value2.LocationA).Direction;
			Vector3 direction3 = _graph.GetPositionDirection(value3.LocationA).Direction;
			if (Vector3.Dot(vector2 - centerPosition, direction2) >= 0f)
			{
				if (Vector3.Dot(direction, direction2) < 0f)
				{
					car.Reverse();
				}
				linkedList.AddFirst(car);
			}
			else
			{
				if (Vector3.Dot(direction, direction3) < 0f)
				{
					car.Reverse();
				}
				linkedList.AddLast(car);
			}
		}
		_elements.Clear();
		_elements.AddRange(linkedList.Select((Car car4) => new Element(car4)));
		InvalidateCachedCarIndexes();
		Car car2 = _elements[0].car;
		List<Element> elements = _elements;
		Car car3 = elements[elements.Count - 1].car;
		if (car2.EndGearA.IsAirConnected || car2.EndGearA.IsCoupled)
		{
			EventHandler.IntegrationSetRequestsBreakConnections(car2, Car.LogicalEnd.A);
		}
		if (car3.EndGearB.IsAirConnected || car3.EndGearB.IsCoupled)
		{
			EventHandler.IntegrationSetRequestsBreakConnections(car3, Car.LogicalEnd.B);
		}
	}

	private void RecenterPositionsIfNeeded()
	{
		float num = float.MaxValue;
		float num2 = float.MinValue;
		foreach (Element element in _elements)
		{
			num = Mathf.Min(num, element.position);
			num2 = Mathf.Max(num2, element.position);
		}
		float num3 = num2 - num;
		float num4 = ((num3 < 2000f) ? 1000f : num3);
		if (Mathf.Abs(num) < num4 && Mathf.Abs(num2) < num4)
		{
			return;
		}
		float num5 = 0f - Mathf.Lerp(num, num2, 0.5f);
		foreach (Element element2 in _elements)
		{
			element2.position += num5;
			element2.oldPosition += num5;
			element2.PositionAtLastLocationUpdate += num5;
		}
	}

	private bool AreCoupled(Element entry0, Element entry1)
	{
		if (entry0.car.EndGearB.IsCoupled)
		{
			return entry1.car.EndGearA.IsCoupled;
		}
		return false;
	}

	private bool AreAirHosesConnected(Element entry0, Element entry1)
	{
		if (entry0.car.EndGearB.IsAirConnected)
		{
			return entry1.car.EndGearA.IsAirConnected;
		}
		return false;
	}

	private void Couple(Element entry0, Element entry1, float deltaVelocity)
	{
		EventHandler.IntegrationSetDidCouple(entry0.car, entry1.car, deltaVelocity);
		EventHandler.IntegrationSetCarsDidCollide(entry0.car, entry1.car, deltaVelocity, isIn: true);
		LogSet("DidCouple");
	}

	private void BreakAirHoses(Element entry0, Element entry1)
	{
		EventHandler.IntegrationSetDidBreakAirHoses(entry0.car, entry1.car);
		Log.Information("Air hoses separated: {Entry0}, {Entry1}", entry0.car, entry1.car);
	}

	public void AddCar(Car car)
	{
		car.set = this;
		_elements.Add(new Element(car));
		InvalidateCachedCarIndexes();
		SortElements();
		RebuildPositions();
		UpdateBounds();
		LogSet("AddCar");
	}

	public void Union(IntegrationSet other)
	{
		foreach (Element element in other._elements)
		{
			element.car.set = this;
			_elements.Add(new Element(element.car));
		}
		InvalidateCachedCarIndexes();
		SortElements();
		RebuildPositions();
		UpdateBounds();
		LogSet("Union");
	}

	public void Split(Car car1, Car car2, out IntegrationSet newSet)
	{
		int num = ValidIndexOfCar(car1);
		int num2 = ValidIndexOfCar(car2);
		if (Math.Abs(num - num2) > 1)
		{
			throw new ArgumentException($"Cars are not adjacent in set: {car1} at {num}, {car2} at {num2}");
		}
		if (num < num2)
		{
			Split(car1, Car.LogicalEnd.B, car2, Car.LogicalEnd.A, out newSet);
		}
		else
		{
			Split(car2, Car.LogicalEnd.B, car1, Car.LogicalEnd.A, out newSet);
		}
	}

	private void Split(Car car1, Car.LogicalEnd side1, Car car2, Car.LogicalEnd side2, out IntegrationSet newSet)
	{
		Car car3 = ((side1 == Car.LogicalEnd.A) ? car1 : car2);
		EventHandler.IntegrationSetRequestsBreakConnections(car1, side1);
		EventHandler.IntegrationSetRequestsBreakConnections(car2, side2);
		int num = ValidIndexOfCar(car3);
		List<Element> range = _elements.GetRange(num, _elements.Count - num);
		_elements.RemoveRange(num, _elements.Count - num);
		InvalidateCachedCarIndexes();
		newSet = new IntegrationSet(EventHandler.GenerateIntegrationSetId(), range, _graph, ActiveCars, EventHandler);
		LogSet("Split1");
		newSet.LogSet("Split2");
	}

	public IEnumerable<Car> EnumerateCoupledTo(Car car, Car.LogicalEnd fromEnd = Car.LogicalEnd.A)
	{
		return EnumerateWhileConnected(car, fromEnd, (Car.EndGear endGear) => endGear.IsCoupled);
	}

	public IEnumerable<Car> EnumerateAirOpenTo(Car car, Car.LogicalEnd fromEnd = Car.LogicalEnd.A)
	{
		return EnumerateWhileConnected(car, fromEnd, (Car.EndGear endGear) => endGear.IsAirConnectedAndOpen);
	}

	private IEnumerable<Car> EnumerateWhileConnected(Car car, Car.LogicalEnd fromEnd, Func<Car.EndGear, bool> predicate)
	{
		int num = ValidIndexOfCar(car);
		if (fromEnd == Car.LogicalEnd.A)
		{
			int num2;
			for (num2 = num; num2 >= 0; num2--)
			{
				Element element = _elements[num2];
				if (!predicate(element.car.EndGearA))
				{
					break;
				}
			}
			if (num2 < 0)
			{
				Log.Error("Set doesn't end with un-connected car: {Car}", car);
				yield break;
			}
			for (int i = num2; i < _elements.Count; i++)
			{
				Element element2 = _elements[i];
				yield return element2.car;
				if (!predicate(element2.car.EndGearB))
				{
					break;
				}
			}
			yield break;
		}
		int j;
		for (j = num; j < _elements.Count; j++)
		{
			Element element3 = _elements[j];
			if (!predicate(element3.car.EndGearB))
			{
				break;
			}
		}
		if (j >= _elements.Count)
		{
			Log.Error("Set doesn't end with un-connected car: {Car}", car);
			yield break;
		}
		for (int i = j; i >= 0; i--)
		{
			Element element2 = _elements[i];
			yield return element2.car;
			if (!predicate(element2.car.EndGearA))
			{
				break;
			}
		}
	}

	public int StartIndexForConnected(Car car, Car.LogicalEnd fromEnd, EnumerationCondition condition)
	{
		int num = ValidIndexOfCar(car);
		if (fromEnd == Car.LogicalEnd.A)
		{
			int num2;
			for (num2 = num; num2 >= 0; num2--)
			{
				Element element = _elements[num2];
				if (!Predicate(condition, element.car.EndGearA))
				{
					break;
				}
			}
			if (num2 < 0)
			{
				Log.Error("Set doesn't end with unconnected car: {car}", car);
				return 0;
			}
			return num2;
		}
		int i;
		for (i = num; i < _elements.Count; i++)
		{
			Element element2 = _elements[i];
			if (!Predicate(condition, element2.car.EndGearB))
			{
				break;
			}
		}
		if (i >= _elements.Count)
		{
			Log.Error("Set doesn't end with unconnected car: {car}", car);
			return _elements.Count - 1;
		}
		return i;
	}

	[CanBeNull]
	public Car NextCarConnected(ref int carIndex, Car.LogicalEnd fromEnd, EnumerationCondition condition, out bool stop)
	{
		if (carIndex < 0 || carIndex >= _elements.Count)
		{
			stop = true;
			return null;
		}
		if (fromEnd == Car.LogicalEnd.A)
		{
			Element element = _elements[carIndex];
			stop = !Predicate(condition, element.car.EndGearB);
			carIndex++;
			return element.car;
		}
		Element element2 = _elements[carIndex];
		stop = !Predicate(condition, element2.car.EndGearA);
		carIndex--;
		return element2.car;
	}

	private static bool Predicate(EnumerationCondition condition, Car.EndGear endGear)
	{
		return condition switch
		{
			EnumerationCondition.AirConnected => endGear.IsAirConnected, 
			EnumerationCondition.Coupled => endGear.IsCoupled, 
			EnumerationCondition.AirAndCoupled => endGear.IsAirConnected && endGear.IsCoupled, 
			_ => throw new ArgumentOutOfRangeException("condition", condition, null), 
		};
	}

	public void ValidateConsistency()
	{
		if (!ActiveCars)
		{
			return;
		}
		for (int i = 0; i < _elements.Count - 1; i++)
		{
			Element element = _elements[i];
			Element element2 = _elements[i + 1];
			Car car = element.car;
			Car car2 = element2.car;
			if (car.EndGearB.IsCoupled != car2.EndGearA.IsCoupled)
			{
				Log.Error("Inconsistent IsCoupled: {CurrentCar} {CurrentIsCoupled} vs {NextCar} {NextIsCoupled}", car, car.EndGearB.IsCoupled, car2, car2.EndGearA.IsCoupled);
				FixInconsistentConnectionsByBreakingConnections(element, element2);
			}
			if (car.EndGearB.IsAirConnected != car2.EndGearA.IsAirConnected)
			{
				Log.Error("Inconsistent IsAirConnected: {CurrentCar} {CurrentIsAirConnected} vs {NextCar} {NextIsAirConnected}", car, car.EndGearB.IsAirConnected, car2, car2.EndGearA.IsAirConnected);
				FixInconsistentConnectionsByBreakingConnections(element, element2);
			}
			if (car.ForceConnectedToAtRear(car2) && car.FrontIsA && (!car.EndGearB.IsAirConnected || !car.EndGearB.IsAirConnected))
			{
				EventHandler.IntegrationSetRequestsReconnect(car, car2);
			}
			else if (car2.ForceConnectedToAtRear(car) && !car2.FrontIsA && (!car2.EndGearA.IsAirConnected || !car2.EndGearA.IsCoupled))
			{
				EventHandler.IntegrationSetRequestsReconnect(car2, car);
			}
		}
		if (_elements.Count > 0)
		{
			Car car3 = _elements[0].car;
			List<Element> elements = _elements;
			Car car4 = elements[elements.Count - 1].car;
			Car.EndGear endGearA = car3.EndGearA;
			if (endGearA.IsCoupled || endGearA.IsAirConnected)
			{
				Log.Error("Inconsistent first: {Car} {IsCoupled} {IsAirConnected}", car3, endGearA.IsCoupled, endGearA.IsAirConnected);
				EventHandler.IntegrationSetRequestsBreakConnections(car3, Car.LogicalEnd.A);
			}
			Car.EndGear endGearB = car4.EndGearB;
			if (endGearB.IsCoupled || endGearB.IsAirConnected)
			{
				Log.Error("Inconsistent last: {Car} {IsCoupled} {IsAirConnected}", car4, endGearB.IsCoupled, endGearB.IsAirConnected);
				EventHandler.IntegrationSetRequestsBreakConnections(car4, Car.LogicalEnd.B);
			}
		}
	}

	private void FixInconsistentConnectionsByBreakingConnections(Element carA, Element carB)
	{
		EventHandler.IntegrationSetRequestsBreakConnections(carA.car, Car.LogicalEnd.B);
		EventHandler.IntegrationSetRequestsBreakConnections(carB.car, Car.LogicalEnd.A);
	}

	public int? IndexOfCar(Car car)
	{
		if (car.CachedSetIndex.HasValue)
		{
			return car.CachedSetIndex.Value;
		}
		int? result = null;
		int count = _elements.Count;
		for (int i = 0; i < count; i++)
		{
			Car car2 = _elements[i].car;
			if ((object)car == car2)
			{
				result = i;
				car.CachedSetIndex = i;
				break;
			}
		}
		return result;
	}

	private int ValidIndexOfCar(Car car)
	{
		int? num = IndexOfCar(car);
		if (!num.HasValue)
		{
			throw new ArgumentOutOfRangeException($"Car {car} not in set");
		}
		return num.Value;
	}

	public void RemoveCar(Car car, out IntegrationSet newSet)
	{
		int num = ValidIndexOfCar(car);
		car.set = null;
		if (num == 0 || num == _elements.Count - 1)
		{
			EventHandler.IntegrationSetRequestsBreakConnections(car, (num == 0) ? Car.LogicalEnd.B : Car.LogicalEnd.A);
			_elements.RemoveAt(num);
			InvalidateCachedCarIndexes();
			newSet = null;
		}
		else
		{
			Split(car, Car.LogicalEnd.B, _elements[num + 1].car, Car.LogicalEnd.A, out newSet);
			EventHandler.IntegrationSetRequestsBreakConnections(car, Car.LogicalEnd.A);
			_elements.RemoveAt(_elements.Count - 1);
			InvalidateCachedCarIndexes();
		}
	}

	public void RemoveCarInternal(Car car)
	{
		int index = ValidIndexOfCar(car);
		car.set = null;
		_elements.RemoveAt(index);
		InvalidateCachedCarIndexes();
	}

	public Car GetAirConnection(Car car, Car.LogicalEnd end)
	{
		int num = ValidIndexOfCar(car);
		switch (end)
		{
		case Car.LogicalEnd.A:
			if (num == 0 || !car.EndGearA.IsAirConnected)
			{
				return null;
			}
			return _elements[num - 1].car;
		case Car.LogicalEnd.B:
			if (num == _elements.Count - 1 || !car.EndGearB.IsAirConnected)
			{
				return null;
			}
			return _elements[num + 1].car;
		default:
			throw new ArgumentOutOfRangeException("end", end, null);
		}
	}

	public Car GetCouplerConnection(Car car, Car.LogicalEnd end)
	{
		int num = ValidIndexOfCar(car);
		switch (end)
		{
		case Car.LogicalEnd.A:
			if (num == 0 || !car.EndGearA.IsCoupled)
			{
				return null;
			}
			return _elements[num - 1].car;
		case Car.LogicalEnd.B:
			if (num == _elements.Count - 1 || !car.EndGearB.IsCoupled)
			{
				return null;
			}
			return _elements[num + 1].car;
		default:
			throw new ArgumentOutOfRangeException("end", end, null);
		}
	}

	public bool TryGetCoupledCar(Car car, Car.End end, out Car coupled)
	{
		coupled = GetCouplerConnection(car, car.EndToLogical(end));
		return coupled != null;
	}

	public bool TryGetAdjacentCar(Car car, Car.LogicalEnd logicalEnd, out Car adjacent)
	{
		int num = ValidIndexOfCar(car);
		adjacent = logicalEnd switch
		{
			Car.LogicalEnd.A => (num == 0) ? null : _elements[num - 1].car, 
			Car.LogicalEnd.B => (num == _elements.Count - 1) ? null : _elements[num + 1].car, 
			_ => null, 
		};
		return adjacent != null;
	}

	public PositionInSet PositionOfCar(Car car)
	{
		int num = ValidIndexOfCar(car);
		if (_elements.Count == 1)
		{
			return PositionInSet.Solo;
		}
		if (num == 0)
		{
			return PositionInSet.A;
		}
		if (num == _elements.Count - 1)
		{
			return PositionInSet.B;
		}
		return PositionInSet.Inside;
	}

	public void AddVelocityToCar(Car car, float velocity, float maxVelocity)
	{
		float fixedDeltaTime = Time.fixedDeltaTime;
		float num = velocity * fixedDeltaTime;
		int index = ValidIndexOfCar(car);
		Element element = _elements[index];
		float f = (element.position - element.oldPosition) / fixedDeltaTime;
		float num2 = element.position + (car.FrontIsA ? num : (0f - num));
		float f2 = (num2 - element.oldPosition) / fixedDeltaTime;
		float num3 = Mathf.Abs(f);
		float num4 = Mathf.Abs(f2);
		if ((!(num3 < maxVelocity) || !(num4 > maxVelocity)) && (!(num3 > maxVelocity) || !(num4 > num3)))
		{
			_elements[index].position = num2;
		}
	}

	public void OrderAB(ref Car a, ref Car b)
	{
		int num = ValidIndexOfCar(a);
		int num2 = ValidIndexOfCar(b);
		if (num > num2)
		{
			Car car = b;
			Car car2 = a;
			a = car;
			b = car2;
		}
	}

	public Snapshot.CarSet Snapshot()
	{
		return new Snapshot.CarSet
		{
			Id = Id,
			CarIds = Cars.Select((Car car) => car.id).ToList(),
			Positions = _elements.Select((Element el) => el.position).ToList(),
			FrontIsAs = _elements.Select((Element el) => el.car.FrontIsA).ToList()
		};
	}

	public void SetPositions(List<float> positions, List<bool> frontIsAs, bool immediate)
	{
		for (int i = 0; i < positions.Count; i++)
		{
			float num = positions[i];
			_elements[i].position = num;
			_elements[i].oldPosition = num;
			_elements[i].car.FrontIsA = frontIsAs[i];
		}
		if (immediate)
		{
			PositionCars(0f, isInitialPosition: true);
		}
	}

	private BatchCarPositionUpdate CreateBatchCarPositionUpdate(long tick, bool critical)
	{
		Location wheelBoundsA = _elements[0].car.WheelBoundsA;
		Snapshot.TrackLocation startLocation = Graph.CreateSnapshotTrackLocation(wheelBoundsA);
		float[] array = new float[_elements.Count];
		array[0] = 0f;
		Location a = wheelBoundsA;
		for (int i = 1; i < _elements.Count; i++)
		{
			Location wheelBoundsA2 = _elements[i].car.WheelBoundsA;
			array[i] = _graph.GetDistanceBetweenClose(a, wheelBoundsA2);
			a = wheelBoundsA2;
		}
		ushort[] array2 = new ushort[_elements.Count];
		for (int j = 0; j < _elements.Count; j++)
		{
			Element element = _elements[j];
			array2[j] = Mathf.FloatToHalf(element.car.velocity * element.car.Orientation);
		}
		return new BatchCarPositionUpdate(Id, tick, startLocation, array, array2, critical);
	}

	public virtual void HandleCarPositionUpdate(Location wheelBoundsA, float[] positions, float[] velocities, long updateTick)
	{
		throw new NotImplementedException();
	}

	public void SendBatchCarPositionUpdate(ClientManager client, bool critical)
	{
		BatchCarPositionUpdate batchCarPositionUpdate = CreateBatchCarPositionUpdate(client.Tick, critical);
		client.Send(batchCarPositionUpdate);
		Dirty = false;
	}

	public void SetVelocity(float velocity, IReadOnlyList<Car> cars)
	{
		foreach (Element item in _elements.Where((Element element) => cars.Contains(element.car)))
		{
			item.oldPosition = item.position + velocity * Time.fixedDeltaTime;
		}
	}

	public bool ContainsBrokenConstraints()
	{
		for (int i = 0; i < _elements.Count - 1; i++)
		{
			Element element = _elements[i];
			Element element2 = _elements[i + 1];
			float num = 1f + element.SlackB + element2.SlackA;
			bool flag = AreCoupled(element, element2);
			float num2 = element2.position - element2.CarRadius - (element.position + element.CarRadius);
			if (num2 < 0.8f)
			{
				Log.Warning("Elements overlap at {i} {j}: {car0} {car1}, distance {dist}", i, i + 1, element.car, element2.car, num2);
				return true;
			}
			if (flag && num2 > num * 1.2f)
			{
				Log.Warning("Elements too far at {i} {j}: {car0} {car1}, distance {dist}", i, i + 1, element.car, element2.car, num2);
				return true;
			}
		}
		return false;
	}

	protected void InvalidateCachedCarIndexes()
	{
		foreach (Element element in _elements)
		{
			element.car.CachedSetIndex = null;
		}
	}
}
