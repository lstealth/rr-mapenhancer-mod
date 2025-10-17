using System;
using System.Collections.Generic;
using System.Linq;
using Core.Diagnostics;
using GalaSoft.MvvmLight.Messaging;
using Game;
using Game.AccessControl;
using Game.Events;
using Game.Messages;
using Game.State;
using JetBrains.Annotations;
using KeyValue.Runtime;
using Network;
using Serilog;
using UnityEngine;

namespace Model.Ops.Timetable;

public class TimetableController : GameBehaviour
{
	public struct TimetableDocument
	{
		public string Source;

		public GameDateTime Modified;

		public string Author;

		public TimetableDocument(string source, GameDateTime modified, string author)
		{
			Source = source;
			Modified = modified;
			Author = author;
		}

		public TimetableDocument(Value value)
		{
			Source = value["timetable"];
			Modified = new GameDateTime(value["modified"].IntValue);
			Author = value["author"];
		}

		public Value ToValue()
		{
			return Value.Dictionary(new Dictionary<string, Value>
			{
				{ "timetable", Source },
				{
					"modified",
					(int)Modified.TotalSeconds
				},
				{ "author", Author }
			});
		}
	}

	public List<TimetableBranch> branches;

	private KeyValueObject _keyValueObject;

	private bool _isInitialObservation = true;

	private Dictionary<string, TimetableStation> _timetableCodeToStation;

	private static TimetableController _shared;

	private IDisposable _timetableObserver;

	private IDisposable _featureObserver;

	private const string KeyValueIdentifier = "timetable";

	private const string KeyCurrent = "current";

	public static TimetableController Shared
	{
		get
		{
			if (_shared == null)
			{
				_shared = UnityEngine.Object.FindObjectOfType<TimetableController>();
			}
			return _shared;
		}
	}

	public Timetable Current { get; private set; }

	public Timetable CurrentRaw { get; private set; }

	public TimetableDocument CurrentDocument { get; private set; }

	public bool HasPassengerTrains { get; private set; }

	public bool HasError { get; private set; }

	public static bool CanEdit => StateManager.CheckAuthorizedToSendMessage(new SetTimetable(""));

	protected override void OnEnable()
	{
		_keyValueObject = base.gameObject.AddComponent<KeyValueObject>();
		StateManager.Shared.RegisterPropertyObject("timetable", _keyValueObject, AuthorizationRequirement.HostOnly);
		base.OnEnable();
	}

	protected override void OnDisable()
	{
		if (StateManager.Shared != null)
		{
			StateManager.Shared.UnregisterPropertyObject("timetable");
		}
		_timetableObserver?.Dispose();
		_featureObserver?.Dispose();
		base.OnDisable();
	}

	protected override void OnEnableWithProperties()
	{
		_timetableObserver = _keyValueObject.Observe("current", UpdateTimetable);
		_featureObserver = StateManager.Shared.Storage.ObserveTimetableFeature(delegate
		{
			UpdateTimetable(_keyValueObject["current"]);
		}, callInitial: false);
	}

	public IReadOnlyList<TimetableStation> GetAllStations(TimetableBranch branch = null, bool includeDisabled = false, bool includeDuplicates = true)
	{
		List<TimetableBranch> source = ((branch != null) ? new List<TimetableBranch> { branch } : branches);
		return (from ts in source.SelectMany((TimetableBranch br) => br.stations)
			where includeDisabled || ts.IsEnabled
			where includeDuplicates || !ts.IsBranchJunctionDuplicate
			select ts).Distinct().ToList();
	}

	public bool TryRead(string document, out Timetable output, [CanBeNull] IDiagnosticCollector diagnostics)
	{
		List<string> validStationCodes = (from ts in GetAllStations(null, includeDisabled: true)
			select ts.code).ToList();
		return TimetableReader.TryRead(document, validStationCodes, out output, diagnostics);
	}

