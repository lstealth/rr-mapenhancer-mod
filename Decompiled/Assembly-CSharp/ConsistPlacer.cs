using System;
using System.Collections.Generic;
using System.Linq;
using Game.Messages;
using Game.State;
using Helpers;
using JetBrains.Annotations;
using Model;
using RollingStock;
using Serilog;
using TMPro;
using Track;
using UI;
using UI.Common;
using UnityEngine;

public class ConsistPlacer : MonoBehaviour
{
	public GameObject panelObject;

	public TMP_Text label;

	private static ConsistPlacer _instance;

	private ConsistPlacerDidPlace _onDidPlace;

	private Camera _camera;

	private List<CarDescriptor> _descriptors;

	private List<string> _ids;

	private float _cutLength;

	private Location? _lastLocation;

	private GameObject _ghost;

	private readonly HashSet<IDisposable> _ghostLoadRequests = new HashSet<IDisposable>();

	private List<Car> _ghostCars;

	private bool _flipLocation;

	private static TrainController trainController => TrainController.Shared;

	private Graph Graph => Graph.Shared;

	private static bool CanMoveTrain => StateManager.Shared.GameMode == GameMode.Sandbox;

	public static ConsistPlacer Instance()
	{
		if ((bool)_instance)
		{
			return _instance;
		}
		_instance = UnityEngine.Object.FindObjectOfType(typeof(ConsistPlacer)) as ConsistPlacer;
		if (!_instance)
		{
			Debug.LogError("There needs to be one active ConsistEditor script on a GameObject in your scene.");
		}
		return _instance;
	}

	public void Present(IEnumerable<CarDescriptor> descriptors, [CanBeNull] List<string> ids = null, ConsistPlacerDidPlace onDidPlace = null)
	{
		_descriptors = descriptors.ToList();
		_ids = ids?.ToList();
		_onDidPlace = onDidPlace;
		_cutLength = TrainController.ApproximateLength(_descriptors);
		label.SetText($"Click track to place {_descriptors.Count} cars ~{_cutLength * 3.28084f:N0}ft\n<size=70%>Escape cancels</size>");
		panelObject.SetActive(value: true);
		base.enabled = true;
	}

	public static void MoveSelectedTrain()
	{
		if (!CanMoveTrain)
		{
			Toast.Present("Not available in this game mode.");
			return;
		}
		List<Car> list = trainController.SelectedTrain.ToList();
		if (list.Count != 0)
		{
			IEnumerable<CarDescriptor> descriptors = list.Select((Car car) => car.Descriptor());
			List<string> ids = list.Select((Car car) => car.id).ToList();
			Instance().Present(descriptors, ids);
		}
	}

	private void Dismiss()
	{
		base.enabled = false;
		panelObject.SetActive(value: false);
	}

	private void Awake()
	{
		base.enabled = false;
	}

	private void OnEnable()
	{
		GameInput.RegisterEscapeHandler(GameInput.EscapeHandler.Transient, delegate
		{
			CleanupGhostTrain();
			Dismiss();
			_onDidPlace?.Invoke(placed: false);
			return true;
		});
	}

	private void OnDisable()
	{
		CleanupGhostTrain();
		GameInput.UnregisterEscapeHandler(GameInput.EscapeHandler.Transient);
	}

	private void CleanupGhostTrain()
	{
		if (_ghost == null)
		{
			return;
		}
		foreach (IDisposable ghostLoadRequest in _ghostLoadRequests)
		{
			ghostLoadRequest.Dispose();
		}
		_ghostLoadRequests.Clear();
		try
		{
			_ghost.GetComponentInChildren<Car>().WillDestroy();
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Exception calling WillDestroy");
		}
		UnityEngine.Object.Destroy(_ghost);
		_ghost = null;
		_ghostCars = null;
	}

	private void Update()
	{
		Location? location = HitLocation();
		if (!location.HasValue)
		{
			CleanupGhostTrain();
			return;
		}
		Location value = location.Value;
		PlaceGhost(value);
		if (_ghost != null && Input.GetMouseButtonDown(0))
		{
			PlaceAt(value);
		}
	}

	private void Flip()
	{
		_flipLocation = !_flipLocation;
		_descriptors = _descriptors.Select(delegate(CarDescriptor desc)
		{
			desc.Flipped = !desc.Flipped;
			return desc;
		}).ToList();
		_descriptors.Reverse();
		CleanupGhostTrain();
	}

	private void PlaceGhost(Location loc)
	{
		if (loc.Equals(_lastLocation))
		{
			return;
		}
		Graph graph = Graph;
		Location location;
		try
		{
			location = graph.LocationByMoving(loc, 1f);
		}
		catch (Exception)
		{
			CleanupGhostTrain();
			return;
		}
		if (!trainController.CanPlaceAt(location, _cutLength + 1f))
		{
			CleanupGhostTrain();
			return;
		}
		try
		{
			CreateGhostIfNeeded();
			Location a = loc;
			foreach (Car ghostCar in _ghostCars)
			{
				a = ghostCar.PositionA(a, graph, MovementInfo.Zero, update: true);
				a = graph.LocationByMoving(a, -1f);
			}
		}
		catch (Exception)
		{
			CleanupGhostTrain();
			return;
		}
		_lastLocation = loc;
	}

	private void CreateGhostIfNeeded()
	{
		if (_ghost != null)
		{
			return;
		}
		_ghost = new GameObject("ConsistPlacer Ghost");
		_ghostCars = _descriptors.Select((CarDescriptor desc) => trainController.CreateCarRaw(desc, "00", ghost: true, _ghost.transform)).ToList();
		foreach (Car ghostCar in _ghostCars)
		{
			_ghostLoadRequests.Add(ghostCar.ModelLoadRetain("Ghost"));
			ghostCar.SetVisible(visible: true);
		}
	}

	private void PlaceAt(Location loc)
	{
		List<string> ids = _ids;
		PlaceTrainHandbrakes placeTrainHandbrakes = ((ids != null && ids.Count > 0) ? PlaceTrainHandbrakes.None : PlaceTrainHandbrakes.Automatic);
		Log.Debug("Place at {loc} with {handbrakes}", loc, placeTrainHandbrakes);
		CleanupGhostTrain();
		try
		{
			trainController.PlaceTrain(loc, _descriptors, _ids, 1f, placeTrainHandbrakes);
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Exception from PlaceTrain");
		}
		_lastLocation = null;
		_onDidPlace?.Invoke(placed: true);
		Dismiss();
	}

	private Location? HitLocation()
	{
		Graph graph = Graph;
		if (graph == null)
		{
			return null;
		}
		if (!MainCameraHelper.TryGetIfNeeded(ref _camera))
		{
			return null;
		}
		Location? location = graph.LocationFromMouse(_camera);
		if (!location.HasValue)
		{
			return null;
		}
		Location value = location.Value;
		if (_flipLocation)
		{
			value = value.Flipped();
		}
		return value;
	}
}
