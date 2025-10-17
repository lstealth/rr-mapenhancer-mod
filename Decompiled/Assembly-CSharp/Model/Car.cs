using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AssetPack.Common;
using AssetPack.Runtime;
using Audio;
using Core;
using Core.Diagnostics;
using Effects;
using GalaSoft.MvvmLight.Messaging;
using Game;
using Game.AccessControl;
using Game.Events;
using Game.Messages;
using Game.State;
using Helpers;
using JetBrains.Annotations;
using KeyValue.Runtime;
using Model.Database;
using Model.Definition;
using Model.Definition.Components;
using Model.Definition.Data;
using Model.Ops;
using Model.Ops.Definition;
using Model.Physics;
using RollingStock;
using RollingStock.Controls;
using Serilog;
using Track;
using UI;
using UI.Map;
using UI.Tags;
using Unity.Mathematics;
using UnityEngine;

namespace Model;

[SelectionBase]
public class Car : MonoBehaviour, IDefinitionReferenceResolver, IPropertyAccessControlDelegate
{
	public enum End
	{
		F,
		R
	}

	public enum LogicalEnd
	{
		A,
		B
	}

	private static class ModelLoadTaskKeys
	{
		public const string Model = "model";
	}

	public struct SetupPrefabs
	{
		public Coupler CouplerPrefab;

		public CutLever CutLeverPrefab;

		public Anglecock AnglecockPrefab;

		public AudioClip AirFlowAudioClip;

		public RollingProfile RollingProfile;

		public MapIcon LocomotiveMapIcon;
	}

	public enum SnapshotOption
	{
		None
	}

	public enum EndGearStateKey
	{
		IsCoupled,
		IsAirConnected,
		Anglecock,
		CutLever
	}

	public struct MotionSnapshot
	{
		public Vector3 Position;

		public Quaternion Rotation;

		public Vector3 Velocity;

		public MotionSnapshot(Vector3 position, Quaternion rotation, Vector3 velocity)
		{
			Position = position;
			Rotation = rotation;
			Velocity = velocity;
		}
	}

	private class CarModelLoadToken : IDisposable
	{
		private Car _car;

		private readonly string _tag;

		public CarModelLoadToken(Car car, string tag)
		{
			_car = car;
			_tag = tag;
		}

		public void Dispose()
		{
			_car.ModelLoadRelease(this);
			_car = null;
		}

		public override string ToString()
		{
			return _tag;
		}
	}

	public class EndGear
	{
		public Anglecock Anglecock;

		[CanBeNull]
		public Coupler Coupler;

		public CutLever CutLever;

		public bool IsCoupled;

		public bool IsAirConnected;

		public float AnglecockSetting;

		public float AirPressure;

		public bool NeedsConnectionUpdate;

		[CanBeNull]
		private EndGear _other;

		private bool _isPopulated;

		public bool IsAirConnectedAndOpen
		{
			get
			{
				if (IsAirConnected)
				{
					return IsAnglecockOpen;
				}
				return false;
			}
		}

		public bool IsAnglecockOpen => AnglecockSetting > 0.1f;

		public void SetConnectedTo([CanBeNull] EndGear other)
		{
			_other = other;
			if (_isPopulated)
			{
				if (other == null)
				{
					Anglecock.hose.SetConnectedTo(null);
				}
				else if (other._isPopulated)
				{
					Anglecock.hose.SetConnectedTo(IsAirConnected ? other.Anglecock.hose : null);
				}
				Anglecock.IsConnected = IsAirConnected;
			}
		}

		public void DidPopulate()
		{
			SetConnectedTo(_other);
			_other?.SetConnectedTo(this);
		}

		public void SetVisible(bool visible)
		{
			if (Coupler != null)
			{
				Coupler.SetVisible(visible);
			}
			if (Anglecock != null)
			{
				Anglecock.SetVisible(visible);
			}
		}

		public void Populate(Anglecock anglecockPrefab, Transform parent, Vector3 airHosePosition)
		{
			Anglecock = UnityEngine.Object.Instantiate(anglecockPrefab, parent, worldPositionStays: false);
			_isPopulated = true;
			Anglecock.hose.Configure(airHosePosition);
			Anglecock.hose.OnGetPressure = () => AirPressure * Mathf.Lerp(0.5f, 1f, AnglecockSetting);
		}

		public void Depopulate()
		{
			if (Coupler != null)
			{
				UnityEngine.Object.Destroy(Coupler.gameObject);
			}
			if (CutLever != null)
			{
				UnityEngine.Object.Destroy(CutLever.gameObject);
			}
			if (Anglecock != null)
			{
				UnityEngine.Object.Destroy(Anglecock.gameObject);
			}
			Coupler = null;
			CutLever = null;
			Anglecock = null;
			_isPopulated = false;
		}
	}

	public string id;

	public string trainCrewId;

	public TypedContainerItem<CarDefinition> DefinitionInfo;

	private float nominalBrakingForce = 30000f;

	protected Config Config;

	internal readonly StringDiagnosticCollector SetupDiagnostics = new StringDiagnosticCollector();

	public static float BrakeForceMultiplier = 1f;

	public static float BrakeForceMultiplierHandbrake = 3f;

	public static bool WearFeature = true;

	public static bool OilFeature = true;

	public static int OverhaulMiles;

	public static float WearMultiplier = 1f;

	public static float OilUseMultiplier = 1f;

	private EndGear EndGearF;

	private EndGear EndGearR;

	public Location LocationF;

	public Location LocationR;

	public float wheelInsetF = 0.3f;

	public float wheelInsetR = 0.3f;

	[NonSerialized]
	public Location WheelBoundsF;

	[NonSerialized]
	public Location WheelBoundsR;

	[Header("State")]
	public bool FrontIsA = true;

	public readonly Vector3[] LastBodyPosition = new Vector3[2];

	public float truckSeparation;

	public float carLength;

	public float couplerHeight = 0.8f;

	public Vector3 airHosePosition = new Vector3(-0.345f, 0.877f, 0.1f);

	private Wheelset _truckA;

	private Wheelset _truckB;

	private Graph.PositionRotation _truckAGizmoPosRot;

	private Graph.PositionRotation _truckBGizmoPosRot;

	private AudioReparenter _audioReparenter;

	internal CarMover _mover;

	protected RollingPlayer _rollingPlayer;

	[CanBeNull]
	public Transform BodyTransform;

	private Location _moverPositionWheelBoundsF;

	private bool _modelLoadPending;

	private Renderer[] _bodyRenderers = Array.Empty<Renderer>();

	private bool _isVisible;

	private bool _hasReceivedDistanceBand;

	private bool _enablePositionCouplers = true;

	private readonly List<Material> _ownedMaterials = new List<Material>();

	private readonly List<Renderer> _truckRenderers = new List<Renderer>();

	private float _velocity;

	private float? _velocityZeroTime;

	private float? _atRestSince;

	private IntegrationSet _set;

	internal int? CachedSetIndex;

	public float compensatingAcceleration;

	[NonSerialized]
	public CarAirSystem air;

	[NonSerialized]
	private AudioClip _airFlowAudioClip;

	[NonSerialized]
	private IAudioSource _brakeExhaustAudioSource;

	private float _grade;

	private float _loadWeight;

	public float maxSpeedMph = 100f;

	private float _curvatureRetardingForce;

	public Action<Vector3, Quaternion> OnPosition;

	protected bool _isFirstPosition = true;

	public bool ghost;

	[NonSerialized]
	public KeyValueObject KeyValueObject;

	protected readonly HashSet<IDisposable> Observers = new HashSet<IDisposable>();

	private CarControlProperties _controlProperties;

	private bool _willDestroyCalled;

	protected readonly HashSet<IDisposable> _controlObservers = new HashSet<IDisposable>();

	private readonly List<ICarMovementListener> _movementListeners = new List<ICarMovementListener>();

	private Vector3 _lastOnPosition;

	private float _lastBrakeCylinderPressure;

	protected MapIcon MapIcon;

	public TagCallout TagCallout;

	protected readonly HashSet<IBrakeAnimator> BrakeAnimators = new HashSet<IBrakeAnimator>(2);

	private readonly HashSet<CarModelLoadToken> _modelLoadTokens = new HashSet<CarModelLoadToken>();

	private readonly Dictionary<string, Task<LoadedAssetReference<GameObject>>> _modelLoadTasks = new Dictionary<string, Task<LoadedAssetReference<GameObject>>>();

	private Task<Wheelset> _truckPrefabLoadTask;

	private float _lastCurvatureUpdate;

	private const float CurvatureUpdatePeriod = 0.2f;

	private float _condition = 1f;

	public bool debugCondition;

	private bool _conditionDebugSetup;

	private Coroutine _conditionUpdateCoroutine;

	private float _hotbox;

	private float _derailment;

	private float _derailmentDisplay;

	private Coroutine _derailmentUpdateCoroutine;

	private float _oiled = 1f;

	public const float MaxOilingSpeedMph = 5f;

	private float _unbankedOdometerService;

	private float _unbankedOdometerActual;

	private const string KeyOdometerActual = "_odometer";

	private const string KeyOdometerService = "_odosvc";

	private const string KeyLastOverhaul = "_lastOverhaul";

	private const string KeyOverhaulProgress = "_overhaulProg";

	public const float CouplerSeparation = 1f;

	internal const float RollingResistance = 0.0015f;

	internal const float CouplerStretch = 0.04f;

	protected SetupPrefabs _setupPrefabs;

	private Coroutine _delayedUnload;

	internal const string KeyOiled = "oiled";

	private readonly List<GameObject> _oilPointPickables = new List<GameObject>();

	private Vector3? _cachedCenterPosition;

	private Quaternion? _cachedCenterRotationCheckForCar;

	private (Graph.PositionRotation, Graph.PositionRotation)? _cachedPositionRotationFR;

	[Header("Sway State")]
	[SerializeField]
	[Range(-1f, 1f)]
	private float swayPosition;

	[SerializeField]
	[Range(-1f, 1f)]
	private float swayVelocity;

	[SerializeField]
	private float swayCurveForce;

	[SerializeField]
	private float swayNoiseForce;

	private float _swayComponentSpeed;

	private float _swayMassCoeff = 1f;

	protected float _linearOffset;

	public const string KeyOwned = "owned";

	public const string KeyOpsPassengerMarker = "ops.passengerMarker";

	public const string KeyOpsRepairDestination = "ops.repair-dest";

	public const string KeyOpsSellDestination = "ops.sell-dest";

	public const string KeyOpsWaybill = "ops.waybill";

	public const string KeyHotbox = "hotbox";

	private static readonly string[] HostPrefixes = new string[5] { "_", "ops.passengerMarker", "owned", "oiled", "hotbox" };

	private static readonly string[] PassengerPrefixes = new string[2] { "door.", "gate." };

	private static readonly string[] TrainmasterPrefixes = new string[6] { "load.", "ops.waybill", "ops.repair-dest", "_colorScheme", "lettering.basic", "whistle.custom" };

	private static readonly string[] OfficerPrefixes = new string[1] { "ops.sell-dest" };

	protected string DefinitionIdentifier => DefinitionInfo.Identifier;

	public CarIdent Ident { get; private set; }

	public string Bardo { get; set; }

	public bool IsInBardo => !string.IsNullOrEmpty(Bardo);

	public CarDefinition Definition => DefinitionInfo.Definition;

	public string CarType => Definition.CarType;

	public string DisplayName { get; private set; }

	public string SortName { get; private set; }

	public virtual bool IsLocomotive => false;

	public Location WheelBoundsA
	{
		get
		{
			if (!FrontIsA)
			{
				return WheelBoundsR.Flipped();
			}
			return WheelBoundsF;
		}
		set
		{
			if (FrontIsA)
			{
				WheelBoundsF = value;
			}
			else
			{
				WheelBoundsR = value.Flipped();
			}
		}
	}

	public Location WheelBoundsB
	{
		get
		{
			if (!FrontIsA)
			{
				return WheelBoundsF.Flipped();
			}
			return WheelBoundsR;
		}
		set
		{
			if (FrontIsA)
			{
				WheelBoundsR = value;
			}
			else
			{
				WheelBoundsF = value.Flipped();
			}
		}
	}

	public Location OpsLocation => WheelBoundsA;

	public EndGear EndGearA
	{
		get
		{
			if (!FrontIsA)
			{
				return EndGearR;
			}
			return EndGearF;
		}
	}

