using System.Collections.Generic;
using Game.AccessControl;
using MessagePack;
using UnityEngine;

namespace Game.Messages;

[MessagePackObject(false)]
public struct Snapshot
{
	[MessagePackObject(false)]
	public struct Map
	{
		[Key(0)]
		public float TimeOfDay;

		[Key(1)]
		public int Day;

		[Key(2)]
		public int YearUnused;

		[Key(3)]
		public Vector3 DefaultSpawnPosition;

		[Key(4)]
		public float DefaultSpawnRotationY;

		public Map(float timeOfDay, int day, int yearUnused, Vector3 defaultSpawnPosition, float defaultSpawnRotation)
		{
			TimeOfDay = timeOfDay;
			Day = day;
			YearUnused = yearUnused;
			DefaultSpawnPosition = defaultSpawnPosition;
			DefaultSpawnRotationY = defaultSpawnRotation;
		}
	}

	[MessagePackObject(false)]
	public struct Player
	{
		[Key(0)]
		public string Name;

		[Key(1)]
		public AccessLevel AccessLevel;

		[Key(2)]
		public CharacterCustomization Customization;

		[Key(3)]
		public CharacterPosition Position;

		public Player(string name, AccessLevel accessLevel, CharacterCustomization customization, CharacterPosition position)
		{
			Name = name;
			AccessLevel = accessLevel;
			Customization = customization;
			Position = position;
		}
	}

	[MessagePackObject(false)]
	public struct CharacterCustomization
	{
		[Key(0)]
		public readonly Dictionary<string, IPropertyValue> Data;

		public CharacterCustomization(Dictionary<string, IPropertyValue> data)
		{
			Data = data;
		}
	}

	[MessagePackObject(false)]
	public struct CarSet
	{
		[Key(0)]
		public uint Id;

		[Key(1)]
		public List<string> CarIds;

		[Key(2)]
		public List<float> Positions;

		[Key(3)]
		public List<bool> FrontIsAs;

		public override string ToString()
		{
			return string.Format("CarSet(Id = {0}, CarIds = {1})", Id, string.Join(", ", CarIds));
		}
	}

	[MessagePackObject(false)]
	public struct Car
	{
		[Key(0)]
		public string id;

		[Key(1)]
		public string prototypeId;

		[Key(2)]
		public string roadNumber;

		[Key(3)]
		public bool UnusedKey3;

		[Key(4)]
		public TrackLocation Location;

		[Key(5)]
		public float velocity;

		[Key(6)]
		public string TrainCrewId;

		[Key(7)]
		public string ReportingMark;

		[Key(8)]
		public bool FrontIsA;

		[Key(9)]
		public string Bardo;

		public Car(string id, string prototypeId, string roadNumber, bool unusedKey3, TrackLocation location, float velocity, string trainCrewId, string reportingMark, bool frontIsA, string bardo)
		{
			this.id = id;
			this.prototypeId = prototypeId;
			this.roadNumber = roadNumber;
			UnusedKey3 = unusedKey3;
			Location = location;
			this.velocity = velocity;
			TrainCrewId = trainCrewId;
			ReportingMark = reportingMark;
			FrontIsA = frontIsA;
			Bardo = bardo;
		}

		public override string ToString()
		{
			return ReportingMark + " " + roadNumber + " (" + id + " " + prototypeId + ")";
		}
	}

	[MessagePackObject(false)]
	public struct TrackLocation
	{
		[Key(0)]
		public string segmentId;

		[Key(1)]
		public float distance;

		[Key(2)]
		public bool endIsA;

		public TrackLocation(string segmentId, float distance, bool endIsA)
		{
			this.segmentId = segmentId;
			this.distance = distance;
			this.endIsA = endIsA;
		}

		public override string ToString()
		{
			return $"{segmentId} {distance:F3} " + (endIsA ? "A" : "B");
		}
	}

	[MessagePackObject(false)]
	public struct TrainCrew
	{
		[Key(0)]
		public string Id;

		[Key(1)]
		public string Name;

		[Key(2)]
		public HashSet<string> MemberPlayerIds;

		[Key(3)]
		public string Description;

		[Key(4)]
		public string TimetableSymbol;

		public TrainCrew(string id, string name, HashSet<string> memberPlayerIds, string description, string timetableSymbol)
		{
			Id = id;
			Name = name;
			MemberPlayerIds = memberPlayerIds;
			Description = description;
			TimetableSymbol = timetableSymbol;
		}
	}

	[MessagePackObject(false)]
	public struct TurntableState
	{
		[Key(0)]
		public float Angle;

		[Key(1)]
		public int? StopIndex;

		public TurntableState(float angle, int? stopIndex)
		{
			Angle = angle;
			StopIndex = stopIndex;
		}
	}

	[Key("version")]
	public int Version;

	[Key("players")]
	public Dictionary<string, Player> players;

	[Key("cars")]
	public Dictionary<string, Car> Cars;

	[Key("carSets")]
	public Dictionary<uint, CarSet> CarSets;

	[Key("carAir")]
	public List<BatchCarAirUpdate> CarAir;

	[Key("thrownSwitchIds")]
	public HashSet<string> thrownSwitchIds;

	[Key("properties")]
	public Dictionary<string, Dictionary<string, IPropertyValue>> Properties;

	[Key("trainCrews")]
	public Dictionary<string, TrainCrew> TrainCrews;

	[Key("map")]
	public Map map;

	[Key("switchLists")]
	public Dictionary<string, SwitchList> SwitchLists;

	[Key("turntables")]
	public Dictionary<string, TurntableState> Turntables;

	public Snapshot(int version, Dictionary<string, Player> players, Dictionary<string, Car> cars, Dictionary<uint, CarSet> carSets, List<BatchCarAirUpdate> carAir, HashSet<string> thrownSwitchIds, Dictionary<string, Dictionary<string, IPropertyValue>> properties, Dictionary<string, TrainCrew> trainCrews, Map map, Dictionary<string, SwitchList> switchLists, Dictionary<string, TurntableState> turntables)
	{
		Version = version;
		this.players = players;
		Cars = cars;
		CarSets = carSets;
		CarAir = carAir;
		this.thrownSwitchIds = thrownSwitchIds;
		Properties = properties;
		TrainCrews = trainCrews;
		this.map = map;
		SwitchLists = switchLists;
		Turntables = turntables;
	}

	public static Snapshot Empty()
	{
		return new Snapshot(1, new Dictionary<string, Player>(), new Dictionary<string, Car>(), new Dictionary<uint, CarSet>(), new List<BatchCarAirUpdate>(), new HashSet<string>(), new Dictionary<string, Dictionary<string, IPropertyValue>>(), new Dictionary<string, TrainCrew>(), default(Map), new Dictionary<string, SwitchList>(), new Dictionary<string, TurntableState>());
	}
}
