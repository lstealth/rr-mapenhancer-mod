using System;
using Game.AccessControl;
using Helpers;
using KeyValue.Runtime;
using Model.AI;
using UnityEngine;

namespace Game.State;

public class GameStorage : IPropertyAccessControlDelegate
{
	private readonly KeyValueObject _gameKeyValueObject;

	public const string ObjectId = "_game";

	public const string KeyMode = "mode";

	private const string KeyDefaultAccessLevel = "defaultAccessLevel";

	private const string KeyAllowNewPlayers = "allowNewPlayers";

	private const string KeyPasswordHash = "passwordHash";

	public const int RailroadNameMaxLength = 50;

	public const int ReportingMarkMaxLength = 6;

	public const string ReportingMarkRegex = "[\\p{L}&]";

	public const int RoadNumberMaxLength = 6;

	private const string KeyBalance = "balance";

	private const string KeyTimeMultiplier = "timeMultiplier";

	private const string TrainCrewMembershipRequiredKey = "trainCrewMembershipRequired";

	private const string TrainCrewMembershipManagedByTrainmasterKey = "trainCrewMembershipManagedByTrainmaster";

	private const string KeyLoanAmount = "loanAmount";

	private const string KeyNextInterestDate = "loanNextInterestDate";

	private const string KeyLoanNextInterestOffset = "loanNextInterestOffset";

	private const string KeyUnbilledRunDuration = "unbilledRunDuration";

	private const string KeyInterchangeServeHour = "interchangeServeHour";

	private const string KeyInterchangeShuffle = "interchangeShuffle";

	private const string KeyPassengerLimit = "passengerLimit";

	private const string KeyBrakeForce = "brakeForce";

	private const string KeyAICrossingSignal = "aiCrossingSignal";

	private const string KeyAIPassengerStopEnable = "aiPassStopEnable";

	private const string KeyAIPassengerStopMinimumStopDuration = "aiPassStopMinStopDur";

	private const string KeyAICallSignals = "aiCallSignals";

	private const string KeyWearFeature = "wearFeatre";

	public const string KeyOilFeature = "oilPrevMaintFeature";

	private const string KeyOverhaulMiles = "overhaulMi";

	private const string KeyWearMultiplier = "wearMult";

	private const string KeyOilUseMultiplier = "oilUseMult";

	private const string KeyMapShowsSwitches = "mapShowsSwitches";

	private const string KeyTimetableFeature = "timetableFeature";

	public GameMode GameMode
	{
		get
		{
			if (!(_gameKeyValueObject == null))
			{
				return (GameMode)_gameKeyValueObject["mode"].IntValue;
			}
			return GameMode.Sandbox;
		}
		set
		{
			_gameKeyValueObject["mode"] = Value.Int((int)value);
		}
	}

	public string SetupId
	{
		get
		{
			return _gameKeyValueObject["setupId"].StringValue;
		}
		set
		{
			_gameKeyValueObject["setupId"] = Value.String(value);
		}
	}

	public string RailroadName
	{
		get
		{
			return _gameKeyValueObject["railroadName"].StringValue.Truncate(50);
		}
		set
		{
			_gameKeyValueObject["railroadName"] = Value.String(value);
		}
	}

	public string RailroadMark
	{
		get
		{
			return _gameKeyValueObject["railroadMark"].StringValue.Truncate(6);
		}
		set
		{
			_gameKeyValueObject["railroadMark"] = Value.String(value);
		}
	}

	public int Balance
	{
		get
		{
			if (!(_gameKeyValueObject == null))
			{
				return _gameKeyValueObject["balance"].IntValue;
			}
			return 0;
		}
		set
		{
			if (_gameKeyValueObject == null)
			{
				throw new Exception("Can't set balance without object");
			}
			if (Balance != value)
			{
				_gameKeyValueObject["balance"] = Value.Int(value);
			}
		}
	}

	public float TimeMultiplier
	{
		get
		{
			return _gameKeyValueObject["timeMultiplier"].FloatValueOrDefault(2f);
		}
		set
		{
			_gameKeyValueObject["timeMultiplier"] = Value.Float(value);
		}
	}

	public AccessLevel DefaultAccessLevel
	{
		get
		{
			Value value = _gameKeyValueObject["defaultAccessLevel"];
			if (!value.IsNull)
			{
				return (AccessLevel)value.IntValue;
			}
			return AccessLevel.Passenger;
		}
		set
		{
			_gameKeyValueObject["defaultAccessLevel"] = Value.Int((int)value);
		}
	}

	public bool AllowNewPlayers
	{
		get
		{
			Value value = _gameKeyValueObject["allowNewPlayers"];
			if (!value.IsNull)
			{
				return value.BoolValue;
			}
			return true;
		}
		set
		{
			_gameKeyValueObject["allowNewPlayers"] = Value.Bool(value);
		}
	}

	public string NewPlayerPasswordHash
	{
		get
		{
			return _gameKeyValueObject["passwordHash"].StringValue;
		}
		set
		{
			_gameKeyValueObject["passwordHash"] = Value.String(value);
		}
	}