	public EndGear EndGearB
	{
		get
		{
			if (!FrontIsA)
			{
				return EndGearF;
			}
			return EndGearR;
		}
	}

	public Location LocationA
	{
		get
		{
			if (!FrontIsA)
			{
				return LocationR.Flipped();
			}
			return LocationF;
		}
		set
		{
			if (FrontIsA)
			{
				LocationF = value;
			}
			else
			{
				LocationR = value.Flipped();
			}
		}
	}

	public Location LocationB
	{
		get
		{
			if (!FrontIsA)
			{
				return LocationF.Flipped();
			}
			return LocationR;
		}
		set
		{
			if (FrontIsA)
			{
				LocationR = value;
			}
			else
			{
				LocationF = value.Flipped();
			}
		}
	}

	public float WheelInsetA
	{
		get
		{
			if (!FrontIsA)
			{
				return wheelInsetR;
			}
			return wheelInsetF;
		}
	}

	public float WheelInsetB
	{
		get
		{
			if (!FrontIsA)
			{
				return wheelInsetF;
			}
			return wheelInsetR;
		}
	}

	public bool IsVisible
	{
		get
		{
			return _isVisible;
		}
		set
		{
			if (value != _isVisible)
			{
				_isVisible = value;
				this.OnVisibleDidChange?.Invoke(_isVisible);
			}
		}
	}

	public bool IsNearby { get; private set; }

	public float velocity
	{
		get
		{
			return _velocity;
		}
		set
		{
			_velocity = value;
			bool flag = Mathf.Abs(_velocity) < 0.022351963f;
			if (flag && !_velocityZeroTime.HasValue)
			{
				_velocityZeroTime = Time.fixedTime;
			}
			else if (!flag && _velocityZeroTime.HasValue)
			{
				_velocityZeroTime = null;
			}
			if (IsStopped(5f))
			{
				float valueOrDefault = _atRestSince.GetValueOrDefault();
				if (!_atRestSince.HasValue)
				{
					valueOrDefault = Time.fixedTime;
					_atRestSince = valueOrDefault;
				}
			}
			else
			{
				_atRestSince = null;
			}
		}
	}

	public float VelocityMphAbs => Mathf.Abs(velocity * 2.23694f);

	public float? StoppedDuration
	{
		get
		{
			if (_velocityZeroTime.HasValue)
			{
				return Time.fixedTime - _velocityZeroTime.Value;
			}
			return null;
		}
	}

	public bool IsOnTurntable
	{
		get
		{
			if (!(WheelBoundsF.segment?.turntable != null))
			{
				return WheelBoundsR.segment?.turntable != null;
			}
			return true;
		}
	}

	public bool IsAtRest
	{
		get
		{
			if (!_atRestSince.HasValue)
			{
				return false;
			}
			return Time.fixedTime - _atRestSince.Value > 3f;
		}
	}

	public IntegrationSet set
	{
		get
		{
			return _set;
		}
		set
		{
			_set = value;
			ResetAtRest();
		}
	}

	public float Weight => (float)Definition.WeightEmpty + _loadWeight;

	public float GravityForce
	{
		get
		{
			float num = Weight / 2000f;
			return _grade * 100f * num * 20f;
		}
	}

	public virtual float TractiveEffort => 0f;

	public float TractiveForce => TractiveForceMultiplier * TractiveEffort;

	public static float TractiveForceMultiplier { get; set; } = 1.1f;

	public virtual float NormalizedTractiveEffort => 0f;

	public virtual CarWheelState WheelState => CarWheelState.Tracking;

	public float Orientation => FrontIsA ? 1 : (-1);

	public CarControlProperties ControlProperties => _controlProperties ?? (_controlProperties = new CarControlProperties(this, KeyValueObject));

	internal bool PendingDestroy { get; set; }

	public CarArchetype Archetype => Definition.Archetype;

	private static TrainController TrainController => TrainController.Shared;

	private static Graph Graph => TrainController.graph;

	public float CurrentTrackCurvature { get; private set; }

	public float MaximumTrackCurvature { get; private set; } = 100f;

	public bool EnableCondition => true;

	public float Condition
	{
		get
		{
			if (!EnableCondition)
			{
				return 1f;
			}
			return _condition;
		}
	}

	public bool HasHotbox => _hotbox > 0.001f;

	public bool IsDerailed => Mathf.Abs(_derailment) > 0.001f;

	public bool EnableOiling
	{
		get
		{
			if (OilFeature)
			{
				return Archetype != CarArchetype.LocomotiveDiesel;
			}
			return false;
		}
	}

	public bool NeedsOiling
	{
		get
		{
			if (EnableOiling)
			{
				return _oiled < 0.999f;
			}
			return false;
		}
	}

	public float Oiled => _oiled;

	public float OdometerService
	{
		get
		{
			return KeyValueObject["_odosvc"];
		}
		private set
		{
			KeyValueObject["_odosvc"] = value;
		}
	}

	public float OdometerActual
	{
		get
		{
			return KeyValueObject["_odometer"];
		}
		private set
		{
			KeyValueObject["_odometer"] = value;
		}
	}

	public float LastOverhaulOdometer
	{
		get
		{
			return KeyValueObject["_lastOverhaul"];
		}
		set
		{
			KeyValueObject["_lastOverhaul"] = value;
		}
	}

	public float OverhaulProgress
	{
		get
		{
			return KeyValueObject["_overhaulProg"];
		}
		set
		{
			KeyValueObject["_overhaulProg"] = ((value == 0f) ? Value.Null() : Value.Float(value));
		}
	}

	protected bool IsInDidLoadModels { get; private set; }

	public Vector3 InitialTagCalloutPosition => _lastOnPosition;

	public EndGear this[LogicalEnd end] => end switch
	{
		LogicalEnd.A => EndGearA, 
		LogicalEnd.B => EndGearB, 
		_ => throw new ArgumentOutOfRangeException("end", end, null), 
	};

	private EndGear this[End end] => end switch
	{
		End.F => EndGearF, 
		End.R => EndGearR, 
		_ => throw new ArgumentOutOfRangeException("end", end, null), 
	};

	public float RepairCap
	{
		get
		{
			if (!WearFeature)
			{
				return 1f;
			}
			return RepairTrack.RepairCapForKilometersSinceOverhaul(OdometerService - LastOverhaulOdometer);
		}
	}

	public Location SnapshotLocation => Graph.Shared.LocationByMoving(WheelBoundsF, wheelInsetF - carLength / 2f);

	public (MotionSnapshot, Location) MotionSnapshotPositionFrontNonTransient => (new MotionSnapshot(_mover.Position, _mover.Rotation, Vector3.zero), _moverPositionWheelBoundsF);

	public bool IsOwnedByPlayer => KeyValueObject["owned"].BoolValue;

	public Waybill? Waybill { get; private set; }

	public event Action<bool> OnVisibleDidChange;

	public event Action<bool> OnIsNearbyDidChange;

	public event Action OnDidLoadModels;

	public bool IsStopped(float duration = 0f)
	{
		return StoppedDuration >= duration;
	}

	public void ResetAtRest()
	{
		_atRestSince = null;
		_velocityZeroTime = null;
	}

	public override string ToString()
	{
		return "(" + id + " " + DisplayName + ")";
	}

	private void Awake()
	{
		_mover = new CarMover();
		KeyValueObject = base.gameObject.AddComponent<KeyValueObject>();
		_audioReparenter = base.gameObject.AddComponent<AudioReparenter>();
		Config = Config.Shared;
	}

	protected virtual void FixedUpdate()
	{
		if (air.BrakeCylinder.Pressure < _lastBrakeCylinderPressure)
		{
			ResetAtRest();
		}
		_lastBrakeCylinderPressure = air.BrakeCylinder.Pressure;
		if (!(BodyTransform == null))
		{
			UpdateAnglecockControl(EndGearA.Anglecock, EndGearA.AnglecockSetting, air.anglecockFlowA);
			UpdateAnglecockControl(EndGearB.Anglecock, EndGearB.AnglecockSetting, air.anglecockFlowB);
			SynchronizeEndGear(EndGearF, End.F);
			SynchronizeEndGear(EndGearR, End.R);
			bool brakeApplied = air.handbrakeApplied || air.BrakeCylinder.Pressure > 2f;
			UpdateBrakeApplied(brakeApplied);
			UpdateBrakeExhaust();
			_mover.CheckForSleepyMover();
		}
	}

	private void OnDestroy()
	{
		foreach (IDisposable observer in Observers)
		{
			observer.Dispose();
		}
		Observers.Clear();
		UnloadModels();
	}

	protected virtual void OnDrawGizmosSelected()
	{
		DrawLine(WheelBoundsF, Color.Lerp(Color.green, Color.red, 0.25f));
		DrawLine(WheelBoundsR, Color.Lerp(Color.green, Color.red, 0.75f));
		DrawLine(LocationA, Color.green);
		DrawLine(LocationB, Color.red);
		DrawTruckLine(_truckAGizmoPosRot);
		DrawTruckLine(_truckBGizmoPosRot);
		static void DrawLine(Location loc, Color color)
		{
			if (loc.IsValid)
			{
				Vector3 vector = Vector3.up * 0f;
				Vector3 vector2 = Graph.Shared.GetPosition(loc).GameToWorld() + vector;
				Gizmos.color = color;
				Gizmos.DrawRay(vector2, Vector3.up);
			}
		}
		static void DrawTruckLine(Graph.PositionRotation pr)
		{
			Vector3 vector = Vector3.right * 1.435f / 2f;
			Gizmos.color = Color.yellow;
			Vector3 vector2 = WorldTransformer.GameToWorld(pr.Position);
			Vector3 vector3 = vector2 + pr.Rotation * vector;
			Vector3 to = vector2 + pr.Rotation * -vector;
			Gizmos.DrawLine(vector3, to);
		}
	}

	public void Setup(string carId, CarDescriptor descriptor, SetupPrefabs prefabs, bool isGhost)
	{
		id = carId;
		DefinitionInfo = descriptor.DefinitionInfo;
		Bardo = descriptor.Bardo;
		trainCrewId = descriptor.TrainCrewId;
		FrontIsA = !descriptor.Flipped;
		SetIdent(descriptor.Ident);
		_mover.DebugId = DisplayName;
		ValidateDefinition();
		truckSeparation = Definition.TruckSeparation;
		airHosePosition = Definition.AirHosePosition;
		couplerHeight = Definition.CouplerHeight;
		carLength = Definition.Length;
		carLength = CalculateCarLength();
		MaximumTrackCurvature = CalculateMaximumTrackCurvature(Definition.MinimumCurveRadius);
		SetNominalBrakingRatio();
		_airFlowAudioClip = prefabs.AirFlowAudioClip;
		ghost = isGhost;
		base.name = DisplayName + " " + id;
		base.hideFlags = HideFlags.DontSave;
		EndGearF = new EndGear();
		EndGearR = new EndGear();
		_setupPrefabs = prefabs;
		FinishSetup();
		air.car = this;
		if (!isGhost)
		{
			SetupKeyValueObject();
		}
		SetupDiagnostics.Clear();
		ComponentSetup.Context ctx = new ComponentSetup.Context
		{
			AnimationMap = null,
			MaterialMap = null
		};
		SetupComponents(ctx, ComponentLifetime.Static);
	}

	public CarDescriptor Descriptor()
	{
		return new CarDescriptor(DefinitionInfo, Ident, Bardo, trainCrewId, !FrontIsA, new Dictionary<string, Value>(KeyValueObject.Dictionary));
	}

	protected virtual void ValidateDefinition()
	{
		if (Definition.Length < 1f)
		{
			Definition.Length = 1f;
		}
		if (Definition.TruckSeparation < 1f)
		{
			Definition.TruckSeparation = Definition.Length / 2f;
		}
		if (Definition.WeightEmpty < 100)
		{
			Definition.WeightEmpty = 100;
		}
		if (Definition.Components == null)
		{
			Definition.Components = new List<Model.Definition.Component>();
		}
	}

	private void SetNominalBrakingRatio()
	{
		nominalBrakingForce = Archetype switch
		{
			CarArchetype.LocomotiveDiesel => 1f, 
			CarArchetype.LocomotiveSteam => 1f, 
			CarArchetype.Boxcar => 0.7f, 
			CarArchetype.Flat => 0.7f, 
			CarArchetype.Tank => 0.7f, 
			CarArchetype.HopperOpen => 0.7f, 
			CarArchetype.Caboose => 0.7f, 
			CarArchetype.Tender => 0.8f, 
			CarArchetype.Gondola => 0.7f, 
			CarArchetype.Coach => 0.9f, 
			CarArchetype.Baggage => 0.9f, 
			_ => 0.7f, 
		} * (float)Definition.WeightEmpty;
	}