	private void FilterForUse(Timetable input)
	{
		IReadOnlyList<TimetableStation> stations = GetAllStations();
		HashSet<string> hashSet = new HashSet<string>();
		string key;
		Timetable.Train value;
		foreach (KeyValuePair<string, Timetable.Train> train3 in input.Trains)
		{
			train3.Deconstruct(out key, out value);
			string item = key;
			Timetable.Train train = value;
			for (int num = train.Entries.Count - 1; num >= 0; num--)
			{
				if (!StationsInclude(train.Entries[num].Station))
				{
					train.Entries.RemoveAt(num);
				}
			}
			if (train.Entries.Count <= 1)
			{
				hashSet.Add(item);
			}
		}
		foreach (string item2 in hashSet)
		{
			input.Trains.Remove(item2);
		}
		foreach (KeyValuePair<string, Timetable.Train> train4 in input.Trains)
		{
			train4.Deconstruct(out key, out value);
			Timetable.Train train2 = value;
			for (int i = 0; i < train2.Entries.Count; i++)
			{
				if (!StationsInclude(train2.Entries[i].Station))
				{
					train2.Entries.RemoveAt(i);
				}
			}
		}
		bool StationsInclude(string code)
		{
			foreach (TimetableStation item3 in stations)
			{
				if (item3.code == code)
				{
					return true;
				}
			}
			return false;
		}
	}

	private void UpdateTimetable(Value documentValue)
	{
		if (StateManager.IsHost && documentValue.Type == KeyValue.Runtime.ValueType.String)
		{
			TimetableDocument timetableDocument = new TimetableDocument
			{
				Source = documentValue,
				Author = "(Migrated)",
				Modified = TimeWeather.Now
			};
			documentValue = timetableDocument.ToValue();
		}
		if (!StateManager.Shared.Storage.TimetableFeature)
		{
			documentValue = null;
		}
		CurrentDocument = new TimetableDocument(documentValue);
		StringDiagnosticCollector stringDiagnosticCollector = new StringDiagnosticCollector();
		Timetable output;
		if (string.IsNullOrEmpty(CurrentDocument.Source))
		{
			Current = null;
			HasError = false;
		}
		else if (TryRead(CurrentDocument.Source, out output, stringDiagnosticCollector))
		{
			CurrentRaw = output;
			output = output.ToAbsolute();
			FilterForUse(output);
			Current = output;
			HasError = false;
		}
		else
		{
			if (_isInitialObservation)
			{
				Console.Log($"Timetable contains error(s): {stringDiagnosticCollector}");
			}
			Current = null;
			HasError = true;
		}
		HasPassengerTrains = Current != null && Current.Trains.Count > 0 && Current.Trains.Any((KeyValuePair<string, Timetable.Train> t) => t.Value.TrainType == Timetable.TrainType.Passenger);
		Messenger.Default.Send(default(TimetableDidChange));
		if (stringDiagnosticCollector.Any)
		{
			Log.Information("Timetable Diagnostics: {d}", stringDiagnosticCollector);
		}
		_isInitialObservation = false;
	}

	public bool TryGetTrainForTrainCrew(TrainCrew trainCrew, out Timetable.Train timetableTrain)
	{
		timetableTrain = null;
		if (trainCrew == null || string.IsNullOrEmpty(trainCrew.TimetableSymbol))
		{
			return false;
		}
		return TryGetTrainForSymbol(trainCrew.TimetableSymbol, out timetableTrain);
	}

	public bool TryGetTrainForSymbol(string timetableTrainSymbol, out Timetable.Train timetableTrain)
	{
		Timetable current = Current;
		if (current == null)
		{
			timetableTrain = null;
			return false;
		}
		return current.Trains.TryGetValue(timetableTrainSymbol, out timetableTrain);
	}