	public bool HasNewPlayerPassword => !string.IsNullOrEmpty(NewPlayerPasswordHash);

	public bool TrainCrewMembershipRequired
	{
		get
		{
			return _gameKeyValueObject["trainCrewMembershipRequired"];
		}
		set
		{
			_gameKeyValueObject["trainCrewMembershipRequired"] = value;
		}
	}

	public bool TrainCrewMembershipManagedByTrainmaster
	{
		get
		{
			return _gameKeyValueObject["trainCrewMembershipManagedByTrainmaster"];
		}
		set
		{
			_gameKeyValueObject["trainCrewMembershipManagedByTrainmaster"] = value;
		}
	}

	public int LoanAmount
	{
		get
		{
			return _gameKeyValueObject["loanAmount"].IntValue;
		}
		set
		{
			_gameKeyValueObject["loanAmount"] = Value.Int(value);
		}
	}

	public GameDateTime? NextInterestDate
	{
		get
		{
			Value value = _gameKeyValueObject["loanNextInterestDate"];
			if (value.IsNull)
			{
				return null;
			}
			return new GameDateTime(value.FloatValue);
		}
		set
		{
			_gameKeyValueObject["loanNextInterestDate"] = ((!value.HasValue) ? Value.Null() : Value.Float((float)value.Value.TotalSeconds));
		}
	}

	public int LoanNextInterestOffset
	{
		get
		{
			return _gameKeyValueObject["loanNextInterestOffset"].IntValue;
		}
		set
		{
			_gameKeyValueObject["loanNextInterestOffset"] = Value.Int(value);
		}
	}

	public float UnbilledAutoEngineerRunDuration
	{
		get
		{
			return _gameKeyValueObject["unbilledRunDuration"].FloatValue;
		}
		set
		{
			_gameKeyValueObject["unbilledRunDuration"] = Value.Float(value);
		}
	}

	public int InterchangeServeHour
	{
		get
		{
			return _gameKeyValueObject["interchangeServeHour"].IntValueOrDefault(6);
		}
		set
		{
			_gameKeyValueObject["interchangeServeHour"] = Value.Int(value);
		}
	}

	public static bool CanWriteInterchangeShuffle => StateManager.CheckAuthorizedToChangeProperty("_game", "interchangeShuffle");

	public int InterchangeShuffle
	{
		get
		{
			return _gameKeyValueObject["interchangeShuffle"].IntValueOrDefault(0);
		}
		set
		{
			_gameKeyValueObject["interchangeShuffle"] = Value.Int(value);
		}
	}

	public int PassengerLimit
	{
		get
		{
			return _gameKeyValueObject["passengerLimit"].IntValueOrDefault(8);
		}
		set
		{
			_gameKeyValueObject["passengerLimit"] = Value.Int(value);
		}
	}

	public static bool CanWriteBrakeForce => StateManager.CheckAuthorizedToChangeProperty("_game", "brakeForce");

	public float? BrakeForce
	{
		get
		{
			Value value = _gameKeyValueObject["brakeForce"];
			if (!value.IsNull)
			{
				return value.FloatValue;
			}
			return null;
		}
		set
		{
			_gameKeyValueObject["brakeForce"] = (value.HasValue ? Value.Float(value.Value) : Value.Null());
		}
	}

	public bool WearFeature
	{
		get
		{
			return _gameKeyValueObject["wearFeatre"].BoolValueOrDefault(defaultValue: true);
		}
		set
		{
			_gameKeyValueObject["wearFeatre"] = value;
		}
	}

	public bool OilFeature
	{
		get
		{
			return _gameKeyValueObject["oilPrevMaintFeature"].BoolValueOrDefault(defaultValue: true);
		}
		set
		{
			_gameKeyValueObject["oilPrevMaintFeature"] = value;
		}
	}

	public bool TimetableFeature
	{
		get
		{
			return _gameKeyValueObject["timetableFeature"].BoolValueOrDefault(defaultValue: false);
		}
		set
		{
			_gameKeyValueObject["timetableFeature"] = value;
		}
	}

	public int OverhaulMiles
	{
		get
		{
			return _gameKeyValueObject["overhaulMi"].IntValueOrDefault(2500);
		}
		set
		{
			_gameKeyValueObject["overhaulMi"] = value;
		}
	}

	public float WearMultiplier
	{
		get
		{
			return _gameKeyValueObject["wearMult"].FloatValueOrDefault(1f);
		}
		set
		{
			_gameKeyValueObject["wearMult"] = value;
		}
	}

	public float OilUseMultiplier
	{
		get
		{
			return _gameKeyValueObject["oilUseMult"].FloatValueOrDefault(1f);
		}
		set
		{
			_gameKeyValueObject["oilUseMult"] = value;
		}
	}

	public bool MapShowsSwitches
	{
		get
		{
			return _gameKeyValueObject["mapShowsSwitches"].BoolValueOrDefault(defaultValue: true);
		}
		set
		{
			_gameKeyValueObject["mapShowsSwitches"] = value;
		}
	}

