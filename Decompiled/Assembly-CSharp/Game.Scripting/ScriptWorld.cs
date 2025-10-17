using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cameras;
using Character;
using Game.Messages;
using Game.Notices;
using Game.Progression;
using Game.State;
using Helpers;
using KeyValue.Runtime;
using Model;
using Model.Database;
using Model.Definition;
using Model.Definition.Data;
using Model.Ops;
using MoonSharp.Interpreter;
using Network;
using RollingStock;
using Serilog;
using Track;
using Track.Search;
using Track.Signals;
using UI;
using UI.CarInspector;
using UI.CompanyWindow;
using UnityEngine;

namespace Game.Scripting;

public class ScriptWorld
{
	private static ScriptWorld _shared;

	public static ScriptWorld Shared => _shared ?? (_shared = new ScriptWorld());

	private static Graph Graph => Graph.Shared;

	private static TrainController TrainController => TrainController.Shared;

	public static float timeScale
	{
		get
		{
			return Time.timeScale;
		}
		set
		{
			Time.timeScale = value;
		}
	}

	public static float time => Time.time;

	public static void say(string message)
	{
		Multiplayer.Broadcast(message);
	}

	public static void set_feature_enabled(string feature, bool enabled)
	{
		MapFeatureManager.Shared.SetFeatureEnabled(feature, enabled);
	}

	public static void set_property(string objectId, string key, DynValue dynVale)
	{
		IPropertyValue value = PropertyValueConverter.RuntimeToSnapshot(ScriptProperties.ToValue(dynVale));
		StateManager.ApplyLocal(new PropertyChange(objectId, key, value));
	}

	public static DynValue get_property(string objectId, string key)
	{
		IKeyValueObject keyValueObject = StateManager.Shared.KeyValueObjectForId(objectId);
		if (keyValueObject == null)
		{
			Log.Warning("getProperty: object {objectId} not found", objectId);
			return DynValue.Nil;
		}
		return ScriptProperties.FromValue(keyValueObject[key]);
	}

	public static void set_signal_system(string command)
	{
		MapFeatureManager shared = MapFeatureManager.Shared;
		switch (command)
		{
		case "off":
			shared.SetFeatureEnabled("signals", unlocked: false);
			break;
		case "ctc":
			shared.SetFeatureEnabled("signals", unlocked: true);
			StateManager.ApplyLocal(new PropertyChange("ctc", "mode", new IntPropertyValue(1)));
			break;
		case "abs":
			shared.SetFeatureEnabled("signals", unlocked: true);
			StateManager.ApplyLocal(new PropertyChange("ctc", "mode", new IntPropertyValue(0)));
			break;
		default:
			throw new ArgumentOutOfRangeException("command", command, null);
		}
	}

	public static void code_ctc_route(string interlockingId, string nr, string lr)
	{
		SwitchSetting switchSetting = ((!(nr.ToUpper() == "N")) ? SwitchSetting.Reversed : SwitchSetting.Normal);
		string text = lr.ToUpper();
		SignalDirection signalDirection = ((text == "L") ? SignalDirection.Left : ((text == "R") ? SignalDirection.Right : SignalDirection.None));
		SignalDirection direction = signalDirection;
		CTCPanelController shared = CTCPanelController.Shared;
		if (shared == null)
		{
			throw new Exception("CTC panel controller not found");
		}
		shared.CodeSwitchAndSignal(interlockingId, switchSetting, direction);
	}

	public static bool is_block_occupied(string blockId)
	{
		CTCPanelController shared = CTCPanelController.Shared;
		if (shared == null)
		{
			throw new Exception("CTC panel controller not found");
		}
		CTCBlock cTCBlock = shared.BlockForId(blockId);
		if (cTCBlock == null)
		{
			throw new Exception("Block " + blockId + " not found");
		}
		return cTCBlock.IsOccupied;
	}

	public static void reset()
	{
		Time.timeScale = 1f;
		TrainController trainController = TrainController;
		Graph graph = trainController.graph;
		trainController.RemoveAllCars();
		foreach (TrackNode node in graph.Nodes)
		{
			if (node.isThrown)
			{
				node.isThrown = false;
			}
		}
		MapFeatureManager.Shared.HandleSnapshotProperties(new Dictionary<string, Value>(), SetValueOrigin.Local);
		NoticeManager.Shared.Clear();
		CTCPanelController shared = CTCPanelController.Shared;
		if (shared != null)
		{
			shared.ClearAllRoutes();
			shared.ClearAllBlocks();
			StateManager.ApplyLocal(new PropertyChange("ctc", "mode", new IntPropertyValue(0)));
		}
		foreach (PassengerStop item in PassengerStop.FindAll())
		{
			item.ClearAllWaiting();
		}
	}