	public void ReloadModel()
	{
		bool num = BodyTransform != null || _modelLoadPending;
		UnloadModels();
		if (num)
		{
			LoadModels();
		}
	}

	[ContextMenu("Log Open Model Load Retains")]
	private void LogModelLoadRetains()
	{
		Debug.Log("Open Model Load Tokens: (" + string.Join(", ", _modelLoadTokens) + ")");
	}

	public IDisposable ModelLoadRetain(string requester)
	{
		CarModelLoadToken carModelLoadToken = new CarModelLoadToken(this, requester);
		bool num = _modelLoadTokens.Any();
		_modelLoadTokens.Add(carModelLoadToken);
		if (!num)
		{
			LoadModels();
		}
		return carModelLoadToken;
	}

	private void ModelLoadRelease(CarModelLoadToken token)
	{
		bool num = _modelLoadTokens.Any();
		_modelLoadTokens.Remove(token);
		if (num && !_modelLoadTokens.Any())
		{
			if (this != null && base.gameObject.activeInHierarchy)
			{
				_delayedUnload = StartCoroutine(UnloadModelsDelayed());
			}
			else
			{
				UnloadModels();
			}
		}
	}

	private IEnumerator UnloadModelsDelayed()
	{
		yield return new WaitForSecondsRealtime(Config.Shared.carModelUnloadDelay);
		_delayedUnload = null;
		UnloadModels();
	}

	private void CancelDelayedUnload()
	{
		if (_delayedUnload != null)
		{
			StopCoroutine(_delayedUnload);
			_delayedUnload = null;
		}
	}

	public IEnumerator WaitForLoaded()
	{
		while (BodyTransform == null)
		{
			yield return null;
		}
	}

	private void LoadModels()
	{
		CancelDelayedUnload();
		if (_modelLoadPending)
		{
			Log.Debug("{car} is already loading", DisplayName);
		}
		else if (!(BodyTransform != null))
		{
			_modelLoadPending = true;
			LoadModelsAsync();
		}
	}

	private async void LoadModelsAsync()
	{
		try
		{
			IPrefabStore prefabStore = TrainController.PrefabStore;
			string modelIdentifier = Definition.ModelIdentifier;
			string assetPackIdentifier = prefabStore.AssetPackIdentifierContainingDefinition(DefinitionInfo.Identifier);
			_modelLoadTasks["model"] = prefabStore.LoadAssetAsync<GameObject>(assetPackIdentifier, modelIdentifier, CancellationToken.None);
			if (!string.IsNullOrEmpty(Definition.TruckIdentifier))
			{
				_truckPrefabLoadTask = prefabStore.TruckPrefabForId(Definition.TruckIdentifier);
			}
			await Task.WhenAll(_modelLoadTasks.Values);
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Error loading car model {identifier}", DefinitionInfo.Identifier);
			return;
		}
		try
		{
			if (_truckPrefabLoadTask != null)
			{
				await _truckPrefabLoadTask;
			}
		}
		catch (Exception exception2)
		{
			Log.Error(exception2, "Error loading trucks");
		}
		HandleModelsLoaded();
	}

	private void HandleModelsLoaded()
	{
		bool modelLoadPending = _modelLoadPending;
		_modelLoadPending = false;
		if (!modelLoadPending)
		{
			Log.Debug("{car} Car unloaded before HandleModelsLoaded.", DisplayName);
			return;
		}
		if (this == null)
		{
			Log.Debug("{car} Car deallocated before HandleModelsLoaded.", DisplayName);
			return;
		}
		GameObject gameObject = UnityEngine.Object.Instantiate(_modelLoadTasks["model"].Result.Asset, Vector3.zero, Quaternion.identity, base.transform);
		_bodyRenderers = GetRenderers(gameObject);
		MakeMaterialsUnique(gameObject, (IReadOnlyCollection<Renderer>)(object)_bodyRenderers);
		gameObject.SetActive(value: false);
		BodyTransform = gameObject.transform;
		_audioReparenter.BodyTransform = BodyTransform;
		try
		{
			IsInDidLoadModels = true;
			DidLoadModels();
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Exception from DidLoadModels {car}", this);
		}
		finally
		{
			IsInDidLoadModels = false;
		}
		gameObject.SetActive(value: true);
		SetVisible(_isVisible);
		DidSetBodyActive();
		if (WheelBoundsF.IsValid)
		{
			PositionWheelBoundsFront(WheelBoundsF, Graph, MovementInfo.Zero, update: true);
		}
		this.OnDidLoadModels?.Invoke();
	}

	private void MakeMaterialsUnique(GameObject obj, IReadOnlyCollection<Renderer> renderers)
	{
		int indexOffset = _ownedMaterials.Count;
		List<Material> uniqueMaterials = (from m in renderers.SelectMany((Renderer r) => r.sharedMaterials)
			where m != null
			select m).Distinct().ToList();
		_ownedMaterials.AddRange(uniqueMaterials.Select(delegate(Material mat)
		{
			Material material = new Material(mat);
			material.name = material.name + " (" + id + ")";
			return material;
		}));
		foreach (Renderer renderer in renderers)
		{
			renderer.sharedMaterials = renderer.sharedMaterials.Select(delegate(Material mat)
			{
				if (mat == null)
				{
					return (Material)null;
				}
				int index = uniqueMaterials.IndexOf(mat) + indexOffset;
				return _ownedMaterials[index];
			}).ToArray();
		}
		MaterialMap componentInChildren = obj.GetComponentInChildren<MaterialMap>();
		if (componentInChildren != null)
		{
			componentInChildren.ReplaceMaterials(uniqueMaterials, _ownedMaterials);
		}
	}

	protected virtual void DidLoadModels()
	{
		SetupPrefabs setupPrefabs = _setupPrefabs;
		GameObject gameObject = BodyTransform.gameObject;
		gameObject.transform.localPosition = Vector3.zero;
		_mover.ConfigureForBody(gameObject);
		SetupTrucks();
		SetupBrakeAnimations();
		SetupCouplers(setupPrefabs.CouplerPrefab);
		SetupCutLevers(setupPrefabs.CutLeverPrefab);
		SetupAnglecocks(setupPrefabs.AnglecockPrefab);
		_rollingPlayer = gameObject.AddComponent<RollingPlayer>();
		_rollingPlayer.profile = setupPrefabs.RollingProfile;
		gameObject.name = "Body";
		gameObject.AddComponent<CarPickable>().car = this;
		CarColorController carColorController = gameObject.AddComponent<CarColorController>();
		_isFirstPosition = true;
		ResetEndGearPositions();
		UpdateAnglecockControl(EndGearA.Anglecock, EndGearA.AnglecockSetting, air.anglecockFlowA, force: true);
		UpdateAnglecockControl(EndGearB.Anglecock, EndGearB.AnglecockSetting, air.anglecockFlowB, force: true);
		AnimationMap item = SetupForAnimation().Item2;
		ComponentSetup.Context ctx = new ComponentSetup.Context
		{
			AnimationMap = item,
			MaterialMap = BodyTransform.GetComponentInChildren<MaterialMap>(),
			CarColorController = carColorController
		};
		SetupComponents(ctx, ComponentLifetime.Model);
		if (ghost)
		{
			DisableAllColliders();
		}
	}

	protected virtual void DidSetBodyActive()
	{
		_movementListeners.AddRange(BodyTransform.GetComponentsInChildren<ICarMovementListener>());
	}

	private void SetupTrucks()
	{
		if (string.IsNullOrEmpty(Definition.TruckIdentifier))
		{
			return;
		}
		Wheelset result = _truckPrefabLoadTask.Result;
		if (result == null)
		{
			Debug.LogWarning("Car defines trucks " + Definition.TruckIdentifier + " but task has no result");
			return;
		}
		_truckA = UnityEngine.Object.Instantiate(result, BodyTransform, worldPositionStays: false);
		_truckB = UnityEngine.Object.Instantiate(result, BodyTransform, worldPositionStays: false);
		_truckA.name = "Truck A";
		_truckB.name = "Truck B";
		WheelClackProfile wheelClackProfile = TrainController.Shared.wheelClackProfile;
		_truckA.Configure(wheelClackProfile, this);
		_truckB.Configure(wheelClackProfile, this);
		UpdateTruckLinearOffset();
		float num = truckSeparation / 2f;
		_truckA.transform.localPosition = Vector3.forward * num;
		_truckB.transform.localPosition = Vector3.back * num;
		_truckA.transform.localRotation = Quaternion.identity;
		_truckB.transform.localRotation = Quaternion.identity;
		BrakeAnimators.Add(_truckA);
		BrakeAnimators.Add(_truckB);
		Renderer[] renderers = GetRenderers(_truckA.gameObject);
		Renderer[] renderers2 = GetRenderers(_truckB.gameObject);
		MakeMaterialsUnique(_truckA.gameObject, (IReadOnlyCollection<Renderer>)(object)renderers);
		MakeMaterialsUnique(_truckB.gameObject, (IReadOnlyCollection<Renderer>)(object)renderers2);
		_truckRenderers.AddRange(renderers);
		_truckRenderers.AddRange(renderers2);
		if (EnableOiling)
		{
			float diameter = _truckA.diameterInInches / 39.37008f;
			float axleSeparation = _truckA.CalculateAxleSpread();
			AddOilPointPickable(num, axleSeparation, diameter);
			AddOilPointPickable(0f - num, axleSeparation, diameter);
		}
	}

	protected void AddOilPointPickable(float zPosition, float axleSeparation, float diameter)
	{
		if (EnableOiling)
		{
			OilPointPickable oilPointPickable = UnityEngine.Object.Instantiate(TrainController.oilPointPrefab, BodyTransform, worldPositionStays: false);
			GameObject gameObject = oilPointPickable.gameObject;
			gameObject.transform.localPosition = zPosition * Vector3.forward;
			oilPointPickable.Configure(this, axleSeparation, diameter, _oilPointPickables.Count);
			_oilPointPickables.Add(gameObject);
		}
	}

	private void RemoveAllOilPointPickables()
	{
		foreach (GameObject oilPointPickable in _oilPointPickables)
		{
			UnityEngine.Object.Destroy(oilPointPickable);
		}
		_oilPointPickables.Clear();
	}

	public void OffsetOiled(float oil)
	{
		SetOiled(_oiled + oil);
	}

	private void SetOiled(float oil)
	{
		if (EnableOiling)
		{
			_oiled = Mathf.Clamp01(oil);
			KeyValueObject["oiled"] = _oiled;
		}
	}

	private static Renderer[] GetRenderers(GameObject o)
	{
		return (from r in o.GetComponentsInChildren<Renderer>()
			where r.enabled
			select r).ToArray();
	}

	private void SetupComponents(ComponentSetup.Context ctx, ComponentLifetime lifetime)
	{
		PreSetupComponents(lifetime);
		foreach (Model.Definition.Component item in Definition.EnabledComponentsForLifetime(lifetime))
		{
			try
			{
				SetupComponent(item, ctx, lifetime);
			}
			catch (TransformReferenceExtensions.BadTransformReferenceException ex)
			{
				SetupDiagnostics.Log($"{item}: {ex.Message}");
				Log.Error("{definition} component {component}: {e}", DefinitionIdentifier, item, ex.Message);
			}
			catch (Exception ex2)
			{
				SetupDiagnostics.Log($"{item}: {ex2.Message}");
				Log.Error(ex2, "Exception configuring component {component} on car {car} {definitionIdentifier}", item, this, DefinitionIdentifier);
			}
		}
		if (lifetime == ComponentLifetime.Model)
		{
			SetupComponent(new DerailedEffectComponent
			{
				Name = "Derailed Effect",
				Separation = truckSeparation
			}, ctx, lifetime);
		}
		PostSetupComponents(lifetime);
	}

	protected virtual void PreSetupComponents(ComponentLifetime lifetime)
	{
	}

	protected virtual void PostSetupComponents(ComponentLifetime lifetime)
	{
		if (lifetime == ComponentLifetime.Model)
		{
			PostSetupComponentsHeadlights();
		}
	}

