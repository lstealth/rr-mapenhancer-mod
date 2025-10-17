using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Audio;
using Avatar;
using Cameras;
using Character;
using Core;
using Enviro;
using GalaSoft.MvvmLight.Messaging;
using Game;
using Game.Events;
using Game.Messages;
using Game.State;
using Helpers;
using JetBrains.Annotations;
using Map.Runtime;
using Model;
using Model.Ops;
using RollingStock;
using Serilog;
using Track;
using Track.Signals;
using UI;
using UI.Common;
using UnityEngine;

public class CameraSelector : MonoBehaviour
{
	public enum CameraIdentifier
	{
		FirstPerson,
		Strategy,
		Dispatcher
	}

	private enum TrainFollowPosition
	{
		Head,
		Tail
	}

	private class SeatJumpState
	{
		public string CarId;

		public int Index;
	}

	public PlayerController character;

	public ICameraSelectable dispatcher;

	public DroneController drone;

	public StrategyCameraController strategyCamera;

	[CanBeNull]
	private Camera _camera;

	[CanBeNull]
	private ICameraSelectable _currentCamera;

	private CameraIdentifier _cameraIdentifier = CameraIdentifier.Strategy;

	private Vector3 _lastCameraPosition;

	private float _lastDoppler;

	private TrainController _trainController;

	private Coroutine _followTrackCoroutine;

	private Vector3 _lastSentCameraPosition;

	private float _lastSentCameraPositionTime;

	private SeatJumpState _seatJumpState = new SeatJumpState();

	private PositionRotation? _defaultSpawn;

	private (JumpTarget, CameraIdentifier?)? _pendingJump;

	public static CameraSelector shared { get; private set; }

	public Vector3 CurrentCameraPosition
	{
		get
		{
			if (_currentCamera != null)
			{
				return WorldTransformer.WorldToGame(_currentCamera.CameraContainer.position);
			}
			return Vector3.zero;
		}
	}

	public Vector3 CurrentCameraGroundPosition => _currentCamera?.GroundPosition ?? Vector3.zero;

	public CameraIdentifier CurrentCameraIdentifier => _cameraIdentifier;

	private StrategyCameraController SelectedStrategyCamera => _currentCamera?.gameObject.GetComponent<StrategyCameraController>();

	public bool CurrentCameraIsFirstPerson => _cameraIdentifier == CameraIdentifier.FirstPerson;

	public LocalAvatar localAvatar { get; private set; }

	public PositionRotation DefaultSpawn
	{
		get
		{
			if (!_defaultSpawn.HasValue)
			{
				SpawnPoint spawnPoint = SpawnPoint.Default;
				_defaultSpawn = new PositionRotation(WorldTransformer.WorldToGame(spawnPoint.transform.position), spawnPoint.transform.rotation);
			}
			return _defaultSpawn.Value;
		}
		set
		{
			_defaultSpawn = value;
		}
	}

	private void Awake()
	{
		shared = this;
		_trainController = TrainController.Shared;
		localAvatar = base.gameObject.AddComponent<LocalAvatar>();
		localAvatar.character = character;
	}

	private void OnEnable()
	{
		Messenger.Default.Register<MapDidLoadEvent>(this, HandleMapDidLoad);
		StartCoroutine(PostAwake());
	}

	private void OnDisable()
	{
		Messenger.Default.Unregister(this);
	}

	private void Update()
	{
		if (MainCameraHelper.TryGetIfNeeded(ref _camera))
		{
			if (GameInput.MovementInputEnabled)
			{
				InputChangeCamera();
				InputJumpCamera();
			}
			SendCameraPositionIfNeeded();
		}
	}

	private void FixedUpdate()
	{
		if (MainCameraHelper.TryGetIfNeeded(ref _camera))
		{
			UpdateDopplerForCameraMovement();
		}
	}