	public static float get_distance(ScriptLocation a, ScriptLocation b)
	{
		if (a == null)
		{
			throw new ScriptRuntimeException("'a' location cannot be nil");
		}
		if (b == null)
		{
			throw new ScriptRuntimeException("'b' location cannot be nil");
		}
		Graph graph = Graph;
		if (graph.TryFindDistance(a.Location, b.Location, out var totalDistance, out var _))
		{
			return totalDistance;
		}
		Log.Warning("Couldn't find route, falling back to linear distance: {a} to {b}", a, b);
		return graph.GetDistanceBetweenClose(a.Location, b.Location);
	}

	public static ScriptCar car_at_location(ScriptLocation scriptLocation, float radius = 1f)
	{
		if (scriptLocation == null)
		{
			throw new ScriptRuntimeException("Null location");
		}
		Location location = scriptLocation.Location;
		Car car = TrainController.CheckForCarAtLocation(location, radius);
		if (!(car == null))
		{
			return car.ScriptCar();
		}
		return null;
	}

	public static ScriptCar find_car_from_location(ScriptLocation scriptLocation, float distance, List<ScriptCar> exceptCars = null)
	{
		if (scriptLocation == null)
		{
			throw new ScriptRuntimeException("Null location");
		}
		TrainController trainController = TrainController;
		Graph graph = trainController.graph;
		Location location = scriptLocation.Location;
		for (float num = 0f; num < distance; num += 2f)
		{
			Car car = trainController.CheckForCarAtLocation(location);
			if (car != null)
			{
				if (exceptCars == null)
				{
					return car.ScriptCar();
				}
				if (exceptCars.All((ScriptCar exceptCar) => exceptCar.id != car.id))
				{
					return car.ScriptCar();
				}
			}
			location = graph.LocationByMoving(location, 2f, checkSwitchAgainstMovement: false, stopAtEndOfTrack: true);
		}
		return null;
	}

	public static void jumpToIndustry(string industryId)
	{
		CameraSelector shared = CameraSelector.shared;
		Industry industry = GetOpsController().IndustryForId(industryId);
		if (industry == null)
		{
			throw new ScriptRuntimeException("Industry " + industryId + " not found");
		}
		shared.JumpToPoint(industry.transform.GamePosition(), Quaternion.identity, CameraSelector.CameraIdentifier.Strategy);
	}

	public static void jump_to_position(Table vectorObject)
	{
		CameraSelector shared = CameraSelector.shared;
		Vector3 gamePoint = ScriptVector3.FromTable(vectorObject);
		shared.JumpToPoint(gamePoint, Quaternion.identity);
	}

	public static string selected_camera()
	{
		return CameraSelector.shared.CurrentCameraIdentifier switch
		{
			CameraSelector.CameraIdentifier.Strategy => "overhead", 
			CameraSelector.CameraIdentifier.FirstPerson => "first-person", 
			_ => null, 
		};
	}

	public static object get_player_position()
	{
		return ScriptVector3.DictionaryRepresentation(CameraSelector.shared.character.GroundPosition.WorldToGame());
	}

	public static object get_camera_position()
	{
		return ScriptVector3.DictionaryRepresentation(CameraSelector.shared.CurrentCameraPosition);
	}