	private void PostSetupComponentsHeadlights()
	{
		HeadlightController[] componentsInChildren = BodyTransform.gameObject.GetComponentsInChildren<HeadlightController>();
		if (componentsInChildren.Length != 0)
		{
			LocomotiveLightingController locomotiveLightingController = BodyTransform.gameObject.AddComponent<LocomotiveLightingController>();
			locomotiveLightingController.key = "headlight";
			locomotiveLightingController.headlights = componentsInChildren.ToList();
			if (Archetype == CarArchetype.Tender)
			{
				KeyValueAdjacentCopier keyValueAdjacentCopier = BodyTransform.gameObject.AddComponent<KeyValueAdjacentCopier>();
				keyValueAdjacentCopier.end = End.F;
				keyValueAdjacentCopier.keys.Add(locomotiveLightingController.key);
			}
		}
	}

	private void SetupComponent(Model.Definition.Component component, ComponentSetup.Context setupContext, ComponentLifetime lifetime)
	{
		Transform parent = lifetime switch
		{
			ComponentLifetime.Static => base.transform, 
			ComponentLifetime.Model => BodyTransform.ResolveTransform(component.Parent, defaultReturnsReceiver: true), 
			_ => throw new ArgumentOutOfRangeException("lifetime", lifetime, null), 
		};
		Action<string, Action<Value>> observeProperty = lifetime switch
		{
			ComponentLifetime.Static => delegate(string key, Action<Value> action)
			{
				Observers.Add(KeyValueObject.Observe(key, action));
			}, 
			ComponentLifetime.Model => delegate(string key, Action<Value> action)
			{
				_controlObservers.Add(KeyValueObject.Observe(key, action));
			}, 
			_ => throw new ArgumentOutOfRangeException("lifetime", lifetime, null), 
		};
		ComponentSetup.Setup(DefinitionIdentifier, component, setupContext, parent, observeProperty, TrainController.Shared.PrefabInstantiator);
	}

	private void SetupBrakeAnimations()
	{
		if (Definition.BrakeAnimations == null || Definition.BrakeAnimations.Count == 0)
		{
			return;
		}
		var (animator, animMap) = SetupForAnimation();
		if (!(animMap == null))
		{
			BrakeAnimator brakeAnimator = animMap.gameObject.AddComponent<BrakeAnimator>();
			brakeAnimator.animator = animator;
			brakeAnimator.brakeAnimationClips = Definition.BrakeAnimations.Select((AnimationReference animRef) => animMap.ClipForName(animRef.ClipName)).ToArray();
			BrakeAnimators.Add(brakeAnimator);
		}
	}

	private (Animator, AnimationMap) SetupForAnimation()
	{
		AnimationMap componentInChildren = BodyTransform.GetComponentInChildren<AnimationMap>();
		if (componentInChildren == null)
		{
			Log.Warning("Car {car} definition BrakeAnimations but no AnimationMap component", DefinitionIdentifier);
			return (null, null);
		}
		Animator component = componentInChildren.GetComponent<Animator>();
		if (component != null)
		{
			return (component, componentInChildren);
		}
		Animator animator = componentInChildren.gameObject.AddComponent<Animator>();
		animator.cullingMode = AnimatorCullingMode.CullCompletely;
		return (animator, componentInChildren);
	}

	protected virtual void UnloadModels()
	{
		CancelDelayedUnload();
		try
		{
			_modelLoadPending = false;
			if (BodyTransform == null)
			{
				return;
			}
			foreach (IDisposable controlObserver in _controlObservers)
			{
				controlObserver.Dispose();
			}
			_controlObservers.Clear();
			RemoveAllOilPointPickables();
			if (_truckA != null)
			{
				BrakeAnimators.Remove(_truckA);
				UnityEngine.Object.Destroy(_truckA.gameObject);
			}
			if (_truckB != null)
			{
				BrakeAnimators.Remove(_truckB);
				UnityEngine.Object.Destroy(_truckB.gameObject);
			}
			_truckA = null;
			_truckB = null;
			BrakeAnimator[] componentsInChildren = BodyTransform.GetComponentsInChildren<BrakeAnimator>();
			foreach (BrakeAnimator item in componentsInChildren)
			{
				BrakeAnimators.Remove(item);
			}
			UnityEngine.Object.Destroy(BodyTransform.GetComponent<CarPickable>());
			UnityEngine.Object.Destroy(BodyTransform.GetComponent<RollingPlayer>());
			UnityEngine.Object.Destroy(BodyTransform.gameObject);
			BodyTransform = null;
			_audioReparenter.BodyTransform = null;
			_bodyRenderers = Array.Empty<Renderer>();
			_truckRenderers.Clear();
			foreach (Material ownedMaterial in _ownedMaterials)
			{
				UnityEngine.Object.Destroy(ownedMaterial);
			}
			_ownedMaterials.Clear();
			_mover.ClearBody();
			EndGearF.Depopulate();
			EndGearR.Depopulate();
			_movementListeners.Clear();
			foreach (Task<LoadedAssetReference<GameObject>> value in _modelLoadTasks.Values)
			{
				value.Result?.Dispose();
			}
			_modelLoadTasks.Clear();
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Exception in UnloadModels for {car}", this);
			Debug.LogException(exception);
		}
	}

	public void SetVisible(bool visible)
	{
		IsVisible = visible;
		Renderer[] bodyRenderers = _bodyRenderers;
		for (int i = 0; i < bodyRenderers.Length; i++)
		{
			bodyRenderers[i].enabled = visible;
		}
		EndGearF.SetVisible(visible);
		EndGearR.SetVisible(visible);
		if (_truckA != null)
		{
			_truckA.SetVisible(visible);
		}
		if (_truckB != null)
		{
			_truckB.SetVisible(visible);
		}
		UpdateMaterialsForCondition();
	}

	public void SetCullerDistanceBand(int previousDistanceBand, int distanceBand)
	{
		if (distanceBand == previousDistanceBand && _hasReceivedDistanceBand)
		{
			return;
		}
		_hasReceivedDistanceBand = true;
		bool flag = distanceBand <= 1;
		if (flag != IsNearby)
		{
			IsNearby = flag;
			this.OnIsNearbyDidChange?.Invoke(flag);
		}
		bool flag2 = distanceBand <= 0;
		if (flag2 != _enablePositionCouplers)
		{
			_enablePositionCouplers = flag2;
			if (_enablePositionCouplers)
			{
				PositionCoupler(LogicalEnd.A);
				PositionCoupler(LogicalEnd.B);
			}
		}
	}

	private void UpdateMaterialsForCondition()
	{
		int wearPropertyName = Shader.PropertyToID("_Wear");
		Renderer[] bodyRenderers = _bodyRenderers;
		foreach (Renderer r in bodyRenderers)
		{
			Apply(r);
		}
		foreach (Renderer truckRenderer in _truckRenderers)
		{
			Apply(truckRenderer);
		}
		void Apply(Renderer renderer)
		{
			LeanTween.cancel(renderer.gameObject);
			Material[] sharedMaterials = renderer.sharedMaterials;
			foreach (Material material in sharedMaterials)
			{
				if (!(material == null) && material.shader.name == "Railroader/Standard Car Shader (Builtin)")
				{
					float value = Mathf.InverseLerp(1f, 0.25f, _condition);
					material.SetFloat(wearPropertyName, value);
				}
			}
		}
	}

	private void SetupKeyValueObject()
	{
		StateManager.Shared.RegisterPropertyObject(id, KeyValueObject, this);
		Observers.Add(KeyValueObject.Observe(KeyValueKeyFor(EndGearStateKey.IsCoupled, End.F), delegate(Value value)
		{
			HandleCoupledChange(End.F, value.BoolValue);
		}));
		Observers.Add(KeyValueObject.Observe(KeyValueKeyFor(EndGearStateKey.IsCoupled, End.R), delegate(Value value)
		{
			HandleCoupledChange(End.R, value.BoolValue);
		}));
		Observers.Add(KeyValueObject.Observe(KeyValueKeyFor(EndGearStateKey.IsAirConnected, End.F), delegate(Value value)
		{
			HandleAirConnectedChange(End.F, value.BoolValue);
		}));
		Observers.Add(KeyValueObject.Observe(KeyValueKeyFor(EndGearStateKey.IsAirConnected, End.R), delegate(Value value)
		{
			HandleAirConnectedChange(End.R, value.BoolValue);
		}));
		Observers.Add(KeyValueObject.Observe(KeyValueKeyFor(EndGearStateKey.Anglecock, End.F), delegate(Value value)
		{
			EndGearF.AnglecockSetting = value.FloatValue;
		}));
		Observers.Add(KeyValueObject.Observe(KeyValueKeyFor(EndGearStateKey.Anglecock, End.R), delegate(Value value)
		{
			EndGearR.AnglecockSetting = value.FloatValue;
		}));
		Observers.Add(KeyValueObject.Observe(KeyValueKeyFor(EndGearStateKey.CutLever, End.F), delegate(Value value)
		{
			HandleCutLeverValue(End.F, value.FloatValue);
		}));
		Observers.Add(KeyValueObject.Observe(KeyValueKeyFor(EndGearStateKey.CutLever, End.R), delegate(Value value)
		{
			HandleCutLeverValue(End.R, value.FloatValue);
		}));
		Observers.Add(KeyValueObject.Observe(PropertyChange.KeyForControl(PropertyChange.Control.Handbrake), delegate(Value value)
		{
			air.handbrakeApplied = value.FloatValue > 0.5f;
			if (!air.handbrakeApplied)
			{
				ResetAtRest();
			}
		}));
		Observers.Add(KeyValueObject.Observe(PropertyChange.KeyForControl(PropertyChange.Control.Bleed), delegate(Value value)
		{
			if (!(value.FloatValue < 0.5f))
			{
				air.BleedBrakeCylinder();
				if (StateManager.IsHost)
				{
					KeyValueObject.SetDelayed(PropertyChange.KeyForControl(PropertyChange.Control.Bleed), Value.Null(), 0.5f);
				}
			}
		}));
		Observers.Add(KeyValueObject.Observe(PropertyChange.KeyForControl(PropertyChange.Control.Condition), delegate(Value value)
		{
			_condition = (value.IsNull ? 1f : value.FloatValue);
			UpdateMaterialsForCondition();
		}));
		Observers.Add(KeyValueObject.Observe(PropertyChange.KeyForControl(PropertyChange.Control.Derailment), delegate(Value value)
		{
			if (Mathf.Abs(_derailment - (float)value) > 0.01f)
			{
				ResetAtRest();
			}
			_derailment = value;
		}));
		Observers.Add(KeyValueObject.Observe("oiled", delegate(Value value)
		{
			_oiled = value.FloatValueOrDefault(1f);
		}));
		Observers.Add(KeyValueObject.Observe(PropertyChange.KeyForControl(PropertyChange.Control.Hotbox), delegate(Value value)
		{
			_hotbox = value;
		}));
		Observers.Add(KeyValueObject.Observe("ops.waybill", UpdateWaybill));
		int count = Definition.LoadSlots.Count;
		for (int num = 0; num < count; num++)
		{
			Observers.Add(KeyValueObject.Observe($"load.{num}", delegate
			{
				UpdateLoadWeight();
			}));
		}
		UpdateSwayMassCoeff();
	}

	protected virtual float CalculateCarLength()
	{
		return carLength;
	}

	private void UpdateLoadWeight()
	{
		_loadWeight = Enumerable.Range(0, Definition.LoadSlots.Count).Sum(delegate(int slotIndex)
		{
			CarLoadInfo? loadInfo = this.GetLoadInfo(slotIndex);
			if (!loadInfo.HasValue)
			{
				return 0f;
			}
			CarLoadInfo value = loadInfo.Value;
			Load load = CarPrototypeLibrary.instance.LoadForId(value.LoadId);
			if (load == null)
			{
				Log.Error("Car {car} contains unknown load: {loadId}", DisplayName, value.LoadId);
				return 0f;
			}
			return load.Pounds(value.Quantity);
		});
		UpdateSwayMassCoeff();
	}

	private void UpdateSwayMassCoeff()
	{
		float time = Weight / 2000f;
		_swayMassCoeff = Config.swayCarTonsMassCoeff.Evaluate(time);
	}

	protected virtual bool WantsEndGear(End end)
	{
		if (Archetype == CarArchetype.Tender)
		{
			return end == End.R;
		}
		return true;
	}

	public virtual bool ForceConnectedToAtRear(Car other)
	{
		return false;
	}

