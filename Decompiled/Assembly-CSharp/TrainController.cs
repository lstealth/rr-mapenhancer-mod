using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Audio;
using Core;
using GalaSoft.MvvmLight.Messaging;
using Game;
using Game.Events;
using Game.Messages;
using Game.State;
using Helpers;
using JetBrains.Annotations;
using KeyValue.Runtime;
using Model;
using Model.AI;
using Model.Database;
using Model.Definition;
using Model.Definition.Data;
using Model.Ops;
using Model.Physics;
using Network;
using Network.Client;
using RollingStock;
using Serilog;
using Track;
using Track.Signals;
using UI.EngineControls;
using UI.LostCarPlacer;
using UI.Map;
using UnityEngine;
using UnityEngine.Pool;

public class TrainController : MonoBehaviour, IIntegrationSetEventHandler
{
	private struct CarOnSpan
	{
		public readonly Car Car;

		public readonly float DistanceOnSegment;

		public CarOnSpan(Car car, float distanceOnSegment)
		{
			Car = car;
			DistanceOnSegment = distanceOnSegment;
		}
	}

	public struct LostCar
	{
		public string Id;

		public CarDescriptor Descriptor;

		public readonly Vector3? Position;

		public LostCar(string id, CarDescriptor descriptor, Vector3? position)
		{
			Id = id;
			Descriptor = descriptor;
			Position = position;
		}
	}

	private readonly Queue<List<CarOnSpan>> _carsOnSpanLists = new Queue<List<CarOnSpan>>();

	[NonSerialized]
	public Graph graph;

	[NonSerialized]
	private CarCuller _carCuller;

	private string _selectedCarId;

	private string _lastSelectedCarId;

	private readonly IntegrationSetManager _integrationSets = new IntegrationSetManager();

	public CarPrototypeLibrary carPrototypeLibrary;

	public Config config;

	public Coupler couplerPrefab;

	public CutLever cutLeverPrefab;

	public Anglecock anglecockPrefab;

	public OilPointPickable oilPointPrefab;

	public AudioClip airFlowAudioClip;

	public RollingProfile rollingProfile;

	public WheelClackProfile wheelClackProfile;

	public AutoEngineerConfig autoEngineerConfig;

	[Header("Map Markers")]
	public MapIcon locomotiveMapIconPrefab;

	[SerializeField]
	private bool debugDrawBrakeDisplay;

	[SerializeField]
	private BuiltinPrefabReferences builtinPrefabReferences;

	private readonly Dictionary<string, RoadNumberAllocator> _roadNumberAllocators = new Dictionary<string, RoadNumberAllocator>();

	private readonly Dictionary<string, Car> _carLookup = new Dictionary<string, Car>();

	private readonly HashSet<Car> _cars = new HashSet<Car>();

	private readonly SpatialHashHelper _spatialHash = new SpatialHashHelper(50, 32);

	private CarSegmentCache _carSegmentCache;

	private readonly HashSet<Car> _carsForUpdateSets = new HashSet<Car>();

	private readonly HashSet<string> _checkForCarKeys = new HashSet<string>();

	private readonly HashSet<Car> _checkForCarCars = new HashSet<Car>();

	private HashSet<string> _carIdsNearbyPlayer = new HashSet<string>();

	private IDisposable _selectedCarLoadToken;

	private PrefabStore _prefabStore;

	private Coroutine _nearbyPlayerCoroutine;

	private IndustryContext.CarSizePreference? _cachedSizePreference;

	public readonly List<List<LostCar>> LostCarCuts = new List<List<LostCar>>();

	private CoalescingAction _showLostCarCuts;

	private readonly Dictionary<string, float> _slackSoundRequests = new Dictionary<string, float>();

	private static WaitForFixedUpdate _waitForFixedUpdateCached;

	public static TrainController Shared { get; private set; }

	public BaseLocomotive SelectedLocomotive
	{
		get
		{
			if (SelectedCar is BaseLocomotive result)
			{
				return result;
			}
			return null;
		}
	}

	public Car SelectedCar
	{
		get
		{
			if (string.IsNullOrEmpty(_selectedCarId))
			{
				return null;
			}
			if (_carLookup.TryGetValue(_selectedCarId, out var value))
			{
				return value;
			}
			_selectedCarId = null;
			return null;
		}
		set
		{
			if (value != null && value.IsInBardo)
			{
				value = null;
			}
			bool flag = value == null;
			if (!(flag ? (_selectedCarId == null) : (value.id == _selectedCarId)))
			{
				_lastSelectedCarId = _selectedCarId;
				_selectedCarId = (flag ? null : value.id);
				_selectedCarLoadToken?.Dispose();
				_selectedCarLoadToken = (flag ? null : value.ModelLoadRetain("Selected"));
				Messenger.Default.Send(default(SelectedCarChanged));
				PlayerPropertiesManager.Shared.UpdateMyProperties(delegate(PlayerProperties props)
				{
					props.SelectedCarId = _selectedCarId;
					return props;
				});
			}
		}
	}

	public IEnumerable<Car> SelectedTrain
	{
		get
		{
			Car selectedCar = SelectedCar;
			if (!(selectedCar == null))
			{
				return selectedCar.EnumerateCoupled();
			}
			return Array.Empty<Car>();
		}
	}

	public IReadOnlyCollection<Car> Cars => _cars;

	public IPrefabInstantiator PrefabInstantiator => builtinPrefabReferences;

	private static bool IsHost => StateManager.IsHost;

	public IPrefabStore PrefabStore
	{
		get
		{
			if (_prefabStore == null)
			{
				_prefabStore = Model.Database.PrefabStore.Create();
			}
			return _prefabStore;
		}
	}

	public List<Car> LastPlacedTrain { get; set; }

	public IndustryContext.CarSizePreference CarSizePreference
	{
		get
		{
			if (_cachedSizePreference.HasValue)
			{
				return _cachedSizePreference.Value;
			}
			IndustryContext.CarSizePreference carSizePreference = InferSizePreference();
			_cachedSizePreference = carSizePreference;
			return carSizePreference;
		}
	}

	private static bool CheckProjectedSegmentsOverlap(Vector3 a0, Vector3 a1, Vector3 b0, Vector3 b1)
	{
		if (a0 == a1)
		{
			if (b0 == b1)
			{
				return a0 == b0;
			}
			return CheckProjectedSegmentsOverlap(b0, b1, a0, a1);
		}
		Vector3 rhs = a1 - a0;
		float a2 = Vector3.Dot(a0 - a0, rhs);
		float b2 = Vector3.Dot(a1 - a0, rhs);
		float a3 = Vector3.Dot(b0 - a0, rhs);
		float b3 = Vector3.Dot(b1 - a0, rhs);
		float num = Mathf.Min(a2, b2);
		float num2 = Mathf.Max(a2, b2);
		float num3 = Mathf.Min(a3, b3);
		float num4 = Mathf.Max(a3, b3);
		if (num <= num4)
		{
			return num3 <= num2;
		}
		return false;
	}

	internal IEnumerable<Car> CarsOnSpan(TrackSpan span)
	{
		List<Car> output = CollectionPool<List<Car>, Car>.Get();
		GetCarsOnSpan(span, output);
		foreach (Car item in output)
		{
			yield return item;
		}
		CollectionPool<List<Car>, Car>.Release(output);
	}