	public static void orient_toward(Table vectorObject)
	{
		CameraSelector shared = CameraSelector.shared;
		Vector3 vector = ScriptVector3.FromTable(vectorObject);
		Vector3 currentCameraPosition = shared.CurrentCameraPosition;
		shared.SelectCamera(CameraSelector.CameraIdentifier.FirstPerson);
		shared.StartCoroutine(Loop(shared.character, Quaternion.LookRotation(vector - currentCameraPosition, Vector3.up)));
		static IEnumerator Loop(PlayerController player, Quaternion targetRotation)
		{
			Quaternion startRotation = player.character.motor.TransientRotation;
			float timeRequired = Mathf.Max(1f, Quaternion.Angle(startRotation, targetRotation) / 90f);
			for (float elapsedTime = 0f; elapsedTime < timeRequired; elapsedTime += Time.fixedDeltaTime)
			{
				float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsedTime / timeRequired));
				Quaternion rotation = Quaternion.Slerp(startRotation, targetRotation, t);
				player.SetRotation(rotation);
				yield return new WaitForFixedUpdate();
			}
			player.SetRotation(targetRotation);
		}
	}

	public static List<ScriptCar> get_cars(string carTypeFilter)
	{
		IEnumerable<Car> source = TrainController.Cars;
		if (!string.IsNullOrEmpty(carTypeFilter))
		{
			CarTypeFilter filter = new CarTypeFilter(carTypeFilter);
			source = source.Where((Car c) => filter.Matches(c.CarType));
		}
		return source.Select((Car c) => c.ScriptCar()).ToList();
	}

	public static ScriptCar get_selected_car()
	{
		return TrainController.SelectedCar?.ScriptCar();
	}

	private static OpsController GetOpsController()
	{
		OpsController shared = OpsController.Shared;
		if (shared == null)
		{
			throw new ScriptRuntimeException("Ops controller not found");
		}
		return shared;
	}

	public static List<ScriptCar> place_train(ScriptLocation location, string[] identifiers)
	{
		StateManager.AssertIsHost();
		if (location == null || !location.Location.IsValid)
		{
			throw new ScriptRuntimeException("Null or invalid location");
		}
		Debug.Log(string.Format("PlaceTrain: {0}: {1}", location.Location, string.Join(", ", identifiers)));
		TrainController trainController = TrainController;
		IPrefabStore prefabStore = trainController.PrefabStore;
		List<CarDescriptor> list = new List<CarDescriptor>();
		foreach (string identifier in identifiers)
		{
			TypedContainerItem<CarDefinition> typedContainerItem = prefabStore.CarDefinitionInfoForIdentifier(identifier);
			list.Add(new CarDescriptor(typedContainerItem));
			if (typedContainerItem.Definition.TryGetTenderIdentifier(out var tenderIdentifier))
			{
				list.Add(new CarDescriptor(prefabStore.CarDefinitionInfoForIdentifier(tenderIdentifier)));
			}
		}
		try
		{
			trainController.PlaceTrain(location.Location, list);
		}
		catch (Exception ex)
		{
			throw new ScriptRuntimeException(ex);
		}
		return (trainController.LastPlacedTrain ?? throw new ScriptRuntimeException("No cars placed")).Select((Car car) => car.ScriptCar()).ToList();
	}

	public static List<ScriptCar> place_train_at_interchange(string interchangeId, List<string> identifiers, List<string> carIds)
	{
		StateManager.AssertIsHost();
		TrainController trainController = TrainController;
		IPrefabStore prefabStore = trainController.PrefabStore;
		Interchange interchange = GetOpsController().EnabledInterchanges.First((Interchange i) => i.Industry.identifier == interchangeId);
		List<CarDescriptor> list = new List<CarDescriptor>();
		List<string> list2 = ((carIds != null) ? new List<string>() : null);
		for (int num = 0; num < identifiers.Count; num++)
		{
			string identifier = identifiers[num];
			TypedContainerItem<CarDefinition> typedContainerItem = prefabStore.CarDefinitionInfoForIdentifier(identifier);
			list.Add(new CarDescriptor(typedContainerItem));
			if (carIds != null && carIds[num] != null)
			{
				list2.Add(carIds[num]);
			}
			if (typedContainerItem.Definition.TryGetTenderIdentifier(out var tenderIdentifier) && ((num < identifiers.Count - 1 && identifiers[num + 1] != tenderIdentifier) || num == identifiers.Count - 1))
			{
				list.Add(new CarDescriptor(prefabStore.CarDefinitionInfoForIdentifier(tenderIdentifier)));
				if (carIds != null && carIds[num] != null)
				{
					list2.Add(carIds[num] + "T");
				}
			}
		}
		List<TrackSpan> spans = interchange.trackSpans.ToList();
		if (!trainController.PlaceTrain(spans, list, list2))
		{
			throw new ScriptRuntimeException("Couldn't find room for cars");
		}
		return (from id in carIds
			select trainController.CarForId(id) into car
			select car.ScriptCar()).ToList();
	}

	public static ScriptLocation get_marker_location(string markerId)
	{
		TrackMarker trackMarker = Graph.MarkerForId(markerId);
		if (trackMarker != null)
		{
			Location? location = trackMarker.Location;
			if (location.HasValue)
			{
				Location valueOrDefault = location.GetValueOrDefault();
				return new ScriptLocation(valueOrDefault);
			}
		}
		TrackMarker[] array = UnityEngine.Object.FindObjectsOfType<TrackMarker>(includeInactive: true);
		foreach (TrackMarker trackMarker2 in array)
		{
			if (!(trackMarker2.id != markerId))
			{
				Location? location = trackMarker2.Location;
				if (location.HasValue)
				{
					Location valueOrDefault2 = location.GetValueOrDefault();
					return new ScriptLocation(valueOrDefault2);
				}
				return null;
			}
		}
		return null;
	}

	public static void set_switch_thrown(string switchId, bool thrown)
	{
		if (!TrainController.TrySetSwitch(switchId, thrown, "", out var errorMessage))
		{
			throw new ScriptRuntimeException(errorMessage);
		}
	}

	public static bool get_switch_thrown(string switchId)
	{
		TrackNode node = Graph.GetNode(switchId);
		if (node == null)
		{
			throw new ScriptRuntimeException("Switch " + switchId + " not found");
		}
		return node.isThrown;
	}

	public static object get_switch_position(string switchId)
	{
		TrackNode node = Graph.GetNode(switchId);
		if (node == null)
		{
			return null;
		}
		return ScriptVector3.DictionaryRepresentation(node.transform.GamePosition());
	}

	public static bool check_same_route(ScriptLocation a, ScriptLocation b, float limit)
	{
		return Graph.CheckSameRoute(a.Location, b.Location, limit);
	}

	public static ScriptPassengerStop get_passenger_stop(string stationId)
	{
		PassengerStop passengerStop = PassengerStop.FindAll().FirstOrDefault((PassengerStop ps) => ps.identifier == stationId);
		if (passengerStop == null)
		{
			throw new ScriptRuntimeException("Passenger stop not found");
		}
		return new ScriptPassengerStop(passengerStop);
	}

	public static bool property_equals(string objectId, string key, DynValue expectedDynValue)
	{
		return ScriptProperties.ToValue(expectedDynValue).Equals(StateManager.Shared.KeyValueObjectForId(objectId)[key]);
	}

	public static ScriptCar get_inspector_car()
	{
		return CarInspector.ShownCar()?.ScriptCar();
	}

	public static string get_company_window_path()
	{
		return CompanyWindow.Shared?.ShownPath;
	}

	public static Dictionary<string, int> get_industry_next_contract_tiers()
	{
		return GetOpsController().AllIndustries.Where((Industry i) => !i.ProgressionDisabled && i.NextContract.HasValue).ToDictionary((Industry i) => i.identifier, (Industry i) => i.NextContract?.Tier ?? 0);
	}

	public static object get_mouse_look()
	{
		MouseLookInput component = CameraSelector.shared.character.GetComponent<MouseLookInput>();
		return ScriptVector3.DictionaryRepresentation(new Vector2(component.Yaw, component.Pitch));
	}

	public static float get_field_of_view()
	{
		Camera main = Camera.main;
		if (main == null)
		{
			return 0f;
		}
		return main.fieldOfView;
	}

	public static void reset_movement_counter()
	{
		GameInput shared = GameInput.shared;
		shared.EnableMovementCounters = true;
		shared.MovementCounter = Vector4.zero;
	}

	public static float get_movement_counter(string direction)
	{
		Vector4 movementCounter = GameInput.shared.MovementCounter;
		return direction switch
		{
			"forward" => movementCounter.x, 
			"back" => movementCounter.y, 
			"left" => movementCounter.z, 
			"right" => movementCounter.w, 
			_ => 0f, 
		};
	}

	public static bool get_movement_jumped()
	{
		return GameInput.shared.MovementJumped;
	}

	private static bool TryGetSeatedCar(out Car car, out Seat seat)
	{
		PlayerController character = CameraSelector.shared.character;
		if (character == null || !character.character.IsSeated)
		{
			car = null;
			seat = null;
			return false;
		}
		seat = character.character.Seat;
		car = seat.GetComponentInParent<Car>();
		if (car != null)
		{
			return true;
		}
		seat = null;
		return false;
	}

	public static ScriptCar get_seated_car()
	{
		if (!TryGetSeatedCar(out var car, out var _))
		{
			return null;
		}
		return new ScriptCar(car);
	}

	public static ScriptCar get_seated_locomotive()
	{
		if (!TryGetSeatedCar(out var car, out var _))
		{
			return null;
		}
		if (!car.IsLocomotive)
		{
			return null;
		}
		return car.ScriptCar();
	}

	public static ScriptCar get_attached_locomotive()
	{
		PlayerController character = CameraSelector.shared.character;
		if (character == null)
		{
			return null;
		}
		Car relativeCar = character.GetRelativeCar();
		if (relativeCar != null && relativeCar.IsLocomotive)
		{
			return relativeCar.ScriptCar();
		}
		return null;
	}

	public static bool get_seated_engineer()
	{
		if (!TryGetSeatedCar(out var car, out var seat))
		{
			return false;
		}
		if (!car.IsLocomotive)
		{
			return false;
		}
		return (from s in car.transform.GetComponentsInChildren<Seat>()
			orderby s.priority
			select s).ToList().IndexOf(seat) == 0;
	}
}