	public float CouplerSlack(End end)
	{
		return end switch
		{
			End.F => WantsEndGear(End.F) ? 0.02f : 0.001f, 
			End.R => WantsEndGear(End.R) ? 0.02f : 0.001f, 
			_ => throw new ArgumentOutOfRangeException("end", end, null), 
		};
	}

	private void SetupCouplers(Coupler couplerPrefab)
	{
		if ((bool)couplerPrefab)
		{
			EndGearF.Coupler = CreateCoupler(End.F);
			EndGearR.Coupler = CreateCoupler(End.R);
		}
		Coupler CreateCoupler(End side)
		{
			if (WantsEndGear(side))
			{
				Coupler coupler = UnityEngine.Object.Instantiate(couplerPrefab, BodyTransform, worldPositionStays: false);
				coupler.car = this;
				coupler.end = side;
				coupler.gameObject.hideFlags = HideFlags.DontSave;
				return coupler;
			}
			return null;
		}
	}

	private void SetupCutLevers(CutLever prefab)
	{
		EndGearF.CutLever = UnityEngine.Object.Instantiate(prefab, BodyTransform, worldPositionStays: false);
		EndGearR.CutLever = UnityEngine.Object.Instantiate(prefab, BodyTransform, worldPositionStays: false);
		EndGearF.CutLever.transform.localPosition = OffsetToEnd(End.F) * Vector3.forward;
		EndGearR.CutLever.transform.localPosition = OffsetToEnd(End.R) * Vector3.forward;
		EndGearR.CutLever.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
		EndGearF.CutLever.OnActivate += delegate
		{
			HandleCouplerClick(EndGearF.Coupler);
		};
		EndGearR.CutLever.OnActivate += delegate
		{
			HandleCouplerClick(EndGearR.Coupler);
		};
		EndGearF.CutLever.gameObject.SetActive(value: false);
		EndGearR.CutLever.gameObject.SetActive(value: false);
	}

	private void SetupAnglecocks(Anglecock prefab)
	{
		EndGearF.Populate(prefab, BodyTransform, airHosePosition);
		EndGearR.Populate(prefab, BodyTransform, airHosePosition);
		EndGearF.Anglecock.transform.localPosition = OffsetToEnd(End.F) * Vector3.forward;
		EndGearR.Anglecock.transform.localPosition = OffsetToEnd(End.R) * Vector3.forward;
		EndGearR.Anglecock.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
		EndGearF.Anglecock.Setup(End.F, id);
		EndGearR.Anglecock.Setup(End.R, id);
		EndGearF.Anglecock.control.tooltipText = () => AnglecockTooltipText(EndGearF.Anglecock);
		EndGearR.Anglecock.control.tooltipText = () => AnglecockTooltipText(EndGearR.Anglecock);
		EndGearF.Anglecock.gameObject.SetActive(WantsEndGear(End.F));
		EndGearR.Anglecock.gameObject.SetActive(WantsEndGear(End.R));
		EndGearF.DidPopulate();
		EndGearR.DidPopulate();
	}

	private void ResetEndGearPositions()
	{
		Vector3 vector = OffsetToEnd(End.F) * Vector3.forward;
		Vector3 vector2 = OffsetToEnd(End.R) * Vector3.forward;
		Quaternion identity = Quaternion.identity;
		Quaternion quaternion = Quaternion.Euler(0f, 180f, 0f);
		Transform transform = EndGearF.CutLever.transform;
		Transform obj = EndGearR.CutLever.transform;
		transform.localPosition = vector;
		obj.localPosition = vector2;
		transform.localRotation = identity;
		obj.localRotation = quaternion;
		Transform obj2 = EndGearF.Anglecock.transform;
		Transform transform2 = EndGearR.Anglecock.transform;
		obj2.localPosition = vector + identity * airHosePosition;
		transform2.localPosition = vector2 + quaternion * airHosePosition;
		obj2.localRotation = identity;
		transform2.localRotation = quaternion;
	}

	public void Reverse()
	{
		FrontIsA = !FrontIsA;
	}

	private void LocationAssertValid(Location loc, End end)
	{
		if (!loc.IsValid)
		{
			throw new InvalidLocationException(loc, this, end);
		}
	}

	private void LocationsAssertValid()
	{
		LocationAssertValid(WheelBoundsF, End.F);
		LocationAssertValid(WheelBoundsR, End.R);
	}

	public void GetPositionRotationFR(Graph graph, out Graph.PositionRotation positionRotationF, out Graph.PositionRotation positionRotationR)
	{
		(Graph.PositionRotation, Graph.PositionRotation)? cachedPositionRotationFR = _cachedPositionRotationFR;
		if (cachedPositionRotationFR.HasValue)
		{
			(positionRotationF, positionRotationR) = cachedPositionRotationFR.GetValueOrDefault();
		}
		else
		{
			positionRotationF = graph.GetPositionRotation(LocationF);
			positionRotationR = graph.GetPositionRotation(LocationR);
			_cachedPositionRotationFR = (positionRotationF, positionRotationR);
		}
	}

	public void GetPositionFR(Graph graph, out Vector3 positionF, out Vector3 positionR)
	{
		GetPositionRotationFR(graph, out var positionRotationF, out var positionRotationR);
		positionF = positionRotationF.Position;
		positionR = positionRotationR.Position;
	}

	public Vector3 GetCenterPosition(Graph graph)
	{
		if (_cachedCenterPosition.HasValue)
		{
			return _cachedCenterPosition.Value;
		}
		LocationsAssertValid();
		GetPositionFR(graph, out var positionF, out var positionR);
		Vector3 vector = Vector3.Lerp(positionF, positionR, 0.5f);
		_cachedCenterPosition = vector;
		return vector;
	}

	public Quaternion GetCenterRotationCheckForCar(Graph graph)
	{
		if (_cachedCenterRotationCheckForCar.HasValue)
		{
			return _cachedCenterRotationCheckForCar.Value;
		}
		GetPositionFR(graph, out var positionF, out var positionR);
		float y = Vector3.SignedAngle(positionR - positionF, Vector3.right, Vector3.up);
		Quaternion quaternion = Quaternion.Euler(0f, y, 0f);
		_cachedCenterRotationCheckForCar = quaternion;
		return quaternion;
	}

	public Quaternion GetCenterRotation(Graph graph)
	{
		LocationsAssertValid();
		GetPositionRotationFR(graph, out var positionRotationF, out var positionRotationR);
		return Quaternion.Lerp(positionRotationF.Rotation, positionRotationR.Rotation, 0.5f);
	}

	public Matrix4x4 GetTransformMatrix(Graph graph)
	{
		Vector3 centerPosition = GetCenterPosition(graph);
		Quaternion centerRotation = GetCenterRotation(graph);
		return Matrix4x4.TRS(centerPosition, centerRotation, Vector3.one);
	}

	private string AnglecockTooltipText(Anglecock anglecock)
	{
		float value = anglecock.control.Value;
		if (value < 0.01f)
		{
			return "Closed";
		}
		if (value > 0.99f)
		{
			return "Open";
		}
		return $"{value * 100f:F0}% Open";
	}

	protected virtual void FinishSetup()
	{
		air = base.gameObject.AddComponent<CarAirSystem>();
		maxSpeedMph = UnityEngine.Random.Range(75, 85);
		float num = 1f;
		wheelInsetF = (wheelInsetR = Mathf.Max(0.3f, (carLength - truckSeparation) / 2f - num));
	}

	public virtual bool ShouldUpdatePosition()
	{
		if (!(Mathf.Abs(_derailment - _derailmentDisplay) > 0.001f))
		{
			return IsOnTurntable;
		}
		return true;
	}

	public virtual Location PositionWheelBoundsFront(Location wheelBoundsF, Graph graph, MovementInfo info, bool update)
	{
		float num = (carLength - truckSeparation) / 2f;
		Location location = graph.LocationByMoving(wheelBoundsF, -1f * (num - wheelInsetF));
		Location location2 = graph.LocationByMoving(location, -1f * truckSeparation);
		Location location3 = graph.LocationByMoving(location2, -1f * (num - wheelInsetR));
		if (!update)
		{
			return location3;
		}
		bool isFirstPosition = _isFirstPosition;
		_isFirstPosition = false;
		UpdateBaseLocations(wheelBoundsF, location3, graph, isFirstPosition);
		UpdateCurvatureForLocation(location);
		PositionAccuracy accuracy = (IsVisible ? PositionAccuracy.High : PositionAccuracy.Standard);
		Graph.PositionRotation positionRotation = graph.GetPositionRotation(location, accuracy);
		Graph.PositionRotation positionRotation2 = graph.GetPositionRotation(location2, accuracy);
		_truckAGizmoPosRot = positionRotation;
		_truckBGizmoPosRot = positionRotation2;
		SetBodyPosition(wheelBoundsF, positionRotation, positionRotation2, 0f, isFirstPosition);
		SetTruckPositions(positionRotation.Rotation, positionRotation2.Rotation, info);
		if (_rollingPlayer != null)
		{
			_rollingPlayer.SetVelocity(velocity);
		}
		FireOnMovement(info);
		return location3;
	}

	public Location PositionFront(Location front, Graph graph, MovementInfo info, bool update)
	{
		Location wheelBoundsF = graph.LocationByMoving(front, 0f - wheelInsetF);
		Location start = PositionWheelBoundsFront(wheelBoundsF, graph, info, update);
		return graph.LocationByMoving(start, 0f - wheelInsetR);
	}

	public Location PositionWheelBoundsA(Location wbA, Graph graph, MovementInfo movementInfo, bool update)
	{
		if (FrontIsA)
		{
			return PositionWheelBoundsFront(wbA, graph, movementInfo, update);
		}
		float num = carLength - (wheelInsetF + wheelInsetR);
		Location wheelBoundsF = graph.LocationByMoving(wbA, 0f - num).Flipped();
		PositionWheelBoundsFront(wheelBoundsF, graph, movementInfo, update);
		return wheelBoundsF.Flipped();
	}

	public Location PositionA(Location a, Graph graph, MovementInfo movementInfo, bool update)
	{
		if (FrontIsA)
		{
			return PositionFront(a, graph, movementInfo, update);
		}
		Location front = graph.LocationByMoving(a, 0f - carLength).Flipped();
		PositionFront(front, graph, movementInfo, update);
		return front.Flipped();
	}

	protected void UpdateBaseLocations(Location wheelBoundsF, Location wheelBoundsR, Graph graph, bool immediate)
	{
		WheelBoundsF = wheelBoundsF;
		WheelBoundsR = wheelBoundsR;
		LocationF = graph.LocationByMoving(wheelBoundsF, wheelInsetF, checkSwitchAgainstMovement: false, Graph.EndOfTrackHandling.Unclamped);
		LocationR = graph.LocationByMoving(wheelBoundsR, 0f - wheelInsetR, checkSwitchAgainstMovement: false, Graph.EndOfTrackHandling.Unclamped);
		_cachedCenterPosition = null;
		_cachedCenterRotationCheckForCar = null;
		_cachedPositionRotationFR = null;
		if (Mathf.Abs(velocity) > 0.001f && StateManager.IsHost)
		{
			CheckForSwitchesAgainstMovement(graph);
		}
		if (_enablePositionCouplers)
		{
			PositionCoupler(LogicalEnd.A);
			PositionCoupler(LogicalEnd.B);
		}
	}

	protected virtual void FireOnMovement(MovementInfo info)
	{
		_unbankedOdometerActual += info.Distance;
		_unbankedOdometerService += ServiceMetersFromActual(info.Distance);
		if (_unbankedOdometerActual > 500f)
		{
			BankOdometer();
		}
		foreach (ICarMovementListener movementListener in _movementListeners)
		{
			movementListener.CarDidMove(info);
		}
	}

	protected virtual float ServiceMetersFromActual(float meters)
	{
		return meters * Config.serviceDistanceConditionMultiplier.Evaluate(Condition);
	}

	public void PrepareForSnapshotSave()
	{
		BankOdometer();
	}

	private void BankOdometer()
	{
		if (StateManager.IsHost)
		{
			float num = _unbankedOdometerActual / 1000f;
			float num2 = _unbankedOdometerService / 1000f;
			OdometerActual += num;
			OdometerService += num2;
			SetCondition(_condition - WearForMovement(num2));
			OffsetOiled(0f - OilUseForMovement(num2));
			CheckForHotbox(num);
			_unbankedOdometerActual = 0f;
			_unbankedOdometerService = 0f;
		}
	}