	internal void GetCarsOnSpan(TrackSpan span, List<Car> output, int limit = int.MaxValue)
	{
		output.Clear();
		if (!span.IsValid)
		{
			return;
		}
		Location location = span.lower.Value;
		Location location2 = span.upper.Value.Flipped();
		string text = null;
		IReadOnlyCollection<TrackSegment> segments = span.GetSegments();
		List<CarOnSpan> list = ((_carsOnSpanLists.Count > 0) ? _carsOnSpanLists.Dequeue() : new List<CarOnSpan>());
		while (true)
		{
			Location loc = location;
			Location loc2 = ((location2.segment == location.segment) ? location2 : new Location(location.segment, location.segment.GetLength(), location.end));
			Vector3 position = graph.GetPosition(loc);
			Vector3 position2 = graph.GetPosition(loc2);
			IReadOnlyList<string> readOnlyList = _carSegmentCache.EnumerateCarIdsOnSegment(location.segment.id);
			for (int i = 0; i < readOnlyList.Count; i++)
			{
				string text2 = readOnlyList[i];
				if (text2 == text)
				{
					continue;
				}
				Car car = CarForId(text2);
				if (!Overlaps(car, position, position2))
				{
					continue;
				}
				float num = DistanceOnSegment(car, location.segment, location.end);
				CarOnSpan item = new CarOnSpan(car, num);
				bool flag = false;
				for (int j = 0; j < list.Count; j++)
				{
					if (!(num > list[j].DistanceOnSegment))
					{
						list.Insert(j, item);
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					list.Add(item);
				}
			}
			foreach (CarOnSpan item2 in list)
			{
				output.Add(item2.Car);
				if (output.Count >= limit)
				{
					return;
				}
				text = item2.Car.id;
			}
			list.Clear();
			if (loc2.Equals(location2))
			{
				break;
			}
			TrackNode node = location.segment.NodeForEnd(location.EndIsA ? TrackSegment.End.B : TrackSegment.End.A);
			IReadOnlyList<TrackSegment> readOnlyList2 = graph.SegmentsConnectedTo(node);
			TrackSegment trackSegment = null;
			for (int k = 0; k < readOnlyList2.Count; k++)
			{
				TrackSegment trackSegment2 = readOnlyList2[k];
				if (trackSegment2 != location.segment && segments.Contains(trackSegment2))
				{
					trackSegment = trackSegment2;
					break;
				}
			}
			if (trackSegment == null)
			{
				break;
			}
			location = new Location(trackSegment, 0f, trackSegment.EndForNode(node));
		}
		if (_carsOnSpanLists.Count < 4)
		{
			list.Clear();
			_carsOnSpanLists.Enqueue(list);
			if (_carsOnSpanLists.Count > 1)
			{
				Log.Verbose("_carsOnSpanLists count {count}", _carsOnSpanLists.Count);
			}
		}
		static float DistanceOnSegment(Car car2, TrackSegment segment, TrackSegment.End end)
		{
			if (car2.LocationF.segment == segment)
			{
				return car2.LocationF.WithEnd(end).distance;
			}
			return car2.LocationR.WithEnd(end).distance;
		}
		bool Overlaps(Car car2, Vector3 boundsA, Vector3 boundsB)
		{
			Vector3 position3 = graph.GetPosition(car2.LocationF);
			Vector3 position4 = graph.GetPosition(car2.LocationR);
			return CheckProjectedSegmentsOverlap(boundsA, boundsB, position3, position4);
		}
	}

	public bool AnyCarsOnSpan(TrackSpan span)
	{
		List<Car> list = CollectionPool<List<Car>, Car>.Get();
		GetCarsOnSpan(span, list, 1);
		bool result = list.Count > 0;
		CollectionPool<List<Car>, Car>.Release(list);
		return result;
	}

	private void Awake()
	{
		Shared = this;
		CarPrototypeLibrary.instance = carPrototypeLibrary;
		_carSegmentCache = new CarSegmentCache(this);
	}

	private void OnEnable()
	{
		Messenger.Default.Register<MapDidLoadEvent>(this, OnMapLoaded);
		Messenger.Default.Register<WorldDidMoveEvent>(this, WorldDidMove);
		Messenger.Default.Register<CarDefinitionDidChangeEvent>(this, CarDefinitionDidChange);
		Messenger.Default.Register<GraphDidRebuildCollections>(this, delegate
		{
			GraphDidRebuildCollections();
		});
		_integrationSets.CreateIntegrationSet = (uint id, IReadOnlyCollection<Car> cars) => CreateIntegrationSet(cars, id);
		_nearbyPlayerCoroutine = StartCoroutine(UpdateCarsNearbyPlayerCoroutine());
		_showLostCarCuts = new CoalescingAction(this, () => StateManager.Shared.HasRestoredProperties && !StateManager.IsUnloading, LostCarPlacerWindow.ShowIfNeeded);
	}

	private void OnDisable()
	{
		Messenger.Default.Unregister(this);
		StopCoroutine(_nearbyPlayerCoroutine);
		_nearbyPlayerCoroutine = null;
		_showLostCarCuts?.Dispose();
		_showLostCarCuts = null;
		_carSegmentCache.Clear();
	}

	private void OnDestroy()
	{
		if (Shared == this)
		{
			Shared = null;
		}
		_prefabStore?.Dispose();
	}

	private void FixedUpdate()
	{
		float deltaTime = Time.deltaTime;
		bool autoSyncTransforms = Physics.autoSyncTransforms;
		Physics.autoSyncTransforms = false;
		_spatialHash.UpdateIfNeeded();
		UpdateSets();
		foreach (Car car in Cars)
		{
			try
			{
				car.air.FixedUpdateAir(deltaTime);
			}
			catch (Exception exception)
			{
				Log.Error(exception, "Exception while running air for {car}", car);
				Debug.LogException(exception);
			}
		}
		foreach (IntegrationSet integrationSet in _integrationSets)
		{
			if (!integrationSet.ShouldSkipTick)
			{
				integrationSet.ValidateConsistency();
				integrationSet.Tick(deltaTime);
			}
		}
		_spatialHash.UpdateIfNeeded();
		UpdateSets();
		_integrationSets.RemoveEmpty();
		if (IsHost && Multiplayer.Client != null)
		{
			SendCarPositionsIfNeeded();
			SendAirIfNeeded();
		}
		Physics.autoSyncTransforms = autoSyncTransforms;
	}

	private void OnGUI()
	{
		if (debugDrawBrakeDisplay && SelectedCar != null)
		{
			CarAirSystem.GUIDrawDebugBrakeDisplay(SelectedCar);
		}
	}

	private void OnMapLoaded(MapDidLoadEvent mapDidLoadEvent)
	{
		CreateCarCullerIfNeeded();
	}

	private void CreateCarCullerIfNeeded()
	{
		if (!(_carCuller != null))
		{
			_carCuller = base.gameObject.AddComponent<CarCuller>();
			_carCuller.trainController = this;
		}
	}

	private void WorldDidMove(WorldDidMoveEvent evt)
	{
		Vector3 offset = evt.Offset;
		foreach (Car car in Cars)
		{
			car.WorldDidMove(offset);
		}
		if (_carCuller != null)
		{
			_carCuller.WorldDidMove(offset);
		}
	}

	private void CarDefinitionDidChange(CarDefinitionDidChangeEvent evt)
	{
		string carIdentifier = evt.CarIdentifier;
		List<Car> list = Cars.Where((Car car) => car.DefinitionInfo.Identifier == carIdentifier).ToList();
		Debug.Log($"CarDefinitionDidChange {carIdentifier} replacing {list.Count}");
		if (list.Any())
		{
			RebuildCars(list);
		}
	}

	private void RebuildCars(List<Car> cars)
	{
		IEnumerable<Snapshot.Car> first = cars.Select((Car car) => car.Snapshot());
		IEnumerable<IReadOnlyDictionary<string, Value>> second = cars.Select((Car car) => car.KeyValueObject.Dictionary);
		foreach (Car car2 in cars)
		{
			car2.PendingDestroy = true;
		}
		foreach (Car car3 in cars)
		{
			WillRemoveCar(car3);
			UnityEngine.Object.Destroy(car3.gameObject);
		}
		IEnumerable<Car> cars2 = first.Zip(second, Tuple.Create).Select(delegate(Tuple<Snapshot.Car, IReadOnlyDictionary<string, Value>> tuple)
		{
			tuple.Deconstruct(out var item, out var item2);
			Snapshot.Car snapshotCar = item;
			IReadOnlyDictionary<string, Value> properties = item2;
			Car car = AddCarInternal(snapshotCar, null);
			car.ResetKeyValueProperties(properties, SetValueOrigin.Local);
			return car;
		});
		UpdateSets(cars2);
	}

	private List<Car> HandleCreateCarsAsTrain(Location locA, IEnumerable<CarDescriptor> descriptors, [CanBeNull] IEnumerable<string> carIds, [CanBeNull] List<Snapshot.TrackLocation> frontLocations)
	{
		List<CarDescriptor> list = descriptors.ToList();
		if (!list.Any())
		{
			return new List<Car>();
		}
		_ = locA.IsValid;
		string[] array = ((carIds == null) ? new string[0] : carIds.ToArray());
		List<Car> list2 = new List<Car>(list.Count);
		Car car = null;
		for (int i = 0; i < list.Count; i++)
		{
			CarDescriptor descriptor = list[i];
			string text = ((array.Length == 0) ? null : array[i]);
			if (text == null && car != null && (object)car != null && car.Archetype == CarArchetype.LocomotiveSteam && descriptor.DefinitionInfo.Definition.Archetype == CarArchetype.Tender)
			{
				descriptor.Ident.ReportingMark = car.Ident.ReportingMark;
				descriptor.Ident.RoadNumber = car.Ident.RoadNumber + "T";
			}
			Car car2 = CreateCarIfNeeded(descriptor, text);
			if (car2.IsInBardo)
			{
				car2.Bardo = null;
				_carCuller.Add(car2);
			}
			car2.WillMove();
			if (frontLocations != null)
			{
				Location wheelBoundsF = graph.MakeLocation(frontLocations[i]);
				car2.PositionWheelBoundsFront(wheelBoundsF, graph, MovementInfo.Zero, update: true);
			}
			else
			{
				locA = car2.PositionA(locA, graph, MovementInfo.Zero, update: true);
				locA = graph.LocationByMoving(locA, -1f);
			}
			list2.Add(car2);
			car = car2;
		}
		Log.Information("HandleCreateCarsAsTrain {cars}", list2);
		CreateIntegrationSet(list2);
		foreach (Car item in list2)
		{
			_carCuller.PostAdd(item);
		}
		return list2;
	}

	public static void ConnectCars(IReadOnlyList<Car> cars, bool endAnglecocksOpen = false)
	{
		if (cars.Count > 0)
		{
			int num = (endAnglecocksOpen ? 1 : 0);
			cars[0].ApplyEndGearChange(Car.LogicalEnd.A, Car.EndGearStateKey.Anglecock, num);
			cars[cars.Count - 1].ApplyEndGearChange(Car.LogicalEnd.B, Car.EndGearStateKey.Anglecock, num);
		}
		for (int i = 0; i < cars.Count - 1; i++)
		{
			Car car = cars[i];
			Car car2 = cars[i + 1];
			car.ApplyEndGearChange(Car.LogicalEnd.B, Car.EndGearStateKey.IsCoupled, boolValue: true);
			car2.ApplyEndGearChange(Car.LogicalEnd.A, Car.EndGearStateKey.IsCoupled, boolValue: true);
			car.ApplyEndGearChange(Car.LogicalEnd.B, Car.EndGearStateKey.IsAirConnected, boolValue: true);
			car2.ApplyEndGearChange(Car.LogicalEnd.A, Car.EndGearStateKey.IsAirConnected, boolValue: true);
			car.ApplyEndGearChange(Car.LogicalEnd.B, Car.EndGearStateKey.Anglecock, 1f);
			car2.ApplyEndGearChange(Car.LogicalEnd.A, Car.EndGearStateKey.Anglecock, 1f);
		}
	}

	internal static void FillAir(IEnumerable<Car> cars)
	{
		foreach (Car car in cars)
		{
			if (car.air is LocomotiveAirSystem locomotiveAirSystem)
			{
				locomotiveAirSystem.MainReservoir.Pressure = 140f;
			}
			car.air.BrakeReservoir.Pressure = 90f;
			car.air.BrakeLine.Pressure = 90f;
			car.air.BrakeCylinder.Pressure = 0f;
		}
	}

	private void ApplyHandbrakesAsNeeded(List<Car> cars, PlaceTrainHandbrakes handbrakes)
	{
		int num = handbrakes switch
		{
			PlaceTrainHandbrakes.Automatic => CalculateNumHandbrakes(cars), 
			PlaceTrainHandbrakes.None => 0, 
			_ => throw new ArgumentOutOfRangeException("handbrakes", handbrakes, null), 
		};
		foreach (Car car in cars)
		{
			bool flag = num > 0;
			if (car.Archetype == CarArchetype.Tender)
			{
				flag = false;
			}
			if (flag)
			{
				if (car is BaseLocomotive baseLocomotive)
				{
					baseLocomotive.ControlHelper.LocomotiveBrake = 1f;
					num = 0;
				}
				else if (!car.air.handbrakeApplied)
				{
					car.SetHandbrake(apply: true);
				}
				num--;
			}
			else if (car.air.handbrakeApplied)
			{
				car.SetHandbrake(apply: false);
			}
		}
	}

	private static int CalculateNumHandbrakes(List<Car> cars, int minimumHandbrakes = 1, int maximumHandbrakes = 3)
	{
		float num = cars.Sum((Car car) => car.Weight) * 0.0005f;
		float num2 = cars.Sum((Car car) => Mathf.Abs(car.GravityForce)) * 0.0005f;
		int b = Mathf.CeilToInt(num2 / 5f);
		int count = cars.Count;
		int a = ((count >= 10) ? ((count < 20) ? 2 : 3) : ((count >= 5) ? 1 : 0));
		int num3 = Mathf.Clamp(Mathf.Max(a, b), minimumHandbrakes, maximumHandbrakes);
		Log.Verbose("CalculateNumHandbrakes: {carCount} cars, {tons} T, gravity {gravityForce} lb -> {numBrakes}", cars.Count, num, num2, num3);
		return num3;
	}

	public void PlaceTrain(Location loc, List<CarDescriptor> descriptors, [CanBeNull] List<string> carIds = null, float initialFuelWaterPercent = 1f, PlaceTrainHandbrakes handbrakes = PlaceTrainHandbrakes.Automatic)
	{
		loc.AssertValid();
		List<PlaceTrain.Car> cars = descriptors.Select((CarDescriptor desc) => ApplyInitialSlotContents(desc, initialFuelWaterPercent)).Select(delegate(CarDescriptor desc)
		{
			Dictionary<string, IPropertyValue> properties = PropertyValueConverter.RuntimeToSnapshot(desc.Properties);
			return new PlaceTrain.Car(desc.DefinitionInfo.Identifier, desc.Flipped, desc.Ident.ReportingMark, desc.Ident.RoadNumber, desc.TrainCrewId, properties);
		}).ToList();
		StateManager.ApplyLocal(new PlaceTrain(Graph.CreateSnapshotTrackLocation(loc), cars, carIds, handbrakes));
	}

	private static CarDescriptor ApplyInitialSlotContents(CarDescriptor desc, float initialSlotContentsPercent)
	{
		CarArchetype archetype = desc.DefinitionInfo.Definition.Archetype;
		if (archetype == CarArchetype.LocomotiveSteam || archetype == CarArchetype.Tender || archetype == CarArchetype.LocomotiveDiesel)
		{
			for (int i = 0; i < desc.DefinitionInfo.Definition.LoadSlots.Count; i++)
			{
				LoadSlot loadSlot = desc.DefinitionInfo.Definition.LoadSlots[i];
				if (!string.IsNullOrEmpty(loadSlot.RequiredLoadIdentifier))
				{
					var (key, value) = CarExtensions.KeyValueForLoadInfo(i, new CarLoadInfo(loadSlot.RequiredLoadIdentifier, loadSlot.MaximumCapacity * initialSlotContentsPercent));
					if (!desc.Properties.ContainsKey(key))
					{
						desc.Properties[key] = value;
					}
				}
			}
		}
		return desc;
	}

	private Car CreateCarIfNeeded(CarDescriptor descriptor, [CanBeNull] string carId)
	{
		if (carId != null && _carLookup.TryGetValue(carId, out var value))
		{
			Log.Debug("CreateCar using existing car for {carId}", carId);
			if (value.set != null)
			{
				value.SetAdjacentCarsNotConnected();
				_integrationSets.RemoveCar(value);
			}
			value.KeyValueObject.ApplyValues(descriptor.Properties);
			value.trainCrewId = descriptor.TrainCrewId;
			return value;
		}
		descriptor.Ident.ReportingMark = (string.IsNullOrEmpty(descriptor.Ident.ReportingMark) ? StateManager.Shared.RailroadMark : descriptor.Ident.ReportingMark);
		descriptor.Ident.RoadNumber = AllocateRoadNumber(descriptor);
		Car car = CreateCarRaw(descriptor, carId, ghost: false, base.transform);
		_carLookup[car.id] = car;
		_cars.Add(car);
		if (!car.IsInBardo)
		{
			_carCuller.Add(car);
		}
		return car;
	}

	public Car CreateCarRaw(CarDescriptor descriptor, [CanBeNull] string carId, bool ghost, Transform parent)
	{
		CarDefinition definition = descriptor.DefinitionInfo.Definition;
		Car.SetupPrefabs prefabs = new Car.SetupPrefabs
		{
			CouplerPrefab = couplerPrefab,
			CutLeverPrefab = cutLeverPrefab,
			AnglecockPrefab = anglecockPrefab,
			AirFlowAudioClip = airFlowAudioClip,
			RollingProfile = rollingProfile,
			LocomotiveMapIcon = locomotiveMapIconPrefab
		};
		AllocateCarIdIfNeeded(ref carId);
		GameObject gameObject = new GameObject("Car " + carId);
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		Car car = definition.Archetype switch
		{
			CarArchetype.LocomotiveSteam => gameObject.AddComponent<SteamLocomotive>(), 
			CarArchetype.LocomotiveDiesel => gameObject.AddComponent<DieselLocomotive>(), 
			_ => gameObject.AddComponent<Car>(), 
		};
		car.Setup(carId, descriptor, prefabs, ghost);
		SetValueOrigin origin = ((!IsHost) ? SetValueOrigin.Remote : SetValueOrigin.Local);
		car.ResetKeyValueProperties(descriptor.Properties, origin);
		car.OnPosition = delegate(Vector3 center, Quaternion rotation)
		{
			CarDidPosition(car, center, rotation);
		};
		return car;
	}

	private static void AllocateCarIdIfNeeded(ref string carId)
	{
		IdGenerator cars = IdGenerator.Cars;
		if (!string.IsNullOrEmpty(carId))
		{
			cars.Add(carId);
		}
		if (string.IsNullOrEmpty(carId))
		{
			carId = cars.Next();
		}
	}

	private void CarDidPosition(Car car, Vector3 center, Quaternion rotation)
	{
		if (!car.ghost && !car.IsInBardo)
		{
			_spatialHash.AddUpdateEntity(car.id, center, car.carLength / 2f);
			_carCuller.CarDidMove(car, center.GameToWorld());
			_carsForUpdateSets.Add(car);
			_carSegmentCache.UpdateCarPosition(car.id, car.LocationF.segment.id, car.LocationR.segment.id);
		}
	}

	internal IEnumerable<Car> CarsOnSegments(IEnumerable<TrackSegment> segments)
	{
		return segments.SelectMany((TrackSegment segment) => _carSegmentCache.EnumerateCarIdsOnSegment(segment.id)).Distinct().Select(CarForId);
	}

	private static bool RotatedRectContains(float rectLong, float rectShort, Quaternion rotation, Vector2 position, float radius)
	{
		Vector3 vector = rotation * new Vector3(position.x, 0f, position.y);
		Vector2 vector2 = new Vector2(vector.x, vector.z);
		Rect rect = new Rect((0f - rectLong) / 2f, (0f - rectShort) / 2f, rectLong, rectShort);
		Vector2 vector3 = new Vector2(radius, radius);
		return rect.Overlaps(new Rect(vector2 - vector3, vector3 * 2f));
	}

	[CanBeNull]
	internal Car CheckForCarAtLocation(Location location, float radius = 1f)
	{
		Vector3 position = graph.GetPosition(location);
		return CheckForCarAtPoint(position, radius);
	}

	[CanBeNull]
	public Car CheckForCarAtPoint(Vector3 point, float radius = 1f)
	{
		CheckForCarsAtPoint(point, radius, _checkForCarCars);
		return _checkForCarCars.SomeElement();
	}

	public void CheckForCarsAtPoint(Vector3 point, float radius, HashSet<Car> foundCars, Location? sameRouteRequirement = null)
	{
		foundCars.Clear();
		_checkForCarKeys.Clear();
		_spatialHash.Query(point, radius, _checkForCarKeys);
		foreach (string checkForCarKey in _checkForCarKeys)
		{
			if (!_carLookup.TryGetValue(checkForCarKey, out var value))
			{
				Log.Error("CheckForCarAtPoint: AABB tree returned {carId} but car is not in _carLookup", checkForCarKey);
				continue;
			}
			Vector3 centerPosition = value.GetCenterPosition(graph);
			float num = radius + value.carLength / 2f + 10f;
			if (Vector3.SqrMagnitude(centerPosition - point) > num * num)
			{
				continue;
			}
			Quaternion centerRotationCheckForCar = value.GetCenterRotationCheckForCar(graph);
			Vector3 vector = point - centerPosition;
			if (!RotatedRectContains(position: new Vector2(vector.x, vector.z), rectLong: value.carLength, rectShort: 2f, rotation: centerRotationCheckForCar, radius: radius))
			{
				continue;
			}
			if (sameRouteRequirement.HasValue)
			{
				Location valueOrDefault = sameRouteRequirement.GetValueOrDefault();
				if (!graph.CheckSameRoute(value.LocationF, valueOrDefault, radius + value.carLength))
				{
					continue;
				}
			}
			foundCars.Add(value);
		}
	}

	public IEnumerable<string> CarIdsInRadius(Vector3 point, float radius)
	{
		_checkForCarKeys.Clear();
		_spatialHash.Query(point, radius, _checkForCarKeys);
		return _checkForCarKeys;
	}

	public bool CanPlaceAt(Location location, float distance)
	{
		try
		{
			Location location2 = location;
			float num = distance - Mathf.Floor(distance / 1f) * 1f;
			for (float num2 = 0f; num2 < distance; num2 += 1f)
			{
				if (!CanPlaceAt(location2))
				{
					return false;
				}
				location2 = graph.LocationByMoving(location2, -1f, checkSwitchAgainstMovement: true);
			}
			location2 = graph.LocationByMoving(location2, 0f - num, checkSwitchAgainstMovement: true);
			if (!CanPlaceAt(location2))
			{
				return false;
			}
			return true;
		}
		catch (EndOfTrack)
		{
			return false;
		}
		catch (SwitchAgainstMovement)
		{
			return false;
		}
	}

	private bool CanPlaceAt(Location location)
	{
		if (!location.IsValid)
		{
			return false;
		}
		if (!location.segment.Available)
		{
			return false;
		}
		if (CheckForCarAtLocation(location) != null)
		{
			return false;
		}
		return true;
	}

	public static float ApproximateLength(IEnumerable<CarDescriptor> descriptors)
	{
		List<CarDescriptor> list = descriptors.ToList();
		return list.Select((CarDescriptor desc) => desc.DefinitionInfo.Definition.Length).Sum() + (float)list.Count * 1f;
	}

	[CanBeNull]
	public Car CarForId(string carId)
	{
		return _carLookup.GetValueOrDefault(carId);
	}

	public bool TryGetCarForId(string carId, out Car car)
	{
		if (carId == null)
		{
			car = null;
			return false;
		}
		return _carLookup.TryGetValue(carId, out car);
	}

	public void RemoveCar(string carId)
	{
		StateManager.ApplyLocal(new RemoveCars(new List<string> { carId }));
	}

	public void RemoveAllCars()
	{
		StateManager.ApplyLocal(new RemoveCars(_carLookup.Keys.ToList()));
	}

	public void RemoveCarSmart(string carId)
	{
		Log.Information("RemoveCarSmart {carId}", carId);
		Car car = CarForId(carId);
		if (car == null)
		{
			Log.Error("Can't remove unknown car: {carId}", carId);
			return;
		}
		List<string> list = new List<string> { carId };
		if (car.set != null)
		{
			switch (car.Archetype)
			{
			case CarArchetype.LocomotiveSteam:
			{
				if (((SteamLocomotive)car).hasTender && car.set.TryGetCoupledCar(car, Car.End.R, out var coupled2) && coupled2.Archetype == CarArchetype.Tender)
				{
					list.Add(coupled2.id);
				}
				break;
			}
			case CarArchetype.Tender:
			{
				if (car.set.TryGetCoupledCar(car, Car.End.F, out var coupled) && coupled.Archetype == CarArchetype.LocomotiveSteam)
				{
					list.Add(coupled.id);
				}
				break;
			}
			}
		}
		StateManager.ApplyLocal(new RemoveCars(list));
	}

	public void RemoveAllCarsCoupledTo(string carId)
	{
		Log.Information("RemoveAllCarsCoupledTo {carId}", carId);
		StateManager.ApplyLocal(new RemoveCars((from c in CarForId(carId).EnumerateCoupled()
			select c.id).ToList()));
	}

	public int CarsInSegment(string segmentId)
	{
		TrackSegment segment = graph.GetSegment(segmentId);
		float length = segment.GetLength();
		HashSet<Car> hashSet = new HashSet<Car>();
		for (float num = 0f; num < length; num += 3f)
		{
			Car car = CheckForCarAtLocation(new Location(segment, num, TrackSegment.End.A));
			if (car != null)
			{
				hashSet.Add(car);
			}
		}
		return hashSet.Count;
	}

	public void MoveCar(Car car, Location location)
	{
		Log.Debug("MoveCar {car} {location}", car, location);
		car.WillMove();
		car.PositionFront(location, graph, MovementInfo.Zero, update: true);
		UpdateSets(new Car[1] { car });
	}

	public void MoveCarCoupleTo(Car car, Location loc, Car coupleToCar)
	{
		if (coupleToCar == null)
		{
			throw new ArgumentException("null coupleToCar", "coupleToCar");
		}
		float distanceBetweenClose = graph.GetDistanceBetweenClose(loc, coupleToCar.LocationA);
		float distanceBetweenClose2 = graph.GetDistanceBetweenClose(loc, coupleToCar.LocationB);
		Car.LogicalEnd logicalEnd = ((!(distanceBetweenClose < distanceBetweenClose2)) ? Car.LogicalEnd.B : Car.LogicalEnd.A);
		Car.End end = coupleToCar.LogicalToEnd(logicalEnd);
		Log.Information("MoveCarCoupleTo {car}, {coupleToLogicalEnd}, {coupleToEnd}", car, logicalEnd, end);
		loc = graph.LocationByMoving(coupleToCar.LocationFor(logicalEnd), (logicalEnd == Car.LogicalEnd.A) ? 1f : (-1f));
		if (logicalEnd == Car.LogicalEnd.A)
		{
			loc = loc.Flipped();
		}
		car.WillMove();
		car.PositionFront(loc, graph, MovementInfo.Zero, update: true);
		UpdateSets(new Car[1] { car });
		car.set.SetVelocity(0f, car.EnumerateCoupled().ToList());
		logicalEnd = coupleToCar.EndToLogical(end);
		Car.LogicalEnd logicalEnd2 = car.EndToLogical(Car.End.F);
		car.ApplyEndGearChange(logicalEnd2, Car.EndGearStateKey.IsCoupled, boolValue: true);
		coupleToCar.ApplyEndGearChange(logicalEnd, Car.EndGearStateKey.IsCoupled, boolValue: true);
		car.ApplyEndGearChange(logicalEnd2, Car.EndGearStateKey.IsAirConnected, boolValue: true);
		coupleToCar.ApplyEndGearChange(logicalEnd, Car.EndGearStateKey.IsAirConnected, boolValue: true);
	}

	[CanBeNull]
	private Car CheckForOtherCar(Car car, Car.End end, float distance)
	{
		bool num = end == Car.End.F;
		Location start = (num ? car.LocationF : car.LocationR);
		float distance2 = (num ? distance : (0f - distance));
		Location location = graph.LocationByMoving(start, distance2, checkSwitchAgainstMovement: false, stopAtEndOfTrack: true);
		return CheckForCarAtLocation(location);
	}

	private void UpdateSets()
	{
		UpdateSets(_carsForUpdateSets);
		_carsForUpdateSets.Clear();
	}

	private void UpdateSets(IEnumerable<Car> cars)
	{
		if (!IsHost)
		{
			return;
		}
		foreach (Car car6 in cars)
		{
			Location location = graph.LocationByMoving(car6.WheelBoundsF, 3f + car6.wheelInsetF, checkSwitchAgainstMovement: false, stopAtEndOfTrack: true);
			Location location2 = graph.LocationByMoving(car6.WheelBoundsR, -3f - car6.wheelInsetR, checkSwitchAgainstMovement: false, stopAtEndOfTrack: true);
			Car car = CheckForCarAtLocation(location);
			Car car2 = CheckForCarAtLocation(location2);
			if (car == car6)
			{
				car = null;
			}
			if (car2 == car6)
			{
				car2 = null;
			}
			if (car != null)
			{
				float limit = car6.wheelInsetF + 2f + Mathf.Max(car.wheelInsetF, car.wheelInsetR);
				if (!graph.CheckSameRoute(car6.WheelBoundsF, car.WheelBoundsF, limit) && !graph.CheckSameRoute(car6.WheelBoundsF, car.WheelBoundsR, limit))
				{
					car = null;
				}
			}
			if (car2 != null)
			{
				float limit2 = car6.wheelInsetR + 2f + Mathf.Max(car2.wheelInsetF, car2.wheelInsetR);
				if (!graph.CheckSameRoute(car6.WheelBoundsR, car2.WheelBoundsF, limit2) && !graph.CheckSameRoute(car6.WheelBoundsR, car2.WheelBoundsR, limit2))
				{
					car2 = null;
				}
			}
			if (car6.set == null)
			{
				if (car != null && car2 != null)
				{
					_integrationSets.Union(car6, car);
					_integrationSets.Union(car6, car2);
				}
				else if (car != null || car2 != null)
				{
					Car car3 = (car ? car : car2);
					_integrationSets.Union(car6, car3);
				}
				else
				{
					CreateIntegrationSet((IReadOnlyCollection<Car>)(object)new Car[1] { car6 });
				}
				continue;
			}
			int i;
			for (i = 0; i < 8; i++)
			{
				IntegrationSet.PositionInSet positionInSet = car6.set.PositionOfCar(car6);
				Car car4 = (car6.FrontIsA ? car : car2);
				Car car5 = (car6.FrontIsA ? car2 : car);
				bool flag = positionInSet == IntegrationSet.PositionInSet.A || positionInSet == IntegrationSet.PositionInSet.Inside;
				bool flag2 = positionInSet == IntegrationSet.PositionInSet.B || positionInSet == IntegrationSet.PositionInSet.Inside;
				if (flag && car5 != null && !SameSet(car5, car6))
				{
					if (!car6.TryGetAdjacentCar(Car.LogicalEnd.B, out var adjacent))
					{
						throw new Exception("Expected neighbor B");
					}
					_integrationSets.Split(car6, adjacent);
				}
				else if (flag2 && car4 != null && !SameSet(car4, car6))
				{
					if (!car6.TryGetAdjacentCar(Car.LogicalEnd.A, out var adjacent2))
					{
						throw new Exception("Expected neighbor A");
					}
					_integrationSets.Split(car6, adjacent2);
				}
				else if (flag2 && car4 == null)
				{
					if (!car6.TryGetAdjacentCar(Car.LogicalEnd.A, out var adjacent3))
					{
						throw new Exception("Expected neighbor A");
					}
					_integrationSets.Split(car6, adjacent3);
				}
				else if (flag && car5 == null)
				{
					if (!car6.TryGetAdjacentCar(Car.LogicalEnd.B, out var adjacent4))
					{
						throw new Exception("Expected neighbor B");
					}
					_integrationSets.Split(car6, adjacent4);
				}
				else if (car4 != null && !SameSet(car6, car4))
				{
					_integrationSets.Union(car6, car4);
				}
				else
				{
					if (!(car5 != null) || SameSet(car6, car5))
					{
						break;
					}
					_integrationSets.Union(car6, car5);
				}
			}
			if (i >= 8)
			{
				Log.Error("Car {car} in set {carSetId} exceeded loop count: {cars}", car6, car6.set?.Id, car6.set?.Cars);
			}
		}
		_integrationSets.SendDelta();
		static bool SameSet(Car a, Car b)
		{
			return a.set == b.set;
		}
	}

	private IntegrationSet CreateIntegrationSet(IReadOnlyCollection<Car> cars, uint? maybeId = null)
	{
		uint id = maybeId ?? GenerateIntegrationSetId();
		IntegrationSet integrationSet = CreateIntegrationSetInstance(id, cars);
		_integrationSets.Add(integrationSet);
		return integrationSet;
	}

	private IntegrationSet CreateIntegrationSetInstance(uint id, IReadOnlyCollection<Car> cars)
	{
		return IntegrationSet.Create(id, cars, graph, IsHost, this);
	}

	public uint GenerateIntegrationSetId()
	{
		return _integrationSets.GenerateId();
	}

	public void IntegrationSetDidCouple(Car car0, Car car1, float deltaVelocity)
	{
		if (IsHost)
		{
			Log.Information("IntegrationSetDidCouple: {car0}, {car1} @ {deltaVelocity} mph", car0, car1, deltaVelocity * 2.23694f);
			if (!graph.CheckSameRoute(car0.LocationB, car1.LocationA, 2f))
			{
				Log.Warning("Rejecting couple of {car0} {car0LocationB} + {car1} {car1LocationA} - not on same route", car0, car0.LocationB, car1, car1.LocationA);
			}
			else if (car0.IsDerailed || car1.IsDerailed)
			{
				Log.Warning("Rejecting couple of {car0} + {car1} - one or more are derailed", car0, car1);
			}
			else
			{
				car0.ApplyEndGearChange(Car.LogicalEnd.B, Car.EndGearStateKey.IsCoupled, boolValue: true);
				car1.ApplyEndGearChange(Car.LogicalEnd.A, Car.EndGearStateKey.IsCoupled, boolValue: true);
			}
		}
	}

	public void IntegrationSetCarsDidCollide(Car car0, [CanBeNull] Car car1, float deltaVelocity, bool isIn)
	{
		if (!IsHost)
		{
			return;
		}
		float num = deltaVelocity * 2.23694f;
		RequestSlackSound(car0, isIn, num);
		float num2 = config.damageForCollisionMph.Evaluate(Mathf.Abs(num));
		if (num2 < 0.01f)
		{
			return;
		}
		Log.Information("IntegrationSetCarsDidCollide: {car0}, {car1} @ {deltaVelocity}mph, dmg={damage}", car0, car1, num, num2);
		if (car1 != null)
		{
			float num3 = car0.Weight + car1.Weight;
			car0.ApplyConditionDelta((0f - num2) * car1.Weight / num3);
			car1.ApplyConditionDelta((0f - num2) * car0.Weight / num3);
		}
		else
		{
			car0.ApplyConditionDelta(0f - num2);
		}
		float num4 = Mathf.InverseLerp(10f, 20f, num);
		if (num4 > 0.001f)
		{
			Log.Information("IntegrationSetCarsDidCollide: {car0}, {car1} @ {deltaVelocity}mph, derail {derail}", car0, car1, num, num4);
			float num5 = ((car1 != null) ? (car1.Weight * deltaVelocity) : (car0.Weight * deltaVelocity));
			float num6 = car0.Weight * deltaVelocity;
			car0.ApplyDerailmentForce(num5, "Collision vs {0}, dmph={1} -> {2} vs {3}", car1, num, num5, num6);
			if (car1 != null)
			{
				car1.ApplyDerailmentForce(num6, "Collision vs {0}, dmph={1} -> {2} vs {3}", car0, num, num5, num6);
			}
		}
	}

	private void RequestSlackSound(Car car0, bool isIn, float deltaMph)
	{
		float valueOrDefault = _slackSoundRequests.GetValueOrDefault(car0.id, 0f);
		float time = Time.time;
		if (!(time - valueOrDefault < 0.5f))
		{
			_slackSoundRequests[car0.id] = time;
			Coupler coupler = car0.EndGearB.Coupler;
			Vector3 gamePosition = WorldTransformer.WorldToGame((coupler == null) ? car0.GetMotionSnapshot().Position : coupler.transform.position);
			float volume = Mathf.Clamp01(Mathf.InverseLerp(0.5f, 15f, deltaMph) + 0.5f);
			ScheduledAudioPlayer.HostPlaySoundAtPosition(isIn ? "slack-in" : "slack-out", gamePosition, AudioDistance.Local, AudioController.Group.WheelsRoll, 30, volume);
		}
	}

	public void IntegrationSetDidBreakAirHoses(Car car0, Car car1)
	{
		if (IsHost)
		{
			car0.ApplyEndGearChange(Car.LogicalEnd.B, Car.EndGearStateKey.IsAirConnected, boolValue: false);
			car1.ApplyEndGearChange(Car.LogicalEnd.A, Car.EndGearStateKey.IsAirConnected, boolValue: false);
		}
	}

	public void IntegrationSetRequestsBreakConnections(Car car, Car.LogicalEnd logicalEnd)
	{
		if (IsHost)
		{
			car.ApplyEndGearChange(logicalEnd, Car.EndGearStateKey.IsCoupled, boolValue: false);
			car.ApplyEndGearChange(logicalEnd, Car.EndGearStateKey.IsAirConnected, boolValue: false);
		}
	}

	[CanBeNull]
	public Car IntegrationSetCheckForCar(Vector3 point)
	{
		return CheckForCarAtPoint(point);
	}

	public void IntegrationSetRequestsReconnect(Car engine, Car tender)
	{
		if (IsHost)
		{
			Log.Debug("Reconnecting {engine} to {tender}", engine, tender);
			engine.ApplyEndGearChange(engine.EndToLogical(Car.End.R), Car.EndGearStateKey.IsAirConnected, boolValue: true);
			engine.ApplyEndGearChange(engine.EndToLogical(Car.End.R), Car.EndGearStateKey.IsCoupled, boolValue: true);
			tender.ApplyEndGearChange(tender.EndToLogical(Car.End.F), Car.EndGearStateKey.IsAirConnected, boolValue: true);
			tender.ApplyEndGearChange(tender.EndToLogical(Car.End.F), Car.EndGearStateKey.IsCoupled, boolValue: true);
		}
	}

	public void HandleCarSetAdd(Snapshot.CarSet snapshotSet)
	{
		Log.Debug("HandleCarSetAdd: {set}", snapshotSet);
		_integrationSets.AddWithoutDelta(snapshotSet, _carLookup);
	}

	public void HandleCarSetRemove(uint removeSetId)
	{
		Log.Debug("HandleCarSetRemove: {setId}", removeSetId);
		_integrationSets.RemoveWithoutDelta(removeSetId);
	}

	public void HandleCarSetChangeCars(Snapshot.CarSet changeSet)
	{
		Log.Debug("HandleCarSetChangeCars: {set}", changeSet);
		_integrationSets.ChangeCarsWithoutDelta(changeSet, _carLookup);
	}

	public static bool CanPlaceTrain(IPlayer player = null)
	{
		if (player == null)
		{
			player = StateManager.Shared.PlayersManager.LocalPlayer;
		}
		if (StateManager.IsSandbox)
		{
			return true;
		}
		if (IsHost)
		{
			return !player.IsRemote;
		}
		return false;
	}

	public void HandlePlaceTrain(IPlayer sender, PlaceTrain place)
	{
		LastPlacedTrain = null;
		if (!IsHost)
		{
			return;
		}
		if (!CanPlaceTrain(sender))
		{
			Log.Error("HandlePlaceTrain only allowed in sandbox.");
			return;
		}
		Location location = graph.MakeLocation(place.Location);
		List<CarDescriptor> descriptors = place.Cars.Select((PlaceTrain.Car car) => new CarDescriptor(PrefabStore.CarDefinitionInfoForIdentifier(car.PrototypeId), new CarIdent(car.ReportingMark, car.RoadNumber), null, car.TrainCrewId, car.Flipped, PropertyValueConverter.SnapshotToRuntime(car.Properties))).ToList();
		float num = ApproximateLength(descriptors);
		if (!CanPlaceAt(location, num))
		{
			throw new Exception($"Can't place a cut of estimated length {num:F3} at {location}");
		}
		using (StateManager.TransactionScope())
		{
			List<Car> list = HandleCreateCarsAsTrain(location, descriptors, place.CarIds, null);
			if (Multiplayer.IsClientActive)
			{
				StateManager.ApplyLocal(new AddCars(list.Select((Car c) => c.Snapshot()).ToList()));
			}
			ConnectCars(list);
			FillAir(list);
			ApplyHandbrakesAsNeeded(list, place.Handbrakes);
			_integrationSets.SendDelta();
			LastPlacedTrain = list;
		}
	}

	public void HandleAddCars(AddCars message)
	{
		if (!IsHost)
		{
			AddCarsInternal(message.Cars);
		}
	}

	public void HandleRequestSetSwitch(RequestSetSwitch setSwitch, IPlayer sender)
	{
		if (IsHost && !TrySetSwitch(setSwitch.nodeId, setSwitch.thrown, EntityReference.URI(EntityType.Player, sender.PlayerId.String), out var errorMessage))
		{
			Multiplayer.SendError(sender, errorMessage);
		}
	}

	public bool TrySetSwitch(string nodeId, bool thrown, string requesterUri, out string errorMessage)
	{
		if (!IsHost)
		{
			throw new Exception("Only host can set switch");
		}
		TrackNode node = graph.GetNode(nodeId);
		if (!CanSetSwitch(node, thrown, out var foundCar))
		{
			errorMessage = "Clear " + foundCar.DisplayName + " first";
			return false;
		}
		if (node.IsCTCSwitch && !node.IsCTCSwitchUnlocked)
		{
			errorMessage = "Switch is CTC controlled";
			return false;
		}
		StateManager.ApplyLocal(new SetSwitch(nodeId, thrown, StateManager.Now, requesterUri));
		errorMessage = null;
		return true;
	}

	public void HandleRequestSetSwitchUnlocked(RequestSetSwitchUnlocked setSwitchUnlocked, IPlayer sender)
	{
		if (IsHost)
		{
			if (!graph.GetNode(setSwitchUnlocked.NodeId).IsCTCSwitch)
			{
				Multiplayer.SendError(sender, "Switch is not CTC controlled");
				return;
			}
			CTCPanelController.Shared.SetSwitchUnlocked(setSwitchUnlocked.NodeId, setSwitchUnlocked.Unlocked);
			AuditManager.Shared.RecordSwitchAction(setSwitchUnlocked.NodeId, setSwitchUnlocked.Unlocked ? "Unlock" : "Lock", EntityReference.URI(EntityType.Player, sender.PlayerId.String));
		}
	}

	public void HandleSetGladhandsConnected(string carIdA, string carIdB, bool connect)
	{
		if (IsHost)
		{
			Car a = CarForId(carIdA);
			Car b = CarForId(carIdB);
			if (a == null || b == null)
			{
				throw new ArgumentException("Bad car id");
			}
			if (a.set != b.set)
			{
				throw new ArgumentException("Cars are not in same set");
			}
			a.set.OrderAB(ref a, ref b);
			a.ApplyEndGearChange(Car.LogicalEnd.B, Car.EndGearStateKey.IsAirConnected, connect);
			b.ApplyEndGearChange(Car.LogicalEnd.A, Car.EndGearStateKey.IsAirConnected, connect);
		}
	}

	public void FixSwitchAgainstMovement(Car car, TrackNode node)
	{
		Debug.Log("Fixing switch thrown against", node);
		if (CarOnSwitch(node, car, out var car2))
		{
			Debug.LogError($"FixSwitchAgainstMovement({car}, {node.id}): car on switch not matching: {car2}.");
		}
		else
		{
			StateManager.ApplyLocal(new SetSwitch(node.id, !node.isThrown, StateManager.Now, EntityReference.URI(EntityType.Car, car.id)));
		}
	}

	public void HandleSetSwitch(SetSwitch setSwitch)
	{
		TrackNode node = graph.GetNode(setSwitch.NodeId);
		if (node == null)
		{
			throw new Exception("No such switch: " + setSwitch.NodeId);
		}
		if (setSwitch.Thrown != node.isThrown)
		{
			node.isThrown = setSwitch.Thrown;
			if (IsHost)
			{
				AuditManager.Shared.RecordSwitchAction(node.id, setSwitch.Thrown ? "Throw Reversed" : "Throw Normal", setSwitch.Requester);
			}
		}
	}

	private CarDescriptor CarDescriptorFromSnapshotCar(Snapshot.Car snapshotCar, Dictionary<string, IPropertyValue> snapshotProperties = null)
	{
		TypedContainerItem<CarDefinition> definitionInfo = PrefabStore.CarDefinitionInfoForIdentifier(snapshotCar.prototypeId);
		Dictionary<string, Value> properties = PropertyValueConverter.SnapshotToRuntime(snapshotProperties);
		return new CarDescriptor(definitionInfo, new CarIdent(snapshotCar.ReportingMark, snapshotCar.roadNumber), snapshotCar.Bardo, snapshotCar.TrainCrewId, !snapshotCar.FrontIsA, properties);
	}

	private void AddCarsInternal(List<Snapshot.Car> snapshotCars)
	{
		IEnumerable<CarDescriptor> descriptors = snapshotCars.Select((Snapshot.Car snapshotCar) => CarDescriptorFromSnapshotCar(snapshotCar));
		IEnumerable<string> carIds = snapshotCars.Select((Snapshot.Car car) => car.id);
		List<Snapshot.TrackLocation> frontLocations = snapshotCars.Select((Snapshot.Car car) => car.Location).ToList();
		HandleCreateCarsAsTrain(Location.Invalid, descriptors, carIds, frontLocations);
	}

	private Car AddCarInternal(Snapshot.Car snapshotCar, Dictionary<string, IPropertyValue> snapshotCarProperties, int version = 1)
	{
		CarDescriptor descriptor = CarDescriptorFromSnapshotCar(snapshotCar, snapshotCarProperties);
		Car car = CreateCarIfNeeded(descriptor, snapshotCar.id);
		CreateCarCullerIfNeeded();
		Location wheelBoundsF = WheelBoundsFrontFromSnapshotLocation(version, car, graph.MakeLocation(snapshotCar.Location));
		car.PositionWheelBoundsFront(wheelBoundsF, graph, MovementInfo.Zero, update: true);
		car.velocity = snapshotCar.velocity;
		Log.Information("AddCar {carId} {displayName} {definitionId}", car.id, car.DisplayName, car.DefinitionInfo.Identifier);
		if (!car.IsInBardo)
		{
			_carCuller.PostAdd(car);
		}
		if (car.IsLocomotive)
		{
			_cachedSizePreference = null;
		}
		return car;
	}

	public void HandleRemoveCars(RemoveCars message)
	{
		RemoveCars(message.CarIds);
	}

	public void WillUnloadMap()
	{
		RemoveCars(Cars.Select((Car car) => car.id).ToList());
	}

	private void RemoveCars(List<string> carIds)
	{
		HashSet<Car> hashSet = new HashSet<Car>();
		foreach (string carId in carIds)
		{
			if (!TryGetCarForId(carId, out var car))
			{
				Log.Warning("RemoveCars ignoring unknown carId {carId}", carId);
				continue;
			}
			car.PendingDestroy = true;
			hashSet.Add(car);
		}
		Log.Information("RemoveCars {cars}", hashSet);
		foreach (Car item in hashSet)
		{
			WillRemoveCar(item);
			UnityEngine.Object.Destroy(item.gameObject);
		}
	}

	private void WillRemoveCar(Car car, bool isMovingToBardo = false)
	{
		string id = car.id;
		_carsForUpdateSets.Remove(car);
		if (!isMovingToBardo)
		{
			DeallocateRoadNumber(car);
			_carLookup.Remove(id);
			_cars.Remove(car);
		}
		_spatialHash.Remove(id);
		_carCuller.Remove(car);
		_carIdsNearbyPlayer.Remove(car.id);
		_carSegmentCache.RemoveCarSegments(car.id, removeRecord: true);
		if (car.IsLocomotive)
		{
			_cachedSizePreference = null;
		}
		if (_selectedCarId == id)
		{
			_selectedCarId = null;
			Messenger.Default.Send(default(SelectedCarChanged));
		}
		if (_lastSelectedCarId == id)
		{
			_lastSelectedCarId = null;
		}
		CameraSelector.shared.WillDestroyCar(car);
		OpsController.Shared?.RemoveCar(car.id);
		car.WillDestroy(isMovingToBardo);
		car.SetAdjacentCarsNotConnected();
		if (car.set != null)
		{
			_integrationSets.RemoveCar(car);
		}
	}

	public void HandleSnapshotCars(int version, Dictionary<string, Snapshot.Car> snapshotCars, Dictionary<uint, Snapshot.CarSet> carSets, List<BatchCarAirUpdate> airBatches, Dictionary<string, Dictionary<string, IPropertyValue>> snapshotProperties)
	{
		RemoveCars(_carLookup.Keys.ToList());
		_integrationSets.Clear();
		_spatialHash.Reserve(snapshotCars.Count);
		Dictionary<string, Car> dictionary = new Dictionary<string, Car>();
		HashSet<string> hashSet = new HashSet<string>();
		List<Snapshot.CarSet> list = new List<Snapshot.CarSet>();
		Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
		foreach (Snapshot.Car value3 in snapshotCars.Values)
		{
			try
			{
				Dictionary<string, IPropertyValue> valueOrDefault = snapshotProperties.GetValueOrDefault(value3.id);
				Car car = AddCarInternal(value3, valueOrDefault, version);
				dictionary[car.id] = car;
			}
			catch (InvalidLocationException exception)
			{
				Log.Error(exception, "Invalid Location for car {car} {loc}, will add to LostCarCuts", value3, value3.Location);
				RemoveCars(new List<string> { value3.id });
				hashSet.Add(value3.id);
			}
			catch (PrefabStore.UnknownIdentifierException ex)
			{
				Log.Error(ex, "Car definition unknown: {car}; car will be missing. {e}", value3, ex);
				RemoveCars(new List<string> { value3.id });
				dictionary2[value3.id] = value3.ToString();
			}
			catch (Exception ex2)
			{
				Log.Error(ex2, "Error from AddCarInternal {car}; car will be missing. {e}", value3, ex2);
				Debug.LogException(ex2);
				RemoveCars(new List<string> { value3.id });
				dictionary2[value3.id] = value3.ToString();
			}
		}
		List<Snapshot.CarSet> list2 = new List<Snapshot.CarSet>();
		foreach (Snapshot.CarSet carSet in carSets.Values)
		{
			if (carSet.CarIds.Count == 0)
			{
				Log.Warning("Received CarSet {id} with no cars", carSet.Id);
				continue;
			}
			if (hashSet.Any((string id) => carSet.CarIds.Contains(id)))
			{
				list.Add(carSet);
				RemoveCars(carSet.CarIds);
				continue;
			}
			if (dictionary2.Keys.Any((string id) => carSet.CarIds.Contains(id)))
			{
				Log.Debug("CarSet {id} contains unknown car(s); will not be created directly. Full set: {set}", carSet.Id, carSet.CarIds);
				list2.Add(carSet);
				continue;
			}
			try
			{
				List<Car> list3 = new List<Car>();
				foreach (string carId in carSet.CarIds)
				{
					if (dictionary.TryGetValue(carId, out var value))
					{
						list3.Add(value);
					}
					else if (!hashSet.Contains(carId) && !dictionary2.ContainsKey(carId))
					{
						Log.Error("Unknown car {carId} in set {carSet}", carId, carSet.Id);
					}
				}
				IntegrationSet integrationSet = CreateIntegrationSetInstance(carSet.Id, list3);
				if (integrationSet.ContainsBrokenConstraints())
				{
					list.Add(carSet);
					RemoveCars(carSet.CarIds);
				}
				else
				{
					_integrationSets.Add(integrationSet);
				}
			}
			catch (Exception exception2)
			{
				Log.Error(exception2, "Error handling snapshot CarSet {carSetId}", carSet.Id);
				Debug.LogException(exception2);
			}
		}
		_integrationSets.ClearDeltas();
		LostCarCuts.Clear();
		if (IsHost)
		{
			foreach (Snapshot.CarSet item2 in list)
			{
				Log.Information("Attempting to fix car set {id} with cars {carIds} which had invalid location(s)", item2.Id, item2.CarIds);
				try
				{
					List<LostCar> item = item2.CarIds.Select(delegate(string carId)
					{
						Snapshot.Car snapshotCar = snapshotCars[carId];
						snapshotProperties.Remove(carId, out var value2);
						CarDescriptor descriptor = CarDescriptorFromSnapshotCar(snapshotCar, value2);
						Vector3? snapshotCarBodyPosition = GetSnapshotCarBodyPosition(carId);
						return new LostCar(carId, descriptor, snapshotCarBodyPosition);
					}).ToList();
					LostCarCuts.Add(item);
				}
				catch (Exception exception3)
				{
					Log.Error(exception3, "Error preparing LostCarCuts entry {carSetId} {carSetCars}", item2.Id, item2.CarIds);
					Debug.LogException(exception3);
				}
			}
			foreach (Snapshot.CarSet item3 in list2)
			{
				StateManager.ApplyLocal(new CarSetRemove(item3.Id));
			}
			ShowLostCarCutsWindowIfNeeded();
		}
		foreach (BatchCarAirUpdate airBatch in airBatches)
		{
			try
			{
				HandleBatchCarAirUpdate(airBatch);
			}
			catch (Exception exception4)
			{
				Debug.LogException(exception4);
				Log.Error(exception4, "Error from HandleBatchCarAirUpdate {carIds}; air may be incorrect.", airBatch.CarIds);
			}
		}
	}

	private Location WheelBoundsFrontFromSnapshotLocation(int version, Car car, Location location)
	{
		try
		{
			return version switch
			{
				0 => graph.LocationByMoving(location, 0f - car.wheelInsetF), 
				_ => graph.LocationByMoving(location, car.carLength / 2f - car.wheelInsetF), 
			};
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Car {car}: Error adjusting location {loc} from version {version}", car, location, version);
			return location;
		}
	}

	private void SendCarPositionsIfNeeded()
	{
		ClientManager client = Multiplayer.Client;
		long now = StateManager.Now;
		foreach (IntegrationSet integrationSet in _integrationSets)
		{
			if (!(integrationSet.LastSentTime + 100f > (float)now) && (integrationSet.Dirty || !(integrationSet.LastSentTime + 5000f > (float)now)))
			{
				SendCarPositionsForSet(integrationSet, critical: false, client);
				integrationSet.LastSentTime = now;
			}
		}
	}

	private void SendCarPositionsForSet(IntegrationSet set, bool critical, ClientManager client)
	{
		set.SendBatchCarPositionUpdate(client, critical);
	}

	public void HandleBatchCarPositionUpdate(BatchCarPositionUpdate update)
	{
		_integrationSets.HandleBatchCarPositionUpdate(update, graph);
	}

	private void SendAirIfNeeded()
	{
		ClientManager client = Multiplayer.Client;
		long now = StateManager.Now;
		long num = now - 1000;
		foreach (IntegrationSet integrationSet in _integrationSets)
		{
			bool flag = false;
			foreach (Car car2 in integrationSet.Cars)
			{
				bool flag2 = car2.air.NeedsSend && car2.air.LastSentTick < num;
				flag = flag || flag2;
			}
			if (!flag)
			{
				continue;
			}
			List<Car> list = integrationSet.Cars.ToList();
			for (int i = 0; i < list.Count; i += 10)
			{
				int num2 = Mathf.Min(10, list.Count - i);
				BatchCarAirUpdate batchCarAirUpdate = new BatchCarAirUpdate(now, new string[num2], new byte[num2], new byte[num2], new byte[num2]);
				for (int j = 0; j < num2; j++)
				{
					int index = i + j;
					batchCarAirUpdate.CarIds[j] = list[index].id;
					Car car = list[index];
					batchCarAirUpdate.BrakeLineValues[j] = BatchCarAirUpdate.ValueToByte(car.air.BrakeLine.Pressure);
					batchCarAirUpdate.BrakeReservoirValues[j] = BatchCarAirUpdate.ValueToByte(car.air.BrakeReservoir.Pressure);
					batchCarAirUpdate.BrakeCylinderValues[j] = BatchCarAirUpdate.ValueToByte(car.air.BrakeCylinder.Pressure);
				}
				client.Send(batchCarAirUpdate);
			}
			foreach (Car item in list)
			{
				item.air.NeedsSend = false;
				item.air.LastSentTick = now;
			}
		}
	}

	private static BatchCarAirUpdate CreateBatchCarAirUpdate(IReadOnlyCollection<Car> cars, long tick)
	{
		return new BatchCarAirUpdate(tick, cars.Select((Car c) => c.id).ToArray(), cars.Select((Car c) => BatchCarAirUpdate.ValueToByte(c.air.BrakeLine.Pressure)).ToArray(), cars.Select((Car c) => BatchCarAirUpdate.ValueToByte(c.air.BrakeReservoir.Pressure)).ToArray(), cars.Select((Car c) => BatchCarAirUpdate.ValueToByte(c.air.BrakeCylinder.Pressure)).ToArray());
	}

	public void HandleBatchCarAirUpdate(BatchCarAirUpdate update)
	{
		for (int i = 0; i < update.CarIds.Length; i++)
		{
			string text = update.CarIds[i];
			if (!TryGetCarForId(text, out var car))
			{
				Log.Warning("BatchCarAirUpdate: Unknown car: {CarId}", text);
				continue;
			}
			float brakeLinePressure = BatchCarAirUpdate.ByteToValue(update.BrakeLineValues[i]);
			float brakeRes = BatchCarAirUpdate.ByteToValue(update.BrakeReservoirValues[i]);
			float brakeCyl = BatchCarAirUpdate.ByteToValue(update.BrakeCylinderValues[i]);
			car.air.SetAir(brakeLinePressure, brakeRes, brakeCyl);
		}
	}

	[ContractAnnotation("=> true, foundCar: null; => false, foundCar: notnull")]
	public bool CanSetSwitch(TrackNode node, bool thrown, out Car foundCar)
	{
		if (node.isThrown == thrown)
		{
			foundCar = null;
			return true;
		}
		if (!CarOnSwitch(node, null, out foundCar))
		{
			return true;
		}
		graph.DecodeSwitchAt(node, out var _, out var a, out var b);
		Car car = foundCar;
		bool num = !thrown;
		bool flag = thrown;
		if (!num || !IsOnSegment(a))
		{
			if (flag)
			{
				return IsOnSegment(b);
			}
			return false;
		}
		return true;
		bool IsOnSegment(TrackSegment segment)
		{
			if (!(car.WheelBoundsF.segment == segment))
			{
				return car.WheelBoundsR.segment == segment;
			}
			return true;
		}
	}

	private bool CarOnSwitch(TrackNode node, Car otherThan, out Car car)
	{
		graph.DecodeSwitchAt(node, out var _, out var a, out var b);
		if (CarWheelBoundsOver(node, out car) && car != otherThan)
		{
			return true;
		}
		Location loc = new Location(a, 4f, a.EndForNode(node));
		Location loc2 = new Location(b, 4f, b.EndForNode(node));
		if (ContainsCar(loc, 2f, 2f, 0.5f, out car) && car != otherThan)
		{
			return true;
		}
		if (ContainsCar(loc2, 2f, 2f, 0.5f, out car) && car != otherThan)
		{
			return true;
		}
		return false;
	}

	public bool CarWheelBoundsOver(TrackNode node, out Car car)
	{
		Vector3 vector = node.transform.GamePosition();
		car = CheckForCarAtPoint(vector);
		if (car == null)
		{
			return false;
		}
		Vector3 position = graph.GetPosition(car.WheelBoundsF);
		return Vector3.Dot(rhs: (graph.GetPosition(car.WheelBoundsR) - vector).normalized, lhs: (position - vector).normalized) < -0.5f;
	}

	private bool ContainsCar(Location loc, float distance, float step, float radius, out Car car)
	{
		for (float num = 0f; num <= distance; num += step)
		{
			car = CheckForCarAtLocation(loc, radius);
			if (car != null)
			{
				return true;
			}
			loc = graph.LocationByMoving(loc, distance, checkSwitchAgainstMovement: false, Graph.EndOfTrackHandling.Unclamped);
		}
		car = null;
		return false;
	}

	[CanBeNull]
	public Car CarForString(string query)
	{
		Car car = CarForId(query);
		if (car != null)
		{
			return car;
		}
		foreach (Car car2 in _cars)
		{
			string b = car2.Ident.ReportingMark + car2.Ident.RoadNumber;
			if (string.Equals(query, b, StringComparison.OrdinalIgnoreCase) || string.Equals(query, car2.DisplayName, StringComparison.OrdinalIgnoreCase))
			{
				return car2;
			}
		}
		return null;
	}

	public void PopulateSnapshotForSave(ref Snapshot snapshot, out Dictionary<string, Vector3[]> carBodyPositions, Car.SnapshotOption carSnapshotOption)
	{
		carBodyPositions = new Dictionary<string, Vector3[]>();
		snapshot.CarSets = _integrationSets.Select((IntegrationSet carSet) => carSet.Snapshot()).ToDictionary((Snapshot.CarSet cs) => cs.Id, (Snapshot.CarSet cs) => cs);
		foreach (Car car in _cars)
		{
			try
			{
				car.PrepareForSnapshotSave();
				snapshot.Cars[car.id] = car.Snapshot(carSnapshotOption);
				snapshot.Properties[car.id] = car.KeyValueObject.SnapshotValues();
				carBodyPositions[car.id] = car.LastBodyPosition;
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				Log.Error(exception, "Exception while creating snapshot of car {car}", car);
			}
		}
		foreach (IntegrationSet integrationSet in _integrationSets)
		{
			snapshot.CarAir.Add(CreateBatchCarAirUpdate(integrationSet.Cars.ToList(), 0L));
		}
		foreach (TrackNode node in graph.Nodes)
		{
			if (node.isThrown)
			{
				snapshot.thrownSwitchIds.Add(node.id);
			}
		}
		foreach (TurntableController turntableController in graph.TurntableControllers)
		{
			snapshot.Turntables[turntableController.turntable.id] = new Snapshot.TurntableState(turntableController.turntable.Angle, turntableController.turntable.StopIndex);
		}
	}

	public void HandleSnapshotSwitches(HashSet<string> thrownSwitchIds)
	{
		foreach (TrackNode node in Shared.graph.Nodes)
		{
			node.isThrown = thrownSwitchIds.Contains(node.id);
		}
	}

	public void HandleSnapshotTurntables(Dictionary<string, Snapshot.TurntableState> states)
	{
		foreach (KeyValuePair<string, Snapshot.TurntableState> state2 in states)
		{
			state2.Deconstruct(out var key, out var value);
			string text = key;
			Snapshot.TurntableState state = value;
			TurntableController turntableController = graph.TurntableControllerForId(text);
			if (turntableController == null)
			{
				Log.Error("Couldn't find turntable in snapshot: {turntableId}", text);
			}
			else
			{
				turntableController.RestoreFromSnapshot(state);
			}
		}
	}

	public void HandleManualMoveCar(string carId, int direction)
	{
		if (IsHost)
		{
			Car car = CarForId(carId);
			if (car == null)
			{
				throw new ArgumentException("No such car");
			}
			float velocity = Mathf.Sign(direction) * config.manualMoveCarForce / car.Weight;
			car.ResetAtRest();
			car.set.AddVelocityToCar(car, velocity, 1.3411179f);
		}
	}

	public void HandleRequestSetIdent(IPlayer sender, string carId, CarIdent ident)
	{
		StateManager.AssertIsHost();
		Car car = CarForId(carId);
		ident.ReportingMark = ident.ReportingMark.StripHtml().Trim().Truncate(6);
		ident.RoadNumber = ident.RoadNumber.StripHtml().Trim().Truncate(6);
		if (string.IsNullOrEmpty(ident.ReportingMark) || string.IsNullOrEmpty(ident.RoadNumber))
		{
			ValidationError("Must be non-empty.");
			return;
		}
		if (!RoadNumberAllocator.ValidateRoadNumber(ident.RoadNumber))
		{
			ValidationError("Road number format not supported.");
			return;
		}
		foreach (Car car2 in Cars)
		{
			if (!(car2 == car) && car2.Ident.Equals(ident))
			{
				ValidationError("Already in use.");
				return;
			}
		}
		if (car is SteamLocomotive steamLocomotive && steamLocomotive.TryGetTender(out var tender))
		{
			StateManager.ApplyLocal(new CarSetIdent(tender.id, ident.ReportingMark, ident.RoadNumber + "T"));
		}
		StateManager.ApplyLocal(new CarSetIdent(carId, ident.ReportingMark, ident.RoadNumber));
		void ValidationError(string message)
		{
			Multiplayer.SendError(sender, message);
			StateManager.Shared.SendFireEvent(default(RequestRejected));
		}
	}

	public void HandleSetIdent(string carId, CarIdent ident)
	{
		Car car = CarForId(carId);
		if (car == null)
		{
			throw new ArgumentException("No such car", "carId");
		}
		Log.Information("CarSetIdent: {carId} {old} -> {new}", carId, car.Ident, ident);
		CarIdent ident2 = car.Ident;
		try
		{
			DeallocateRoadNumber(car);
			car.SetIdent(ident);
			AllocateRoadNumber(car.Descriptor());
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Exception while setting ident {car} to {ident}", car, ident);
			car.SetIdent(ident2);
			throw;
		}
		Messenger.Default.Send(new CarIdentChanged(car.id));
		car.ReloadModel();
	}

	public void HandleSetCarTrainCrew(IPlayer sender, string carId, string trainCrewId)
	{
		Car car = CarForId(carId);
		string trainCrewId2 = car.trainCrewId;
		car.trainCrewId = trainCrewId;
		Messenger.Default.Send(new CarTrainCrewChanged(car.id));
		if (car is SteamLocomotive steamLocomotive && steamLocomotive.TryGetTender(out var tender))
		{
			tender.trainCrewId = trainCrewId;
			Messenger.Default.Send(new CarTrainCrewChanged(tender.id));
		}
		if (IsHost)
		{
			StateManager.Shared.PlayersManager.TrainCrewForId(trainCrewId, out var trainCrew);
			if (trainCrew != null)
			{
				Multiplayer.Broadcast(sender.Name + " has assigned " + car.DisplayName + " to " + trainCrew.Name);
			}
			else
			{
				string text = StateManager.Shared.PlayersManager.NameForTrainCrewId(trainCrewId2);
				Multiplayer.Broadcast(sender.Name + " has unassigned " + car.DisplayName + " from " + text + ".");
			}
		}
	}

	public void PostRestoreProperties()
	{
		foreach (Car car2 in Cars)
		{
			car2.PostRestoreProperties();
		}
		string selectedCarId = PlayerPropertiesManager.Shared.MyProperties.SelectedCarId;
		if (TryGetCarForId(selectedCarId, out var car))
		{
			SelectedCar = car;
		}
	}

	public void LogCarSets()
	{
		_integrationSets.LogState();
	}

	public void HandleRerail(string[] carIds, float amount, IPlayer sender)
	{
		if (!IsHost)
		{
			return;
		}
		amount = Mathf.Clamp01(amount);
		int num = 0;
		foreach (string carId in carIds)
		{
			if (TryGetCarForId(carId, out var car) && car.IsDerailed)
			{
				car.ApplyDerailmentDelta(0f - amount, "");
				if (!car.IsDerailed)
				{
					num++;
				}
			}
		}
		if (num != 0)
		{
			Multiplayer.Broadcast(sender.Name + " rerailed " + num.Pluralize("car") + ".");
		}
	}

	private bool TryGetAutoEngineerPlanner(string locomotiveId, out AutoEngineerPlanner planner)
	{
		if (TryGetCarForId(locomotiveId, out var car) && car is BaseLocomotive baseLocomotive && baseLocomotive.AutoEngineerPlanner != null)
		{
			planner = baseLocomotive.AutoEngineerPlanner;
			return true;
		}
		planner = null;
		return false;
	}

	public void HandleAutoEngineerCommand(AutoEngineerCommand command, IPlayer sender)
	{
		if (IsHost && TryGetAutoEngineerPlanner(command.LocomotiveId, out var planner))
		{
			planner.HandleCommand(command, sender);
		}
	}

	public void HandleAutoEngineerWaypointRerouteRequest(string locomotiveId, IPlayer sender)
	{
		if (IsHost && TryGetAutoEngineerPlanner(locomotiveId, out var planner))
		{
			planner.HandleRequestReroute(sender);
		}
	}

	public void HandleAutoEngineerContextualOrder(AutoEngineerContextualOrder contextualOrder, IPlayer sender)
	{
		if (IsHost && TryGetAutoEngineerPlanner(contextualOrder.LocomotiveId, out var planner))
		{
			planner.HandleContextualOrder(contextualOrder);
		}
	}

	public void HandleAutoEngineerWaypointRouteRequest(AutoEngineerWaypointRouteRequest request, IPlayer sender)
	{
		if (!IsHost || !TryGetAutoEngineerPlanner(request.LocomotiveId, out var planner))
		{
			return;
		}
		List<Snapshot.TrackLocation> list = new List<Snapshot.TrackLocation>();
		List<Location> list2 = CollectionPool<List<Location>, Location>.Get();
		planner.GetWaypointRouteLocation(list2, out var hasMoreSteps);
		foreach (Location item in list2)
		{
			list.Add(Graph.CreateSnapshotTrackLocation(item));
		}
		CollectionPool<List<Location>, Location>.Release(list2);
		AutoEngineerWaypointRouteResponse autoEngineerWaypointRouteResponse = new AutoEngineerWaypointRouteResponse(request.LocomotiveId, list, hasMoreSteps);
		StateManager.Shared.SendTo(sender, autoEngineerWaypointRouteResponse);
	}

	public void HandleAutoEngineerWaypointRouteResponse(AutoEngineerWaypointRouteResponse response, IPlayer sender)
	{
		if (AutoEngineerWaypointControls.Shared == null)
		{
			return;
		}
		List<Location> list = CollectionPool<List<Location>, Location>.Get();
		foreach (Snapshot.TrackLocation location in response.Locations)
		{
			list.Add(graph.MakeLocation(location));
		}
		AutoEngineerWaypointOverlayController.Shared.DidReceiveRoute(response.LocomotiveId, list, response.HasMore);
		CollectionPool<List<Location>, Location>.Release(list);
	}

	public void HandleAutoEngineerWaypointRouteUpdate(AutoEngineerWaypointRouteUpdate change)
	{
		AutoEngineerWaypointControls shared = AutoEngineerWaypointControls.Shared;
		if (!(shared == null))
		{
			Location? currentStepLocation = (change.Current.HasValue ? new Location?(graph.MakeLocation(change.Current.Value)) : ((Location?)null));
			shared.WaypointRouteDidUpdate(change.LocomotiveId, currentStepLocation, change.RouteChanged);
		}
	}

	private IndustryContext.CarSizePreference InferSizePreference()
	{
		if (!TryGetMedianTractiveEffort(out var median))
		{
			return IndustryContext.CarSizePreference.Medium;
		}
		IndustryContext.CarSizePreference carSizePreference = ((median < 35000f) ? ((!(median < 22500f)) ? IndustryContext.CarSizePreference.Medium : IndustryContext.CarSizePreference.Small) : ((!(median < 50000f)) ? IndustryContext.CarSizePreference.ExtraLarge : IndustryContext.CarSizePreference.Large));
		IndustryContext.CarSizePreference carSizePreference2 = carSizePreference;
		Log.Debug("Size Preference: {median} -> {sizePreference}", (int)median, carSizePreference2);
		return carSizePreference2;
	}

	private bool TryGetMedianTractiveEffort(out float median)
	{
		List<BaseLocomotive> list = (from BaseLocomotive p in Cars.Where((Car car) => car.IsLocomotive)
			orderby p.RatedTractiveEffort
			select p).ToList();
		if (list.Count == 0)
		{
			median = 0f;
			return false;
		}
		int count = list.Count;
		median = list.ElementAt(count / 2).RatedTractiveEffort + list.ElementAt((count - 1) / 2).RatedTractiveEffort;
		median /= 2f;
		return true;
	}

	private void UpdateCarsNearbyPlayer()
	{
		Vector3 position = CameraSelector.shared.localAvatar.character.character.GetMotionSnapshot().Position;
		Vector3 center = WorldTransformer.WorldToGame(position);
		List<(string carId, float distance)> source = CarIdsInRadius(center, 30f).Select(delegate(string carId)
		{
			Car car = CarForId(carId);
			if (car == null)
			{
				return (carId: carId, distance: 1000f);
			}
			Vector3 a = WorldTransformer.WorldToGame(car.GetMotionSnapshot().Position);
			return (carId: carId, distance: Vector3.Distance(a, center));
		}).ToList();
		HashSet<string> first = (from t in source
			where t.distance < 21f
			select t.carId).ToHashSet();
		HashSet<string> second = (from t in source
			where t.distance < 30f
			select t.carId).ToHashSet();
		HashSet<string> hashSet = _carIdsNearbyPlayer.Except(second).ToHashSet();
		HashSet<string> hashSet2 = first.Except(_carIdsNearbyPlayer).ToHashSet();
		foreach (string item in hashSet)
		{
			_carLookup[item].SetPlayerNearby(nearby: false);
		}
		foreach (string item2 in hashSet2)
		{
			_carLookup[item2].SetPlayerNearby(nearby: true);
		}
		_carIdsNearbyPlayer.UnionWith(hashSet2);
		_carIdsNearbyPlayer.ExceptWith(hashSet);
	}

	private IEnumerator UpdateCarsNearbyPlayerCoroutine()
	{
		while (true)
		{
			yield return new WaitForSecondsRealtime(1f);
			yield return new WaitForFixedUpdate();
			UpdateCarsNearbyPlayer();
		}
	}

	private string AllocateRoadNumber(CarDescriptor descriptor)
	{
		string key = descriptor.Ident.ReportingMark.ToUpper();
		if (!_roadNumberAllocators.TryGetValue(key, out var value))
		{
			value = (_roadNumberAllocators[key] = new RoadNumberAllocator());
		}
		bool forceSequential = descriptor.Properties.GetValueOrDefault("owned");
		return value.Allocate(descriptor.DefinitionInfo.Definition.BaseRoadNumber, descriptor.Ident.RoadNumber, forceSequential);
	}

	private void DeallocateRoadNumber(Car car)
	{
		if (_roadNumberAllocators.TryGetValue(car.Ident.ReportingMark.ToUpper(), out var value))
		{
			value.Release(car.Ident.RoadNumber);
		}
	}

	private void GraphDidRebuildCollections()
	{
		if (!IsHost)
		{
			return;
		}
		HashSet<Car> hashSet = new HashSet<Car>();
		foreach (Car car in Cars)
		{
			if (CarIsLost(car))
			{
				hashSet.Add(car);
			}
		}
		if (hashSet.Count == 0)
		{
			return;
		}
		foreach (Car lostCar in hashSet)
		{
			if (!LostCarCuts.Any((List<LostCar> cut) => cut.Any((LostCar car) => car.Id == lostCar.id)))
			{
				List<LostCar> list = lostCar.EnumerateCoupled().Select(delegate(Car car)
				{
					Vector3? snapshotCarBodyPosition = GetSnapshotCarBodyPosition(car.id);
					return new LostCar(car.id, car.Descriptor(), snapshotCarBodyPosition);
				}).ToList();
				Log.Information("LostCarCuts: Added cut of {count}", list.Count);
				LostCarCuts.Add(list);
			}
		}
		StateManager.ApplyLocal(new RemoveCars(hashSet.Select((Car c) => c.id).ToList()));
		ShowLostCarCutsWindowIfNeeded();
		static bool CarIsLost(Car car)
		{
			if (!LocationOnDisabledTrack(car.WheelBoundsF) && !LocationOnDisabledTrack(car.WheelBoundsR) && !LocationOnDisabledTrack(car.LocationF))
			{
				return LocationOnDisabledTrack(car.LocationR);
			}
			return true;
		}
		static bool LocationOnDisabledTrack(Location loc)
		{
			if (!(loc.segment == null))
			{
				return !loc.segment.GroupEnabled;
			}
			return true;
		}
	}

	public void ShowLostCarCutsWindowIfNeeded()
	{
		if (LostCarCuts.Any() && IsHost)
		{
			_showLostCarCuts.RequestRun();
		}
	}

	private static Vector3? GetSnapshotCarBodyPosition(string carId)
	{
		if (HostManager.Shared.SnapshotCarBodyPositions.TryGetValue(carId, out var value))
		{
			return value[0];
		}
		return null;
	}

	public bool SelectRecall()
	{
		string lastSelectedCarId = _lastSelectedCarId;
		if (_lastSelectedCarId == _selectedCarId || !TryGetCarForId(lastSelectedCarId, out var car))
		{
			return false;
		}
		SelectedCar = car;
		return true;
	}

	public void MoveToBardo(string carId, string senderId)
	{
		StateManager.AssertIsHost();
		StateManager.ApplyLocal(new CarSetBardo(carId, senderId));
	}

	public void HandleSetBardo(string carId, string bardo)
	{
		string.IsNullOrEmpty(bardo);
		if (!TryGetCarForId(carId, out var car))
		{
			throw new Exception("No such car: " + carId);
		}
		if (car.Bardo == bardo)
		{
			Log.Warning("Car {car} already has bardo: {bardo}", car, car.Bardo);
			return;
		}
		if (car.IsInBardo)
		{
			Log.Warning("Car {car} change bardo: {old} to {new}", car, car.Bardo, bardo);
			car.Bardo = bardo;
			return;
		}
		if (car == SelectedCar)
		{
			SelectedCar = null;
		}
		WillRemoveCar(car, isMovingToBardo: true);
		car.SetVisible(visible: false);
		car.Bardo = bardo;
	}

	public float RelativeVelocity(Car a, Car b)
	{
		Graph.PositionDirection positionDirection = graph.GetPositionDirection(a.LocationF);
		Graph.PositionDirection positionDirection2 = graph.GetPositionDirection(b.LocationF);
		Vector3 vector = a.velocity * positionDirection.Direction;
		Vector3 vector2 = b.velocity * positionDirection2.Direction;
		Vector3 lhs = positionDirection2.Position - positionDirection.Position;
		Vector3 rhs = vector2 - vector;
		float num = Vector3.Dot(lhs, rhs);
		float magnitude = lhs.magnitude;
		return num / magnitude;
	}

	public bool StoppingDistanceIfMovingToward(Car a, Car b, out float distance, out float stoppingDistanceForA)
	{
		Graph.PositionDirection positionDirection = graph.GetPositionDirection(a.LocationF);
		Graph.PositionDirection positionDirection2 = graph.GetPositionDirection(b.LocationF);
		Graph.PositionDirection positionDirection3 = graph.GetPositionDirection(a.LocationR);
		Graph.PositionDirection positionDirection4 = graph.GetPositionDirection(b.LocationR);
		Graph.PositionDirection positionDirection5 = ((Vector3.SqrMagnitude(positionDirection.Position - positionDirection2.Position) < Vector3.SqrMagnitude(positionDirection3.Position - positionDirection2.Position)) ? positionDirection : positionDirection3);
		Graph.PositionDirection positionDirection6 = ((Vector3.SqrMagnitude(positionDirection2.Position - positionDirection.Position) < Vector3.SqrMagnitude(positionDirection4.Position - positionDirection.Position)) ? positionDirection2 : positionDirection4);
		Vector3 position = positionDirection5.Position;
		Vector3 position2 = positionDirection6.Position;
		distance = Vector3.Distance(position, position2);
		Vector3 vector = a.velocity * positionDirection5.Direction;
		Vector3 vector2 = b.velocity * positionDirection6.Direction;
		Vector3.Dot(vector, vector2);
		if (Vector3.Distance(position + vector * 0.01f, position2 + vector2 * 0.01f) < distance)
		{
			Vector3 normalized = (position2 - position).normalized;
			float positionAScalar = Vector3.Dot(position, normalized);
			float positionBScalar = Vector3.Dot(position2, normalized);
			float velocityAScalar = Vector3.Dot(vector, normalized);
			float velocityBScalar = Vector3.Dot(vector2, normalized);
			return FindStoppingDistance(positionAScalar, positionBScalar, velocityAScalar, velocityBScalar, out stoppingDistanceForA);
		}
		stoppingDistanceForA = 0f;
		return false;
	}

	private static bool FindStoppingDistance(float positionAScalar, float positionBScalar, float velocityAScalar, float velocityBScalar, out float stoppingDistanceForA)
	{
		stoppingDistanceForA = 0f;
		float num = velocityAScalar - velocityBScalar;
		float num2 = positionBScalar - positionAScalar;
		if (Mathf.Approximately(num, 0f))
		{
			return false;
		}
		float num3 = num2 / num;
		if (num3 < 0f)
		{
			return false;
		}
		stoppingDistanceForA = Mathf.Abs(velocityAScalar * num3);
		return true;
	}

	public void HandleRequestOilCar(string carId, float amount)
	{
		if (TryGetCarForId(carId, out var car) && !(car.VelocityMphAbs > 5f))
		{
			amount = Mathf.Clamp01(amount);
			car.OffsetOiled(amount);
		}
	}

	internal static IEnumerator WaitFixed(float seconds)
	{
		if (_waitForFixedUpdateCached == null)
		{
			_waitForFixedUpdateCached = new WaitForFixedUpdate();
		}
		float t0 = Time.fixedTime;
		do
		{
			yield return _waitForFixedUpdateCached;
		}
		while (Time.fixedTime - t0 < seconds);
	}
}