	public CrossingSignalSetting AICrossingSignal
	{
		get
		{
			return (CrossingSignalSetting)_gameKeyValueObject["aiCrossingSignal"].IntValueOrDefault(1);
		}
		set
		{
			_gameKeyValueObject["aiCrossingSignal"] = Value.Int((int)value);
		}
	}

	public bool AIPassengerStopEnable
	{
		get
		{
			return _gameKeyValueObject["aiPassStopEnable"].BoolValueOrDefault(defaultValue: true);
		}
		set
		{
			_gameKeyValueObject["aiPassStopEnable"] = Value.Bool(value);
		}
	}

	public int AIPassengerStopMinimumStopDuration
	{
		get
		{
			return _gameKeyValueObject["aiPassStopMinStopDur"].IntValueOrDefault(60);
		}
		set
		{
			_gameKeyValueObject["aiPassStopMinStopDur"] = Value.Int(value);
		}
	}

	public int AICallSignals
	{
		get
		{
			return _gameKeyValueObject["aiCallSignals"].IntValueOrDefault(1);
		}
		set
		{
			_gameKeyValueObject["aiCallSignals"] = value;
		}
	}

	public GameStorage(KeyValueObject keyValueObject)
	{
		_gameKeyValueObject = keyValueObject;
		StateManager.Shared.RegisterPropertyObject("_game", keyValueObject, this);
	}

	public void Dispose()
	{
		if (!(_gameKeyValueObject == null))
		{
			UnityEngine.Object.DestroyImmediate(_gameKeyValueObject);
			StateManager.Shared.UnregisterPropertyObject("_game");
		}
	}

	public IDisposable ObserveTimeMultiplier(Action<float> action, bool initial)
	{
		return _gameKeyValueObject.Observe("timeMultiplier", delegate
		{
			action(TimeMultiplier);
		}, initial);
	}

	public IDisposable ObserveNewPlayerPasswordHash(Action action, bool initial)
	{
		return _gameKeyValueObject.Observe("passwordHash", delegate
		{
			action();
		}, initial);
	}

	public IDisposable ObserveGameMode(Action<GameMode> action, bool initial)
	{
		return _gameKeyValueObject.Observe("mode", delegate
		{
			action(GameMode);
		}, initial);
	}

	public IDisposable ObserveWeatherId(Action<int> action)
	{
		return _gameKeyValueObject.Observe("weatherId", delegate(Value value)
		{
			action(value.IntValueOrDefault(TimeWeather.WeatherIdLookup["cloudy2"]));
		});
	}

	public IDisposable ObserveBrakeForce(Action<float?> action)
	{
		return _gameKeyValueObject.Observe("brakeForce", delegate(Value value)
		{
			action(value.IsNull ? ((float?)null) : new float?(value.FloatValue));
		});
	}

	public IDisposable ObserveBrakeForceHandbrake(Action<float?> action)
	{
		return _gameKeyValueObject.Observe("brakeForceHandbrake", delegate(Value value)
		{
			action(value.IsNull ? ((float?)null) : new float?(value.FloatValue));
		});
	}

	public IDisposable ObserveWearFeature(Action<bool> action, bool observeFirst = true)
	{
		return _gameKeyValueObject.Observe("wearFeatre", delegate
		{
			action(WearFeature);
		}, observeFirst);
	}

	public IDisposable ObserveOilFeature(Action<bool> action)
	{
		return _gameKeyValueObject.Observe("oilPrevMaintFeature", delegate
		{
			action(OilFeature);
		});
	}

	public IDisposable ObserveTimetableFeature(Action<bool> action, bool callInitial)
	{
		return _gameKeyValueObject.Observe("timetableFeature", delegate
		{
			action(TimetableFeature);
		}, callInitial);
	}

	public IDisposable ObserveOverhaulMiles(Action<int> action)
	{
		return _gameKeyValueObject.Observe("overhaulMi", delegate
		{
			action(OverhaulMiles);
		});
	}

	public IDisposable ObserveWearMultiplier(Action<float> action)
	{
		return _gameKeyValueObject.Observe("wearMult", delegate
		{
			action(WearMultiplier);
		});
	}

	public IDisposable ObserveOilUseMultiplier(Action<float> action)
	{
		return _gameKeyValueObject.Observe("oilUseMult", delegate
		{
			action(OilUseMultiplier);
		});
	}

	public IDisposable ObserveMapShowsSwitches(Action<bool> action, bool callInitial)
	{
		return _gameKeyValueObject.Observe("mapShowsSwitches", delegate(Value value)
		{
			action(value);
		}, callInitial);
	}

	public AuthorizationRequirementInfo AuthorizationRequirementForPropertyWrite(string key)
	{
		return key switch
		{
			"interchangeServeHour" => AuthorizationRequirement.MinimumLevelOfficer, 
			"interchangeShuffle" => AuthorizationRequirement.MinimumLevelOfficer, 
			"aiCrossingSignal" => AuthorizationRequirement.MinimumLevelTrainmaster, 
			"aiPassStopEnable" => AuthorizationRequirement.MinimumLevelTrainmaster, 
			"aiPassStopMinStopDur" => AuthorizationRequirement.MinimumLevelTrainmaster, 
			_ => AuthorizationRequirement.HostOnly, 
		};
	}
}