	private void CheckForHotbox(float distanceKm)
	{
		if (HasHotbox || !EnableOiling || IsLocomotive || VelocityMphAbs < 15f)
		{
			return;
		}
		float num = distanceKm * 0.6213712f;
		if (num < 0.1f)
		{
			return;
		}
		float num2 = Config.hotboxChanceForOil.Evaluate(_oiled);
		if (!(num2 < 0.01f))
		{
			float value = UnityEngine.Random.value;
			float num3 = num2 / (num * 100f);
			bool flag = value < num3;
			Log.Debug("CheckForHotbox: {car} {distanceMi}, {chancePer100Mi}, {random} vs {threshold} -> {trigger}", this, num, num2, value, num3, flag);
			if (flag)
			{
				Log.Information("Hotbox triggered on {car} at {odoKm} with oil {oil}", this, OdometerActual, _oiled);
				ControlProperties[PropertyChange.Control.Hotbox] = 1;
			}
		}
	}

	private float WearForMovement(float kilometers)
	{
		if (!WearFeature)
		{
			return 0f;
		}
		float num = Config.wearPerMileForCondition.Evaluate(_condition) / 100f;
		if (EnableOiling)
		{
			float num2 = Config.wearMultiplierForOil.Evaluate(_oiled);
			num *= num2;
			if (HasHotbox)
			{
				float num3 = Config.hotboxWearPerMileForSpeed.Evaluate(VelocityMphAbs) / 100f;
				num += num3;
			}
		}
		return num * 0.6213712f * kilometers * WearMultiplier;
	}

	private float OilUseForMovement(float kilometers)
	{
		return Config.oilUsePerMileForCondition.Evaluate(_condition) / 100f * 0.6213712f * kilometers * OilUseMultiplier;
	}

	protected void UpdateCurvatureForLocation(Location location)
	{
		float time = Time.time;
		if (!(time < _lastCurvatureUpdate + 0.2f))
		{
			_lastCurvatureUpdate = time;
			float currentTrackCurvature = Graph.CurvatureAtLocation(location);
			CurrentTrackCurvature = currentTrackCurvature;
			ApplyCurvatureToModel(0.2f);
		}
	}

	private void ApplyCurvatureToModel(float dt)
	{
		if (!StateManager.IsHost || !EnableCondition)
		{
			return;
		}
		float num = Mathf.Abs(velocity);
		float num2 = TrainMath.MaximumSpeedMphForCurve(CurrentTrackCurvature, MaximumTrackCurvature) * 0.44703928f;
		float num3 = 0f;
		if (num > num2)
		{
			num3 = TrainMath.DamageForSpeed(num, num2);
			if (debugCondition && !_conditionDebugSetup)
			{
				DebugGUI.SetGraphProperties("dmg", "Condition", 0f, 1f, 0, Color.magenta, autoScale: false);
				DebugGUI.SetGraphProperties("dps", "Damage per Second", 0f, 0.001f, 0, Color.cyan, autoScale: true);
				_conditionDebugSetup = true;
			}
			ApplyConditionDelta((0f - num3) * dt);
			float delta = TrainMath.DerailmentForSpeedOnCurve(num, num2);
			ApplyDerailmentDelta(delta, "Curve mph={0} max={1}", num * 2.23694f, num2 * 2.23694f);
		}
		if (IsDerailed)
		{
			float num4 = TrainMath.DamageForSpeed(num, 0f);
			ApplyConditionDelta((0f - num4) * dt);
			num3 += num4;
		}
		if (debugCondition)
		{
			DebugGUI.Graph("dps", num3);
			DebugGUI.Graph("dmg", _condition);
		}
	}

	public void ApplyConditionDelta(float delta)
	{
		_condition = Mathf.Clamp01(_condition + delta);
		if (_conditionUpdateCoroutine == null)
		{
			_conditionUpdateCoroutine = StartCoroutine(UpdateConditionAfterDelay());
		}
	}

	private IEnumerator UpdateConditionAfterDelay()
	{
		yield return new WaitForSeconds(0.5f);
		_conditionUpdateCoroutine = null;
		SetCondition(_condition);
	}

	public void SetCondition(float condition)
	{
		_condition = Mathf.Clamp01(condition);
		KeyValueObject[PropertyChange.KeyForControl(PropertyChange.Control.Condition)] = Value.Float(_condition);
	}

	private Vector3 CouplerPivot(End end, float extra = 0f)
	{
		float z = OffsetToEnd(end, extra);
		return TransformPoint(new Vector3(0f, couplerHeight, z));
	}

	protected virtual float OffsetToEnd(End end, float extra = 0f)
	{
		return (float)((end == End.F) ? 1 : (-1)) * (extra + carLength / 2f);
	}

	private void PositionCoupler(LogicalEnd logicalEnd)
	{
		Coupler coupler = this[logicalEnd].Coupler;
		if (!(coupler == null))
		{
			float extra = -0.276f;
			LogicalEnd logical = ((logicalEnd == LogicalEnd.A) ? LogicalEnd.B : LogicalEnd.A);
			Car car = set?.GetCouplerConnection(this, logicalEnd);
			Vector3? obj = ((car != null) ? new Vector3?(car.CouplerPivot(car.LogicalToEnd(logical), extra)) : ((Vector3?)null));
			Vector3 vector = CouplerPivot(LogicalToEnd(logicalEnd), extra);
			Vector3 obj2 = obj ?? CouplerPivot(LogicalToEnd(logicalEnd), 1f);
			Vector3 vector2 = TransformDirection(Vector3.up);
			Vector3 vector3 = Vector3.ProjectOnPlane(Quaternion.LookRotation(obj2 - vector, vector2) * Vector3.forward, vector2);
			Quaternion quaternion = Quaternion.Inverse(_mover.Rotation);
			coupler.transform.SetLocalPositionAndRotation(quaternion * (vector - _mover.Position), quaternion * Unity.Mathematics.quaternion.LookRotation(vector3, vector2));
		}
	}

	private void CheckForSwitchesAgainstMovement(Graph graph)
	{
		try
		{
			float distance = velocity * 3f;
			Location start = ((velocity > 0f) ? WheelBoundsF : WheelBoundsR);
			graph.LocationByMoving(start, distance, checkSwitchAgainstMovement: true, Graph.EndOfTrackHandling.Throw);
		}
		catch (SwitchAgainstMovement switchAgainstMovement)
		{
			TrainController.FixSwitchAgainstMovement(this, switchAgainstMovement.Node);
		}
		catch (EndOfTrack)
		{
		}
	}

	private void SetTruckPositions(Quaternion aRot, Quaternion bRot, MovementInfo info)
	{
		if (!(_truckA == null) && !(_truckB == null))
		{
			_truckA.transform.rotation = aRot;
			_truckB.transform.rotation = bRot;
			float distance = info.DeltaTime * velocity;
			_truckA.Roll(distance, velocity);
			_truckB.Roll(distance, velocity);
		}
	}

	public Graph.PositionRotation TransformForDerailment(Graph.PositionRotation pr, Vector3 center0, Location location)
	{
		float num = _derailmentDisplay;
		if (!FrontIsA)
		{
			num *= -1f;
		}
		float num2 = Math.Abs(num);
		if (num2 < 0.01f)
		{
			return pr;
		}
		Vector3 position = pr.Position;
		Quaternion rotation = pr.Rotation;
		float t = Noise.OctavePerlin(position.x * 0.05f, position.z * 0.05f, 3);
		float num3 = -0.2f * Ramp(Mathf.InverseLerp(0.025f, 0.2f, num2));
		bool num4 = location.segment.style != TrackSegment.Style.Standard;
		float a = (num4 ? 0.1f : 1f);
		float b = (num4 ? 0.2f : 2f);
		float num5 = num * Mathf.Lerp(a, b, t);
		position += num3 * Vector3.up;
		position += pr.Rotation * (num5 * Vector3.right);
		float y = Vector3.SignedAngle(pr.Position - center0, position - center0, Vector3.up);
		Quaternion quaternion = Quaternion.Euler(0f, 0f, (0f - num) * Mathf.Lerp(5f, 15f, t));
		rotation = Quaternion.Euler(0f, y, 0f) * (rotation * quaternion);
		return new Graph.PositionRotation(position, rotation);
		static float Ramp(float x)
		{
			return Mathf.Sin((x - 0.5f) * MathF.PI) * 0.5f + 0.5f;
		}
	}

	[StringFormatMethod("reasonFormat")]
	public void ApplyDerailmentForce(float force, string reasonFormat, params object[] formatParams)
	{
		float weight = Weight;
		float num = weight * 5f;
		float num2 = weight * 20f;
		Debug.Log($"ApplyDerailmentForce: {this} {force:g2} {num:g2} {num2:g2} -> {Mathf.InverseLerp(num, num2, force):F1}");
		if (!(force < num))
		{
			float delta = Mathf.InverseLerp(num, num2, force);
			ApplyDerailmentDelta(delta, reasonFormat, formatParams);
		}
	}

	[StringFormatMethod("reasonFormat")]
	public void ApplyDerailmentDelta(float delta, string reasonFormat = "", params object[] formatParams)
	{
		if (delta >= 0f && delta < 0.001f)
		{
			return;
		}
		float derailment = _derailment;
		bool isDerailed = IsDerailed;
		if (!isDerailed && delta > 0f)
		{
			delta = Mathf.Max(delta, 0.15f);
		}
		_derailment = Mathf.Clamp01(_derailment + delta);
		ResetAtRest();
		if (delta > 0f)
		{
			if (!isDerailed)
			{
				string value = $"{DateTime.Now:s} {string.Format(reasonFormat, formatParams)}";
				KeyValueObject["derailmentReason"] = Value.String(value);
				Log.Information($"Derailment: {this}: {reasonFormat}", formatParams);
				Messenger.Default.Send(default(CarDidDerail));
			}
			if (derailment < 0.25f && _derailment > 0.25f)
			{
				BreakConnections(LogicalEnd.A);
				BreakConnections(LogicalEnd.B);
			}
		}
		if (_derailmentUpdateCoroutine == null)
		{
			_derailmentUpdateCoroutine = StartCoroutine(UpdateDerailmentAfterDelay());
		}
		void BreakConnections(LogicalEnd end)
		{
			EndGear endGear = this[end];
			if (endGear.IsCoupled || endGear.IsAirConnected)
			{
				ApplyEndGearChange(end, EndGearStateKey.IsCoupled, boolValue: false);
				ApplyEndGearChange(end, EndGearStateKey.IsAirConnected, boolValue: false);
				if (set.TryGetAdjacentCar(this, end, out var adjacent))
				{
					LogicalEnd logicalEnd = ((end == LogicalEnd.A) ? LogicalEnd.B : LogicalEnd.A);
					adjacent.ApplyEndGearChange(logicalEnd, EndGearStateKey.IsCoupled, boolValue: false);
					adjacent.ApplyEndGearChange(logicalEnd, EndGearStateKey.IsAirConnected, boolValue: false);
				}
			}
		}
	}

	private IEnumerator UpdateDerailmentAfterDelay()
	{
		yield return new WaitForSeconds(0.3f);
		_derailmentUpdateCoroutine = null;
		string key = PropertyChange.KeyForControl(PropertyChange.Control.Derailment);
		Log.Information("Derailment {car}: {old} -> {new}", this, KeyValueObject[key].FloatValue, _derailment);
		KeyValueObject[key] = Value.Float(_derailment);
	}

	private void UpdateSwaySpring(float dt)
	{
		float num = (0f - Config.swaySpringStiffness) * (swayPosition - 0f) + (0f - Config.swaySpringDamping) * swayVelocity;
		float num2 = _swayMassCoeff * Config.swaySpringMass;
		float num3 = (num + swayCurveForce + swayNoiseForce) / num2;
		swayVelocity += num3 * dt;
		swayVelocity = Mathf.Clamp(swayVelocity, -10f, 10f);
		swayPosition += swayVelocity * dt;
		swayPosition = Mathf.Clamp(swayPosition, -10f, 10f);
	}

	private void CalculateRandomSwayComponent(Vector3 position)
	{
		float num = Mathf.PerlinNoise(position.x * Config.swayNoiseScale, position.z * Config.swayNoiseScale);
		float num2 = ((num < 0.2f) ? (0f - Mathf.InverseLerp(0.2f, 0f, num)) : ((!(num > 0.8f)) ? 0f : Mathf.InverseLerp(0.8f, 1f, num)));
		float num3 = num2;
		swayNoiseForce = _swayComponentSpeed * num3 * Config.swayImpulseScale;
	}