	private void SendCameraPositionIfNeeded()
	{
		Vector3 vector = WorldTransformer.WorldToGame(_camera.transform.position);
		float time = Time.time;
		float num = time - _lastSentCameraPositionTime;
		if ((Vector3.Distance(vector, _lastSentCameraPosition) > 50f || num > 10f) && PlayersManager.PlayerId.IsValid)
		{
			StateManager.ApplyLocal(new UpdateCameraPosition(vector));
			_lastSentCameraPosition = vector;
			_lastSentCameraPositionTime = time;
		}
	}

	private void HandleMapDidLoad(MapDidLoadEvent evt)
	{
		(MotionSnapshot, Car) relativePositionRotation = character.GetRelativePositionRotation();
		var (motionSnapshot, _) = relativePositionRotation;
		if (relativePositionRotation.Item2 == null && motionSnapshot.Position == Vector3.zero)
		{
			JumpToSpawn();
		}
	}

	private IEnumerator PostAwake()
	{
		yield return new WaitForEndOfFrame();
		ProgressIndicator.DrawCurtains();
		ICameraSelectable[] array = new ICameraSelectable[3] { character, drone, strategyCamera };
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetSelected(selected: false, null);
		}
		while (!MainCameraHelper.TryGetIfNeeded(ref _camera))
		{
			yield return null;
		}
		UpdateCurrentCamera();
	}

	private static void InputGameSpeed()
	{
	}

	private void InputChangeCamera()
	{
		CameraIdentifier cameraIdentifier = _cameraIdentifier;
		GameInput gameInput = GameInput.shared;
		if (gameInput.CameraSelectFirstPerson)
		{
			cameraIdentifier = CameraIdentifier.FirstPerson;
		}
		if (gameInput.CameraSelectStrategy)
		{
			cameraIdentifier = CameraIdentifier.Strategy;
		}
		if (gameInput.CameraSelectDispatcher)
		{
			cameraIdentifier = CameraIdentifier.Dispatcher;
		}
		if (gameInput.CameraJumpStrategyToAvatar)
		{
			cameraIdentifier = CameraIdentifier.Strategy;
			MoveStrategyToPoint(localAvatar.character.GroundPosition, localAvatar.character.character.motor.TransientRotation);
		}
		if (SelectCamera(cameraIdentifier))
		{
			CameraJumped();
		}
	}

	private void CameraJumped()
	{
		if (WorldTransformer.TryGetShared(out var worldTransformer))
		{
			worldTransformer.MoveNow();
		}
	}

	internal bool SelectCamera(CameraIdentifier cameraIdentifier)
	{
		if (_cameraIdentifier == cameraIdentifier)
		{
			return false;
		}
		Log.Debug("Change to {cameraIdentifier}", cameraIdentifier);
		_cameraIdentifier = cameraIdentifier;
		UpdateCurrentCamera();
		return true;
	}

	private void InputJumpCamera()
	{
		GameInput gameInput = GameInput.shared;
		if (gameInput.CameraJumpToSeat)
		{
			JumpToSeat();
		}
		else if (gameInput.CameraJumpToHead)
		{
			JumpToCarRelative(TrainFollowPosition.Head);
		}
		else if (gameInput.CameraFollowHead)
		{
			JumpToCar(TrainFollowPosition.Head);
		}
		if (gameInput.CameraJumpToTail)
		{
			JumpToCarRelative(TrainFollowPosition.Tail);
		}
		else if (gameInput.CameraFollowTail)
		{
			JumpToCar(TrainFollowPosition.Tail);
		}
		if (gameInput.Teleport)
		{
			TeleportToMouse();
		}
		if (gameInput.PlaceFlare)
		{
			FlareManager.Shared.PlaceFlare(_camera);
		}
	}

	private void JumpToCarRelative(TrainFollowPosition toward)
	{
		StrategyCameraController selectedStrategyCamera = SelectedStrategyCamera;
		if (!selectedStrategyCamera)
		{
			return;
		}
		Car selectedCar = _trainController.SelectedCar;
		if (selectedCar == null)
		{
			return;
		}
		Car.LogicalEnd logicalEnd = LogicalEndForEnumerationFrom(selectedCar);
		List<Car> list = selectedCar.EnumerateCoupled(logicalEnd).ToList();
		if (logicalEnd == Car.LogicalEnd.B)
		{
			toward = ((toward == TrainFollowPosition.Head) ? TrainFollowPosition.Tail : TrainFollowPosition.Head);
		}
		if (!list.Contains(selectedStrategyCamera.FollowCar))
		{
			return;
		}
		int num = list.IndexOf(selectedStrategyCamera.FollowCar);
		switch (toward)
		{
		case TrainFollowPosition.Head:
			if (num != 0)
			{
				selectedStrategyCamera.FollowCar = list[num - 1];
			}
			break;
		case TrainFollowPosition.Tail:
			if (num < list.Count - 1)
			{
				selectedStrategyCamera.FollowCar = list[num + 1];
			}
			break;
		default:
			throw new ArgumentOutOfRangeException("toward", toward, null);
		}
	}

	[CanBeNull]
	private Car CarForPosition(TrainFollowPosition position)
	{
		Car selectedCar = _trainController.SelectedCar;
		if (selectedCar == null || selectedCar.set == null)
		{
			return selectedCar;
		}
		Car.LogicalEnd fromEnd = LogicalEndForEnumerationFrom(selectedCar);
		List<Car> list = selectedCar.EnumerateCoupled(fromEnd).ToList();
		return list[((position == TrainFollowPosition.Head) ? ((Index)0) : (^1)).GetOffset(list.Count)];
	}

	private static Car.LogicalEnd LogicalEndForEnumerationFrom(Car selectedCar)
	{
		if ((selectedCar.set.IndexOfCar(selectedCar) ?? 0) >= selectedCar.set.NumberOfCars / 2)
		{
			return Car.LogicalEnd.B;
		}
		return Car.LogicalEnd.A;
	}

	private void JumpToCar(TrainFollowPosition position)
	{
		SelectCamera(CameraIdentifier.Strategy);
		Car car = CarForPosition(position);
		if (!(car == null))
		{
			strategyCamera.JumpToCar(car);
		}
	}

	public void JumpCharacterTo(Vector3 position, string relativeToCarId, Vector3 look)
	{
		Log.Debug("JumpCharacterTo: {position}, {relativeToCarId}, {look}", position, relativeToCarId, look);
		if (string.IsNullOrEmpty(relativeToCarId))
		{
			JumpCharacterTo(new JumpTarget(position, Quaternion.LookRotation(look)));
			return;
		}
		Car car = _trainController.CarForId(relativeToCarId);
		if (car == null)
		{
			throw new ArgumentException("No such car " + relativeToCarId, "relativeToCarId");
		}
		JumpToCar(car, position, Quaternion.LookRotation(look));
	}

	public void JumpToCar(Car car, Vector3 relativePosition, Quaternion relativeRotation)
	{
		StartCoroutine(_JumpToCar(car, relativePosition, relativeRotation));
	}

	private IEnumerator _JumpToCar(Car car, Vector3 relativePosition, Quaternion relativeRotation)
	{
		IDisposable loadToken = car.ModelLoadRetain("Jump");
		yield return car.WaitForLoaded();
		yield return new WaitForFixedUpdate();
		JumpCharacterTo(new JumpTarget(relativePosition, relativeRotation, car));
		loadToken.Dispose();
	}

	private void JumpToSeat()
	{
		Car selectedCar = _trainController.SelectedCar;
		if (selectedCar == null)
		{
			Console.Log("No selected car.");
			return;
		}
		SelectCamera(CameraIdentifier.FirstPerson);
		Seat seat = FindBestSeat(selectedCar, character.character.Seat);
		if (seat != null)
		{
			character.JumpTo(seat.FootPosition, seat.transform.rotation);
			character.Sit(seat);
		}
		else
		{
			character.JumpToCar(selectedCar);
		}
	}

	private Seat FindBestSeat(Car car, [CanBeNull] Seat currentSeat)
	{
		AvatarManager avatarManager = AvatarManager.Instance;
		List<Seat> list = (from seat in car.transform.GetComponentsInChildren<Seat>()
			where !avatarManager.RemoteAvatarNear(seat.transform.position)
			orderby seat.priority
			select seat).ToList();
		if (list.Count == 0)
		{
			return null;
		}
		if (car.id != _seatJumpState.CarId || currentSeat == null)
		{
			_seatJumpState.Index = 0;
			_seatJumpState.CarId = car.id;
		}
		else
		{
			if (currentSeat != null)
			{
				int index = list.IndexOf(currentSeat);
				_seatJumpState.Index = index;
			}
			_seatJumpState.Index = (_seatJumpState.Index + 1) % list.Count;
		}
		return list[_seatJumpState.Index];
	}

	private void TeleportToMouse()
	{
		if (!GameInput.IsMouseOverGameWindow() || _camera == null)
		{
			return;
		}
		Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
		if (Physics.Raycast(ray, out var hitInfo, 2f, 1 << Layers.Clickable))
		{
			CTCPanelGroup componentInParent = hitInfo.transform.GetComponentInParent<CTCPanelGroup>();
			if (componentInParent != null && componentInParent.switches.Count > 0)
			{
				Transform transform = componentInParent.switches.First().transform;
				JumpToPoint(WorldTransformer.WorldToGame(transform.position + 2f * (transform.rotation * Vector3.right)), transform.rotation, CameraIdentifier.Strategy);
				return;
			}
		}
		if (Physics.Raycast(ray, out var hitInfo2, 2000f, 1 << Layers.Terrain))
		{
			character.JumpTo(hitInfo2.point, Quaternion.LookRotation(ray.direction));
			LeanTween.delayedCall(0.1f, (Action)delegate
			{
				SelectCamera(CameraIdentifier.FirstPerson);
			});
		}
		else
		{
			Toast.Present("Nothing in range.");
		}
	}

	public void JumpToSpawn()
	{
		PositionRotation defaultSpawn = DefaultSpawn;
		SelectedStrategyCamera.JumpTo(defaultSpawn.Position, defaultSpawn.Rotation);
		JumpCharacterTo(new JumpTarget(defaultSpawn.Position, defaultSpawn.Rotation, 2f));
	}

	private void UpdateCurrentCamera()
	{
		ICameraSelectable cameraSelectable = _cameraIdentifier switch
		{
			CameraIdentifier.FirstPerson => character, 
			CameraIdentifier.Strategy => strategyCamera, 
			CameraIdentifier.Dispatcher => dispatcher, 
			_ => throw new ArgumentOutOfRangeException("_cameraIdentifier", $"Unexpected: {_cameraIdentifier}"), 
		};
		if (cameraSelectable != _currentCamera)
		{
			if (_currentCamera != null)
			{
				_currentCamera.SetSelected(selected: false, null);
			}
			_currentCamera = cameraSelectable;
			Transform cameraContainer = _currentCamera.CameraContainer;
			_camera.transform.SetParent(cameraContainer, worldPositionStays: false);
			_currentCamera.SetSelected(selected: true, _camera);
			localAvatar.CurrentCameraDidChange();
		}
	}

	private static string NameForCamera(CameraIdentifier identifier)
	{
		return identifier switch
		{
			CameraIdentifier.FirstPerson => "First Person", 
			CameraIdentifier.Strategy => "Tracking", 
			_ => throw new ArgumentOutOfRangeException("identifier", identifier, null), 
		};
	}

	public void ZoomToCar(Car target, bool select = true)
	{
		if (select)
		{
			SelectCamera(CameraIdentifier.Strategy);
		}
		strategyCamera.JumpToCar(target);
	}

	public void ZoomToTransform(Transform target)
	{
		SelectCamera(CameraIdentifier.Strategy);
		SelectedStrategyCamera.JumpTo(WorldTransformer.WorldToGame(target.position), target.rotation);
	}

	public void ZoomToPoint(Vector3 position)
	{
		SelectCamera(CameraIdentifier.Strategy);
		SelectedStrategyCamera.JumpTo(position);
	}

	public void FollowCar(Car car)
	{
		ZoomToCar(car);
		SelectedStrategyCamera.FollowCar = car;
	}

	public void JumpCharacterTo(JumpTarget jumpTarget)
	{
		Log.Debug("JumpCharacterTo {descriptor}", jumpTarget);
		StartCoroutine(_JumpToPoint(jumpTarget, CameraIdentifier.FirstPerson));
	}

	public void MoveStrategyToPoint(Vector3 worldPosition, Quaternion? rotation = null)
	{
		strategyCamera.JumpTo(WorldTransformer.WorldToGame(worldPosition), rotation);
	}

	public void JumpToPoint(Vector3 gamePoint, Quaternion rotation, CameraIdentifier? cameraIdentifier = null)
	{
		Log.Debug("JumpToPoint {gamePoint} {rotation} {cameraIdentifier}", gamePoint, rotation.eulerAngles, cameraIdentifier);
		CameraIdentifier cameraIdentifier2 = cameraIdentifier switch
		{
			null => (_cameraIdentifier == CameraIdentifier.Dispatcher) ? CameraIdentifier.Strategy : _cameraIdentifier, 
			CameraIdentifier.Dispatcher => throw new ArgumentException("Unsupported: Dispatcher"), 
			CameraIdentifier.Strategy => CameraIdentifier.Strategy, 
			CameraIdentifier.FirstPerson => CameraIdentifier.FirstPerson, 
			_ => throw new ArgumentException($"Unexpected camera identifier {cameraIdentifier}"), 
		};
		StartCoroutine(_JumpToPoint(new JumpTarget(gamePoint, rotation), cameraIdentifier2));
	}

	private bool PendingJumpEquals(JumpTarget jumpTarget, CameraIdentifier? cam)
	{
		if (!_pendingJump.HasValue)
		{
			return false;
		}
		(JumpTarget, CameraIdentifier?) value = _pendingJump.Value;
		if (jumpTarget.Equals(value.Item1))
		{
			return cam == value.Item2;
		}
		return false;
	}

	private bool ResolveJumpTarget(JumpTarget jumpTarget, out Vector3 worldPosition, out Quaternion rotation)
	{
		if (jumpTarget.IsRelativeToCar)
		{
			if (jumpTarget.RelativeToCar == null)
			{
				Log.Error("Couldn't resolve jump descriptor: car has been deallocated");
				worldPosition = Vector3.zero;
				rotation = Quaternion.identity;
				return false;
			}
			Car.MotionSnapshot motionSnapshot = jumpTarget.RelativeToCar.GetMotionSnapshot();
			worldPosition = motionSnapshot.Rotation * jumpTarget.Position + motionSnapshot.Position;
			rotation = motionSnapshot.Rotation * jumpTarget.Rotation;
		}
		else
		{
			worldPosition = jumpTarget.Position.GameToWorld();
			rotation = jumpTarget.Rotation;
		}
		if (jumpTarget.RandomRadius > 0.001f)
		{
			worldPosition += new Vector3(UnityEngine.Random.Range(0f - jumpTarget.RandomRadius, jumpTarget.RandomRadius), 0f, UnityEngine.Random.Range(0f - jumpTarget.RandomRadius, jumpTarget.RandomRadius));
		}
		return true;
	}

	private IEnumerator _JumpToPoint(JumpTarget jumpTarget, CameraIdentifier cameraIdentifier)
	{
		if (PendingJumpEquals(jumpTarget, cameraIdentifier))
		{
			yield break;
		}
		_pendingJump = (jumpTarget, cameraIdentifier);
		ProgressIndicator.Show("Loading...", 0.18f);
		float t0 = Time.time;
		ResolveJumpTarget(jumpTarget, out var worldPosition, out var _);
		yield return MapManager.Instance.RequestPriorityLoad(worldPosition.WorldToGame());
		float time = Time.time;
		if (!PendingJumpEquals(jumpTarget, cameraIdentifier))
		{
			yield break;
		}
		bool needsProgress = time - t0 > 0.15f;
		if (!needsProgress)
		{
			ProgressIndicator.Hide();
		}
		SelectCamera(cameraIdentifier);
		if (jumpTarget.IsRelativeToCar)
		{
			yield return new WaitForFixedUpdate();
		}
		ResolveJumpTarget(jumpTarget, out var worldPosition2, out var rotation2);
		switch (_cameraIdentifier)
		{
		case CameraIdentifier.FirstPerson:
		{
			character.character.motor.BaseVelocity = Vector3.zero;
			character.JumpTo(worldPosition2, rotation2);
			Car relativeToCar = jumpTarget.RelativeToCar;
			if (relativeToCar != null)
			{
				character.AttachTo(relativeToCar, worldPosition2, rotation2);
			}
			break;
		}
		case CameraIdentifier.Strategy:
			SelectedStrategyCamera.JumpTo(worldPosition2.WorldToGame());
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
		CameraJumped();
		if (needsProgress)
		{
			yield return new WaitForSecondsRealtime(0.5f);
			if (!PendingJumpEquals(jumpTarget, cameraIdentifier))
			{
				yield break;
			}
			ProgressIndicator.Hide();
		}
		EnviroManager.instance.Reflections.RenderGlobalReflectionProbe(forced: true);
		_pendingJump = null;
	}

	public void WillDestroyCar(Car car)
	{
		try
		{
			character.WillDestroyCar(car);
		}
		catch (Exception ex)
		{
			Debug.LogError(ex);
			Log.Error(ex, "WillDestroyCar: Exception for {car}", car);
		}
	}

	private void UpdateDopplerForCameraMovement()
	{
		Vector3 vector = _camera.transform.GamePosition();
		float deltaTime = Time.deltaTime;
		float time = Vector3.Distance(vector, _lastCameraPosition) / deltaTime;
		Config config = _trainController.config;
		float num = config.cameraVelocityToDoppler.Evaluate(time);
		float num2 = deltaTime * Mathf.Min(config.dopplerDeltaDecreasing, config.dopplerDeltaIncreasing);
		if (Mathf.Abs(_lastDoppler - num) >= num2)
		{
			bool flag = num > _lastDoppler;
			float maxDelta = deltaTime * (flag ? config.dopplerDeltaIncreasing : config.dopplerDeltaDecreasing);
			_lastDoppler = Mathf.MoveTowards(_lastDoppler, num, maxDelta);
			VirtualAudioSourcePool.SetGlobalDopplerLevel(_lastDoppler);
		}
		_lastCameraPosition = vector;
	}

	public void JumpTo(IIndustryTrackDisplayable passengerStop)
	{
		Log.Debug("Jump to {industry}", passengerStop);
		if (!passengerStop.TrackSpans.Any())
		{
			Log.Error("Industry has no track spans? {industry}", passengerStop);
		}
		else
		{
			ZoomToPoint(passengerStop.CenterPoint);
		}
	}

	internal void FollowTrack(Location location, int speed)
	{
		if (_followTrackCoroutine != null)
		{
			StopCoroutine(_followTrackCoroutine);
			_followTrackCoroutine = null;
		}
		if (speed > 0)
		{
			SelectCamera(CameraIdentifier.Strategy);
			_followTrackCoroutine = StartCoroutine(FollowTrackCoroutine(location, speed));
		}
	}

	private IEnumerator FollowTrackCoroutine(Location cursor, int speed)
	{
		while (true)
		{
			try
			{
				Location location = _trainController.graph.LocationByMoving(cursor, (float)speed * Time.deltaTime);
				cursor = location;
				strategyCamera.JumpTo(_trainController.graph.GetPosition(cursor));
			}
			catch
			{
				cursor = cursor.Flipped();
			}
			yield return null;
		}
	}

	public void SetCamera(CameraIdentifier identifier, ICameraSelectable cameraSelectable)
	{
		if (identifier == CameraIdentifier.Dispatcher)
		{
			dispatcher = cameraSelectable;
			return;
		}
		throw new ArgumentException($"Unsupported: {identifier}");
	}
}