	public bool TryGetTrainForTrainCrewId(string trainCrewId, out Timetable.Train timetableTrain)
	{
		Timetable current = Current;
		if (current == null)
		{
			timetableTrain = null;
			return false;
		}
		try
		{
			timetableTrain = null;
			if (current.Trains == null)
			{
				return false;
			}
			if (string.IsNullOrEmpty(trainCrewId) || !StateManager.Shared.PlayersManager.TrainCrewForId(trainCrewId, out var trainCrew))
			{
				return false;
			}
			if (!current.Trains.TryGetValue(trainCrew.TimetableSymbol, out timetableTrain))
			{
				return false;
			}
			return timetableTrain.Entries != null && timetableTrain.Entries.Count > 0;
		}
		catch
		{
			timetableTrain = null;
			return false;
		}
		finally
		{
		}
	}

	public void SetCurrent(string content)
	{
		StateManager.ApplyLocal(new SetTimetable(content));
	}

	private void HostSetCurrent(string content, IPlayer sender)
	{
		if (!StateManager.IsHost)
		{
			throw new InvalidOperationException("Only host can set timetable");
		}
		Log.Information("Set Timetable: {sender} {content}", sender, content);
		TimetableDocument timetableDocument = new TimetableDocument
		{
			Source = content,
			Author = sender.Name,
			Modified = TimeWeather.Now
		};
		_keyValueObject["current"] = timetableDocument.ToValue();
		Multiplayer.Broadcast($"{Hyperlink.To(sender.PlayerId)} has updated the {Hyperlink.To(new EntityReference(EntityType.Timetable, null))}.");
	}

	public bool TryGetPassengerStop(string stationCode, out PassengerStop passengerStop)
	{
		passengerStop = null;
		if (!TryGetStation(stationCode, out var station))
		{
			return false;
		}
		passengerStop = station.passengerStop;
		return passengerStop != null;
	}

	public bool TryGetStation(string stationCode, out TimetableStation station)
	{
		if (_timetableCodeToStation == null)
		{
			_timetableCodeToStation = new Dictionary<string, TimetableStation>();
			foreach (TimetableStation allStation in GetAllStations(null, includeDisabled: true))
			{
				if (!_timetableCodeToStation.ContainsKey(allStation.code))
				{
					_timetableCodeToStation[allStation.code] = allStation;
				}
			}
		}
		return _timetableCodeToStation.TryGetValue(stationCode, out station);
	}

	public bool TryGetTimingForStations(string fromStationCode, string toStationCode, out int minutesBetweenFast, out int minutesBetweenSlow)
	{
		minutesBetweenFast = 0;
		minutesBetweenSlow = 0;
		if (!TryGetStation(fromStationCode, out var station) || !TryGetStation(toStationCode, out var station2))
		{
			return false;
		}
		if (!TryGetCommonBranch(station, station2, out var branchOut, out var indexA, out var indexB))
		{
			Log.Debug("Couldn't find common branch from {a} to {b}", fromStationCode, toStationCode);
			return false;
		}
		if (indexA > indexB)
		{
			int num = indexB;
			int num2 = indexA;
			indexA = num;
			indexB = num2;
		}
		float num3 = 0f;
		for (int i = indexA; i < indexB; i++)
		{
			num3 += (float)branchOut.stations[i].traverseTimeToNext;
		}
		minutesBetweenFast = Mathf.CeilToInt(num3 / 60f);
		minutesBetweenSlow = Mathf.CeilToInt(1.2f * num3 / 60f);
		return true;
	}

	private bool TryGetCommonBranch(TimetableStation stationA, TimetableStation stationB, out TimetableBranch branchOut, out int indexA, out int indexB)
	{
		foreach (TimetableBranch branch in branches)
		{
			indexA = branch.stations.FindIndex((TimetableStation s) => s.code == stationA.code);
			indexB = branch.stations.FindIndex((TimetableStation s) => s.code == stationB.code);
			if (indexA >= 0 && indexB >= 0)
			{
				branchOut = branch;
				return true;
			}
		}
		indexA = -1;
		indexB = -1;
		branchOut = null;
		return false;
	}

	public void HandleSetTimetable(string source, IPlayer sender)
	{
		HostSetCurrent(source, sender);
	}
}