	private void CalculateCurveSwayComponent(Quaternion qa, Quaternion qb)
	{
		float currentTrackCurvature = CurrentTrackCurvature;
		float f = Mathf.DeltaAngle(0f, (Quaternion.Inverse(qb) * qa).eulerAngles.y);
		float b = Config.swayComponentCurveDegrees.Evaluate(currentTrackCurvature);
		swayCurveForce = Mathf.Sign(f) * Config.swayCurveScale * Mathf.Min(_swayComponentSpeed, b) * _swayMassCoeff;
	}

	protected void SetBodyPosition(Location wheelBoundsF, Graph.PositionRotation a, Graph.PositionRotation b, float centerZ, bool immediate)
	{
		float deltaTime = Time.deltaTime;
		LastBodyPosition[0] = a.Position;
		LastBodyPosition[1] = b.Position;
		Vector3 vector = b.Position - a.Position;
		vector.y = 0f;
		_grade = (b.Position.y - a.Position.y) / vector.magnitude;
		float cameraSwayIntensity = Preferences.CameraSwayIntensity;
		Quaternion swayRotation;
		if (cameraSwayIntensity > 0.01f)
		{
			_swayComponentSpeed = Config.swayComponentSpeedMph.Evaluate(Mathf.Abs(velocity * 2.23694f));
			CalculateRandomSwayComponent(a.Position);
			CalculateCurveSwayComponent(a.Rotation, b.Rotation);
			UpdateSwaySpring(deltaTime);
			float num = swayPosition;
			swayRotation = Quaternion.Euler(0f, 0f, cameraSwayIntensity * Config.swayScaleRoll * num);
			a = Sway(a);
			b = Sway(b);
		}
		_derailmentDisplay = Mathf.Lerp(_derailmentDisplay, _derailment, deltaTime * 2f);
		Vector3 center = Vector3.Lerp(a.Position, b.Position, 0.5f);
		a = TransformForDerailment(a, center, wheelBoundsF);
		b = TransformForDerailment(b, center, wheelBoundsF);
		Quaternion quaternion = Quaternion.LookRotation(a.Position - b.Position, Vector3.Lerp(a.Rotation * Vector3.up, b.Rotation * Vector3.up, 0.5f));
		Vector3 vector2 = Vector3.Lerp(a.Position, b.Position, 0.5f) + quaternion * Vector3.forward * (0f - centerZ);
		Vector3 vector3 = WorldTransformer.GameToWorld(vector2);
		_mover.Move(vector3, quaternion, immediate);
		_audioReparenter.Rigidbody.Move(_mover.Position, _mover.Rotation.OnlyEulerY());
		_moverPositionWheelBoundsF = wheelBoundsF;
		if (immediate || (_lastOnPosition - vector2).magnitude > 0.1f)
		{
			OnPosition?.Invoke(vector2, quaternion);
			_lastOnPosition = vector2;
		}
		UpdateMapIconPosition(vector3, quaternion);
		if (TagCallout != null)
		{
			TagCallout.SetPosition(vector3);
		}
		Graph.PositionRotation Sway(Graph.PositionRotation pr)
		{
			return new Graph.PositionRotation(pr.Position, pr.Rotation * swayRotation);
		}
	}

	private void SynchronizeEndGear(EndGear endGear, End end)
	{
		if (endGear.Coupler != null)
		{
			endGear.Coupler.SetOpen(!endGear.IsCoupled);
		}
		endGear.AirPressure = air.BrakeLine.Pressure;
		_ = endGear.NeedsConnectionUpdate;
		if (set == null)
		{
			return;
		}
		endGear.NeedsConnectionUpdate = false;
		LogicalEnd logicalEnd = EndToLogical(end);
		LogicalEnd end2 = ((logicalEnd == LogicalEnd.A) ? LogicalEnd.B : LogicalEnd.A);
		Car car = AirConnectedTo(logicalEnd);
		if (car == null)
		{
			if (endGear.IsAirConnected)
			{
				Debug.LogWarning($"Can't synchronize end gear yet: {DisplayName} {end}'s otherCar is null");
			}
			else
			{
				endGear.SetConnectedTo(null);
			}
		}
		else
		{
			EndGear connectedTo = car[end2];
			endGear.SetConnectedTo(connectedTo);
		}
	}

	private static void UpdateAnglecockControl(Anglecock anglecock, float value, float flow, bool force = false)
	{
		if (!(anglecock == null))
		{
			anglecock.control.Value = value;
			anglecock.Flow = flow;
		}
	}

	private void UpdateBrakeExhaust()
	{
		if (BodyTransform == null)
		{
			return;
		}
		if (air.exhaustFlow > 0.1f)
		{
			if (_brakeExhaustAudioSource == null)
			{
				_brakeExhaustAudioSource = VirtualAudioSourcePool.Checkout("BrakeExhaust", _airFlowAudioClip, loop: true, AudioController.Group.AirHose, 11, BodyTransform, AudioDistance.Local);
				_brakeExhaustAudioSource.volume = 0f;
				_brakeExhaustAudioSource.minDistance = 2f;
				_brakeExhaustAudioSource.maxDistance = 20f;
				_brakeExhaustAudioSource.Play();
			}
			_brakeExhaustAudioSource.volume = Mathf.InverseLerp(0f, 100f, air.exhaustFlow);
		}
		else if (_brakeExhaustAudioSource != null)
		{
			VirtualAudioSourcePool.Return(_brakeExhaustAudioSource);
			_brakeExhaustAudioSource = null;
		}
	}

	public LogicalEnd ClosestLogicalEndTo(Vector3 point, Graph graph)
	{
		Vector3 position = graph.GetPosition(LocationA);
		Vector3 position2 = graph.GetPosition(LocationB);
		if (!((position - point).sqrMagnitude < (position2 - point).sqrMagnitude))
		{
			return LogicalEnd.B;
		}
		return LogicalEnd.A;
	}

	public LogicalEnd ClosestLogicalEndTo(Location loc, Graph graph)
	{
		Vector3 position = graph.GetPosition(loc);
		return ClosestLogicalEndTo(position, graph);
	}

	public Car AirConnectedTo(LogicalEnd logicalEnd)
	{
		return set.GetAirConnection(this, logicalEnd);
	}

	public Car CoupledTo(LogicalEnd logicalEnd)
	{
		return set.GetCouplerConnection(this, logicalEnd);
	}

	public Snapshot.Car Snapshot(SnapshotOption option = SnapshotOption.None)
	{
		Location snapshotLocation = SnapshotLocation;
		return new Snapshot.Car(id, DefinitionIdentifier, Ident.RoadNumber, unusedKey3: false, Graph.CreateSnapshotTrackLocation(snapshotLocation), velocity, trainCrewId, Ident.ReportingMark, FrontIsA, Bardo);
	}

	public void HandleCouplerClick(Coupler coupler)
	{
		bool smartAirHelperModifier = GameInput.SmartAirHelperModifier;
		if (coupler == EndGearA.Coupler && EndGearA.IsCoupled)
		{
			Car car = CoupledTo(LogicalEnd.A);
			ApplyEndGearChange(LogicalEnd.A, EndGearStateKey.CutLever, 1f);
			if (smartAirHelperModifier)
			{
				ApplyEndGearChange(LogicalEnd.A, EndGearStateKey.Anglecock, 0f);
				car.ApplyEndGearChange(LogicalEnd.B, EndGearStateKey.Anglecock, 0f);
			}
		}
		else if (coupler == EndGearB.Coupler && EndGearB.IsCoupled)
		{
			Car car2 = CoupledTo(LogicalEnd.B);
			ApplyEndGearChange(LogicalEnd.B, EndGearStateKey.CutLever, 1f);
			if (smartAirHelperModifier)
			{
				ApplyEndGearChange(LogicalEnd.B, EndGearStateKey.Anglecock, 0f);
				car2.ApplyEndGearChange(LogicalEnd.A, EndGearStateKey.Anglecock, 0f);
			}
		}
	}

	private void HandleCutLeverValue(End end, float value)
	{
		if (!((double)value < 0.5))
		{
			LogicalEnd logicalEnd = EndToLogical(end);
			HandleOpenCoupler(logicalEnd);
			LeanTween.delayedCall(1f, (Action)delegate
			{
				ApplyEndGearChange(logicalEnd, EndGearStateKey.CutLever, 0f);
			});
		}
	}

	private void HandleOpenCoupler(LogicalEnd logicalEnd)
	{
		if (StateManager.IsHost && this[logicalEnd].IsCoupled)
		{
			Log.Debug("OpenCoupler on {car} at {logicalEnd}", this, logicalEnd);
			Car car;
			Car car2;
			switch (logicalEnd)
			{
			case LogicalEnd.A:
				car = set.GetCouplerConnection(this, LogicalEnd.A);
				car2 = this;
				break;
			case LogicalEnd.B:
				car = this;
				car2 = set.GetCouplerConnection(this, LogicalEnd.B);
				break;
			default:
				throw new ArgumentOutOfRangeException("logicalEnd", logicalEnd, null);
			}
			car.ApplyEndGearChange(LogicalEnd.B, EndGearStateKey.IsCoupled, boolValue: false);
			car2.ApplyEndGearChange(LogicalEnd.A, EndGearStateKey.IsCoupled, boolValue: false);
		}
	}

	protected virtual void UpdateBrakeApplied(bool brakeApplied)
	{
		foreach (IBrakeAnimator brakeAnimator in BrakeAnimators)
		{
			brakeAnimator.BrakeApplied = brakeApplied;
		}
	}

	public void WillDestroy(bool isMovingToBardo = false)
	{
		UnloadModels();
		if (isMovingToBardo)
		{
			return;
		}
		_willDestroyCalled = true;
		try
		{
			if (StateManager.Shared != null)
			{
				StateManager.Shared.UnregisterPropertyObject(id);
			}
			IdGenerator.Cars.Remove(id);
		}
		catch (Exception ex)
		{
			Debug.LogError(ex);
			Log.Error(ex, "Exception in WillDestroy {car}", this);
		}
	}

	public LogicalEnd EndToLogical(End end)
	{
		if (FrontIsA)
		{
			if (end != End.F)
			{
				return LogicalEnd.B;
			}
			return LogicalEnd.A;
		}
		if (end != End.R)
		{
			return LogicalEnd.B;
		}
		return LogicalEnd.A;
	}

	public End LogicalToEnd(LogicalEnd logical)
	{
		if (FrontIsA)
		{
			if (logical != LogicalEnd.A)
			{
				return End.R;
			}
			return End.F;
		}
		if (logical != LogicalEnd.B)
		{
			return End.R;
		}
		return End.F;
	}

	public void HandleCoupledChange(End end, bool isCoupled)
	{
		this[end].IsCoupled = isCoupled;
		PositionCoupler(EndToLogical(end));
		if (!isCoupled)
		{
			ResetAtRest();
		}
	}

	public void HandleAirConnectedChange(End end, bool b)
	{
		this[end].IsAirConnected = b;
	}

	public void ApplyEndGearChange(LogicalEnd logicalEnd, EndGearStateKey endGearStateKey, bool boolValue)
	{
		End end = LogicalToEnd(logicalEnd);
		if (!ValidateEndGearChange(end, endGearStateKey, boolValue))
		{
			if (!PendingDestroy)
			{
				Log.Warning("Ignoring end gear change: {car} {logicalEnd} {endGearStateKey} {boolValue}", this, logicalEnd, endGearStateKey, boolValue);
			}
		}
		else
		{
			string key = KeyValueKeyFor(endGearStateKey, end);
			KeyValueObject[key] = Value.Bool(boolValue);
		}
	}

	public void ApplyEndGearChange(LogicalEnd logicalEnd, EndGearStateKey endGearStateKey, float f)
	{
		End end = LogicalToEnd(logicalEnd);
		string key = KeyValueKeyFor(endGearStateKey, end);
		KeyValueObject[key] = Value.Float(f);
	}

	private bool ValidateEndGearChange(End end, EndGearStateKey endGearStateKey, bool boolValue)
	{
		if (!boolValue && !AnyCarAdjacent(end))
		{
			return true;
		}
		if (RequiresConnectionToEnd(end))
		{
			return boolValue;
		}
		return true;
		bool AnyCarAdjacent(End queryEnd)
		{
			Car adjacent;
			if (set != null)
			{
				return set.TryGetAdjacentCar(this, EndToLogical(queryEnd), out adjacent);
			}
			return false;
		}
	}

