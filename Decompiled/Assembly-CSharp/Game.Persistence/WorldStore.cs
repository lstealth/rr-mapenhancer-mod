using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Core;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Game.Messages;
using Game.State;
using KeyValue.Runtime;
using MessagePack;
using Model;
using Serilog;
using UnityEngine;

namespace Game.Persistence;

public static class WorldStore
{
	public struct SaveInfo
	{
		public string Name;

		public DateTime Date;
	}

	[MessagePackObject(false)]
	public struct World
	{
		[Key("version")]
		public int Version;

		[Key("snapshot")]
		public Snapshot Snapshot;

		[Key("playerStates")]
		public Dictionary<string, PlayerRecord> PlayerStates;

		[Key("carBodyPositions")]
		public Dictionary<string, Vector3[]> CarBodyPositions;

		[Key("ledgerEntries")]
		public List<SerializableLedgerEntry> LedgerEntries { get; set; }
	}

	private const string Extension = ".shortsave";

	private static string SavePath => Path.Combine(Application.persistentDataPath, "Saves");

	public static bool Exists(string saveName)
	{
		return File.Exists(PathForSaveName(saveName));
	}

	public static void Save(string saveName)
	{
		Log.Debug("Save: {saveName}", saveName);
		World value = CaptureWorldSnapshot();
		string savePath = SavePath;
		if (!Directory.Exists(savePath))
		{
			Log.Debug("Creating directory {savePath}", savePath);
			Directory.CreateDirectory(savePath);
		}
		string text = PathForSaveName(saveName);
		byte[] array = MessagePackSerializer.Serialize(value);
		File.WriteAllBytes(text, array);
		Log.Debug("Wrote {bytes} to {path}", array.Length, text);
	}

	public static void Load(string saveName)
	{
		string text = PathForSaveName(saveName);
		Log.Information("Loading from {path}", text);
		World world = MessagePackSerializer.Deserialize<World>(File.ReadAllBytes(text));
		Log.Information("Deserialized world v{worldVersion}, snapshot v{snapshotVersion}", world.Version, world.Snapshot.Version);
		ApplyWorld(world);
	}

	public static void InitializeNew()
	{
		Snapshot snapshot = Snapshot.Empty();
		snapshot.map = new Snapshot.Map(9f, 0, 1940, Vector3.zero, 0f);
		ApplyWorld(new World
		{
			Version = 0,
			PlayerStates = new Dictionary<string, PlayerRecord>(),
			Snapshot = snapshot
		});
	}

	private static void ApplyWorld(World world)
	{
		Snapshot snapshot = world.Snapshot;
		Migrate(snapshot);
		Migrate(world.PlayerStates);
		HostManager.Shared.LoadSnapshot(snapshot, world.PlayerStates, world.CarBodyPositions);
		StateManager.Shared.Ledger.Load(world.LedgerEntries);
	}

	private static void Migrate(Snapshot snapshot)
	{
		if (!snapshot.Properties.TryGetValue("_game", out var value))
		{
			return;
		}
		Value value2 = value.ToRuntime();
		if (value2["mode"].IntValue == 1 && snapshot.Properties.TryGetValue("_progression", out var value3))
		{
			value3["progression"] = new StringPropertyValue("ewh");
		}
		if (!value2["oilPrevMaintFeature"].BoolValueOrDefault(defaultValue: true))
		{
			foreach (string key2 in snapshot.Cars.Keys)
			{
				if (snapshot.Properties.TryGetValue(key2, out var value4))
				{
					value4.Remove("hotbox");
					value4.Remove("oiled");
				}
			}
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string>
		{
			{ "hm-hopper03", "hmr-hopper03" },
			{ "ls-260-g26", "ls-260-g25" },
			{ "lt-260-g26", "lt-260-g25" }
		};
		foreach (var (key, car2) in snapshot.Cars.ToList())
		{
			if (dictionary.TryGetValue(car2.prototypeId, out var value5))
			{
				Snapshot.Car value6 = car2;
				value6.prototypeId = value5;
				snapshot.Cars[key] = value6;
			}
		}
	}

	private static void Migrate(Dictionary<string, PlayerRecord> playerStates)
	{
		foreach (string item in playerStates.Keys.ToList())
		{
			if (!ulong.TryParse(item, out var _))
			{
				Log.Information("Removing player record with outdated key: {key} {name}", item, playerStates[item].Name);
				playerStates.Remove(item);
			}
		}
	}

	private static string PathForSaveName(string saveName)
	{
		return Path.Combine(SavePath, saveName + ".shortsave");
	}

	public static List<SaveInfo> FindSaveInfos()
	{
		if (!Directory.Exists(SavePath))
		{
			return new List<SaveInfo>();
		}
		List<FileInfo> list = (from fi in new DirectoryInfo(SavePath).GetFiles()
			where fi.Extension == ".shortsave"
			select fi).ToList();
		list.Sort((FileInfo a, FileInfo b) => b.LastWriteTime.CompareTo(a.LastWriteTime));
		return list.Select((FileInfo fi) => new SaveInfo
		{
			Name = Path.GetFileNameWithoutExtension(fi.Name),
			Date = fi.LastWriteTime
		}).ToList();
	}

	private static World CaptureWorldSnapshot()
	{
		Messenger.Default.Send(default(WorldWillSave));
		GameDateTime now = TimeWeather.Now;
		PositionRotation defaultSpawn = CameraSelector.shared.DefaultSpawn;
		Snapshot.Map map = new Snapshot.Map(now.Hours, now.Day, 0, defaultSpawn.Position, defaultSpawn.Rotation.eulerAngles.y);
		Snapshot snapshot = Snapshot.Empty();
		snapshot.map = map;
		TrainController.Shared.PopulateSnapshotForSave(ref snapshot, out var carBodyPositions, Car.SnapshotOption.None);
		Dictionary<string, PlayerRecord> playerPersistedStates = new Dictionary<string, PlayerRecord>();
		List<SerializableLedgerEntry> ledgerEntries = new List<SerializableLedgerEntry>();
		StateManager.Shared.PopulateSnapshotForSave(ref snapshot, ref playerPersistedStates, ref ledgerEntries);
		return new World
		{
			Version = 1,
			Snapshot = snapshot,
			PlayerStates = playerPersistedStates,
			LedgerEntries = ledgerEntries,
			CarBodyPositions = carBodyPositions
		};
	}

	public static void Clear(string saveName)
	{
		string path = PathForSaveName(saveName);
		if (File.Exists(path))
		{
			File.Delete(path);
		}
	}

	public static DateTime? TimestampForSave(string saveName)
	{
		FileInfo fileInfo = new FileInfo(PathForSaveName(saveName));
		if (!fileInfo.Exists)
		{
			return null;
		}
		return fileInfo.LastWriteTime;
	}

	public static string NewGameName()
	{
		string text = DateTime.Now.ToString("yyyy-MM-dd");
		if (!Exists(text))
		{
			return text;
		}
		int num = 1;
		string text2;
		while (true)
		{
			text2 = $"{text} {num}";
			if (!Exists(text2))
			{
				break;
			}
			num++;
		}
		return text2;
	}
}