	protected virtual bool RequiresConnectionToEnd(End end)
	{
		if (Archetype == CarArchetype.Tender)
		{
			return end == End.F;
		}
		return false;
	}

	private static string KeyValueKeyFor(EndGearStateKey key, End end)
	{
		string text = ((end == End.F) ? "f" : "r");
		bool flag = false;
		string text2;
		switch (key)
		{
		case EndGearStateKey.IsCoupled:
			text2 = "coupled";
			flag = true;
			break;
		case EndGearStateKey.IsAirConnected:
			text2 = "airConnected";
			flag = true;
			break;
		case EndGearStateKey.Anglecock:
			text2 = "anglecock";
			break;
		case EndGearStateKey.CutLever:
			text2 = "cutLever";
			break;
		default:
			throw new ArgumentOutOfRangeException("key", key, null);
		}
		if (!flag)
		{
			return text + "." + text2;
		}
		return "_" + text + "." + text2;
	}

	public bool TryGetAdjacentCar(LogicalEnd logicalEnd, out Car adjacent)
	{
		if (set != null)
		{
			return set.TryGetAdjacentCar(this, logicalEnd, out adjacent);
		}
		adjacent = null;
		return false;
	}

	public virtual void WillMove()
	{
		EndGearF.NeedsConnectionUpdate = true;
		EndGearR.NeedsConnectionUpdate = true;
		_isFirstPosition = true;
		_grade = 0f;
		_velocityZeroTime = null;
		velocity = 0f;
		compensatingAcceleration = 0f;
		_lastCurvatureUpdate = 0f;
		_hasReceivedDistanceBand = false;
		if (StateManager.IsHost)
		{
			ApplyEndGearChange(LogicalEnd.A, EndGearStateKey.IsCoupled, boolValue: false);
			ApplyEndGearChange(LogicalEnd.B, EndGearStateKey.IsCoupled, boolValue: false);
			ApplyEndGearChange(LogicalEnd.A, EndGearStateKey.IsAirConnected, boolValue: false);
			ApplyEndGearChange(LogicalEnd.B, EndGearStateKey.IsAirConnected, boolValue: false);
			SetAdjacentCarsNotConnected();
		}
	}

	public void SetAdjacentCarsNotConnected()
	{
		if (StateManager.IsHost && set != null)
		{
			if (TryGetAdjacentCar(LogicalEnd.A, out var adjacent))
			{
				adjacent.ApplyEndGearChange(LogicalEnd.B, EndGearStateKey.IsCoupled, boolValue: false);
				adjacent.ApplyEndGearChange(LogicalEnd.B, EndGearStateKey.IsAirConnected, boolValue: false);
			}
			if (TryGetAdjacentCar(LogicalEnd.B, out var adjacent2))
			{
				adjacent2.ApplyEndGearChange(LogicalEnd.A, EndGearStateKey.IsCoupled, boolValue: false);
				adjacent2.ApplyEndGearChange(LogicalEnd.A, EndGearStateKey.IsAirConnected, boolValue: false);
			}
		}
	}

	private void DisableAllColliders()
	{
		Collider[] componentsInChildren = GetComponentsInChildren<Collider>();
		foreach (Collider collider in componentsInChildren)
		{
			if (!collider.isTrigger)
			{
				collider.enabled = false;
			}
		}
	}

	public void WorldDidMove(Vector3 offset)
	{
		_mover.WorldDidMove(offset);
		_audioReparenter.Rigidbody.position = _mover.Position;
		OffsetMapIconPosition(offset);
	}

	private void UpdateMapIconPosition(Vector3 position, Quaternion rotation)
	{
		if (!(MapIcon == null))
		{
			MapIcon.transform.SetPositionAndRotation(position + Vector3.up * 100f, Quaternion.Euler(-90f, rotation.eulerAngles.y, 0f));
		}
	}

	private void OffsetMapIconPosition(Vector3 offset)
	{
		if (!(MapIcon == null))
		{
			MapIcon.transform.position += offset;
		}
	}

	private Vector3 TransformPoint(Vector3 point)
	{
		return _mover.Rotation * point + _mover.Position;
	}

	private Vector3 TransformDirection(Vector3 point)
	{
		return _mover.Rotation * point;
	}

	public MotionSnapshot GetMotionSnapshot()
	{
		return _mover.GetMotionSnapshot();
	}

	public Transform Resolve(TransformReference transformReference)
	{
		return BodyTransform.ResolveTransform(transformReference);
	}

	public AnimationClip Resolve(AnimationReference animationReference)
	{
		if (animationReference == null || string.IsNullOrEmpty(animationReference.ClipName))
		{
			return null;
		}
		return BodyTransform.GetComponentInChildren<AnimationMap>().Resolve(animationReference);
	}

	private static float Sigmoid(float x, float a, float b)
	{
		return 1f / (1f + Mathf.Exp(a * (x + b)));
	}

	public float CalculateCurvatureRetardingForce(float absVelocity)
	{
		float num = 0.75f * (Weight / 2000f) * CurrentTrackCurvature * 4.44822f * Sigmoid(absVelocity, -3.5f, -5f);
		_curvatureRetardingForce = CalculateBindingDueToCurvature();
		return num + _curvatureRetardingForce;
	}

	public float CalculateDerailedRetardingForce()
	{
		if (!IsDerailed)
		{
			return 0f;
		}
		return Weight * 0.7f;
	}

	private float CalculateBindingDueToCurvature()
	{
		if (CurrentTrackCurvature < MaximumTrackCurvature)
		{
			return 0f;
		}
		float num = Sigmoid(Mathf.Abs(velocity) * 2.23694f, -0.6f, -10f);
		float num2 = Sigmoid(CurrentTrackCurvature - MaximumTrackCurvature, -1.8f, -3.4f);
		return 1f * Weight * num * num2;
	}

	public static float CalculateMaximumTrackCurvature(CurveRadius minimumCurveRadius)
	{
		return minimumCurveRadius switch
		{
			CurveRadius.ExtraSmall => 40f, 
			CurveRadius.Small => 36f, 
			CurveRadius.Medium => 23f, 
			CurveRadius.Large => 17f, 
			CurveRadius.ExtraLarge => 14f, 
			_ => 1000f, 
		};
	}

	public virtual void PostRestoreProperties()
	{
		air.PostRestoreProperties();
		_derailmentDisplay = _derailment;
	}

	public void ResetKeyValueProperties(IReadOnlyDictionary<string, Value> properties, SetValueOrigin origin)
	{
		KeyValueObject.ResetData(properties, origin);
	}

	public IEnumerable<Car> EnumerateCoupled(LogicalEnd fromEnd = LogicalEnd.A)
	{
		if (set == null)
		{
			Log.Warning("Can't enumerate car that has no integration set {carId}", id);
			return Array.Empty<Car>();
		}
		return set.EnumerateCoupledTo(this, fromEnd);
	}

	public IEnumerable<Car> EnumerateAirOpen(LogicalEnd fromEnd = LogicalEnd.A)
	{
		if (set == null)
		{
			throw new Exception("Can't enumerate car that has no integration set");
		}
		return set.EnumerateAirOpenTo(this, fromEnd);
	}

	public IEnumerable<Car> EnumerateCoupled(End fromEnd)
	{
		return EnumerateCoupled(EndToLogical(fromEnd));
	}

	public void SetOffsetWithinSet(float elementPosition)
	{
		_linearOffset = elementPosition;
		UpdateTruckLinearOffset();
	}

	protected virtual void UpdateTruckLinearOffset()
	{
		if (_truckA != null)
		{
			_truckA.SetLinearOffset(_linearOffset - truckSeparation / 2f);
		}
		if (_truckB != null)
		{
			_truckB.SetLinearOffset(_linearOffset + truckSeparation / 2f);
		}
	}

	public float CalculateBrakingForce(float brakePercent, float absVelocity)
	{
		float time = absVelocity * 2.23694f;
		float num = Config.brakeForceCurve.Evaluate(time);
		brakePercent *= Mathf.Lerp(0.8f, 1f, Condition);
		float num2 = nominalBrakingForce * BrakeForceMultiplier;
		return brakePercent * num2 * num * 4.44822f;
	}

	public Location LocationFor(LogicalEnd logicalEnd)
	{
		if (logicalEnd != LogicalEnd.A)
		{
			return LocationB;
		}
		return LocationA;
	}

	public Location LocationFor(End end)
	{
		if (end != End.F)
		{
			return LocationR;
		}
		return LocationF;
	}

	public void SetPlayerNearby(bool nearby)
	{
		if (!Preferences.EnableCarUpdateOptimization)
		{
			nearby = true;
		}
		_mover.SetPlayerNearby(nearby);
	}

	public virtual void SetIdent(CarIdent ident)
	{
		Ident = ident;
		DisplayName = Ident.ReportingMark + " " + Ident.RoadNumber;
		string text = CarType;
		if (text.StartsWith("L"))
		{
			text = "L";
		}
		SortName = $"{Ident.ReportingMark} {text} {Ident.RoadNumber,8}";
	}

	internal (Vector3 position, Quaternion rotation) GetMoverTargetPositionRotation()
	{
		_mover.UpdateMovement(out var goalPosition, out var goalRotation, Time.deltaTime);
		return (position: goalPosition, rotation: goalRotation);
	}

	internal void ApplyBuilderPhotoMaterial(Shader carShader, Shader windowShader)
	{
		Apply(_bodyRenderers);
		if (_truckA != null)
		{
			Apply(_truckA.GetComponentsInChildren<MeshRenderer>());
		}
		if (_truckB != null)
		{
			Apply(_truckB.GetComponentsInChildren<MeshRenderer>());
		}
		void Apply(IEnumerable<Renderer> mrs)
		{
			foreach (Renderer mr in mrs)
			{
				Material[] sharedMaterials = mr.sharedMaterials;
				foreach (Material material in sharedMaterials)
				{
					if (material.shader.name.Contains("Standard Car Shader"))
					{
						material.shader = carShader;
					}
					if (material.shader.name.Contains("Window Glass"))
					{
						material.shader = windowShader;
					}
				}
			}
		}
	}

	public void SendPropertyChange(PropertyChange.Control control, float value)
	{
		SendPropertyChange(control, new FloatPropertyValue(value));
	}

	public void SendPropertyChange(PropertyChange.Control control, bool value)
	{
		SendPropertyChange(control, new BoolPropertyValue(value));
	}

	public void SendPropertyChange(PropertyChange.Control control, IPropertyValue value)
	{
		if (!_willDestroyCalled)
		{
			StateManager.ApplyLocal(new PropertyChange(id, PropertyChange.KeyForControl(control), value));
		}
	}

	private void UpdateWaybill(Value waybillValue)
	{
		try
		{
			Waybill = Model.Ops.Waybill.FromPropertyValue(waybillValue, OpsController.Shared);
		}
		catch (OpsController.InvalidOpsCarPositionException ex)
		{
			Log.Error(ex, "Waybill for car {car} contains an invalid ops position: {pos}", this, ex.Identifier);
			Waybill = null;
		}
		catch (Exception exception)
		{
			Log.Warning(exception, "{car} Exception in Waybill.FromPropertyValue", this);
			Waybill = null;
		}
	}

	public AuthorizationRequirementInfo AuthorizationRequirementForPropertyWrite(string key)
	{
		string[] officerPrefixes = OfficerPrefixes;
		foreach (string value in officerPrefixes)
		{
			if (key.StartsWith(value))
			{
				return AuthorizationRequirement.MinimumLevelOfficer;
			}
		}
		officerPrefixes = TrainmasterPrefixes;
		foreach (string value2 in officerPrefixes)
		{
			if (key.StartsWith(value2))
			{
				return AuthorizationRequirement.MinimumLevelTrainmaster;
			}
		}
		officerPrefixes = PassengerPrefixes;
		foreach (string value3 in officerPrefixes)
		{
			if (key.StartsWith(value3))
			{
				return AuthorizationRequirement.MinimumLevelPassenger;
			}
		}
		officerPrefixes = HostPrefixes;
		foreach (string value4 in officerPrefixes)
		{
			if (key.StartsWith(value4))
			{
				return AuthorizationRequirement.HostOnly;
			}
		}
		return new AuthorizationRequirementInfo(AuthorizationRequirement.MinimumLevelCrew, trainCrewId);
	}
}
