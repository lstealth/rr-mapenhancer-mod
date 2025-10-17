using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Game;
using Game.AccessControl;
using Game.Events;
using Game.Messages;
using Game.Persistence;
using Game.State;
using HeathenEngineering.SteamworksIntegration;
using JetBrains.Annotations;
using Serilog;
using UI.Builder;
using UI.Map;
using UnityEngine;

namespace UI.CompanyWindow;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct EmployeesPanelBuilder
{
	private class RecordsPanelItem
	{
		public readonly PlayerId PlayerId;

		[CanBeNull]
		public IPlayer Player;

		public PlayerRecord Record;

		public string Name
		{
			get
			{
				if (Player != null)
				{
					return Player.Name;
				}
				return Record.Name;
			}
		}

		public RecordsPanelItem(PlayerId playerId, IPlayer player, PlayerRecord record)
		{
			PlayerId = playerId;
			Player = player;
			Record = record;
		}
	}

	private static readonly List<AccessLevel> AccessLevelOptions = new List<AccessLevel>
	{
		AccessLevel.Banned,
		AccessLevel.Passenger,
		AccessLevel.Crew,
		AccessLevel.Dispatcher,
		AccessLevel.Trainmaster,
		AccessLevel.Officer,
		AccessLevel.President
	};

	public static void Build(UIPanelBuilder builder, UIState<string> selectedPlayerId)
	{
		builder.RebuildOnEvent<PlayersDidChange>();
		builder.RebuildOnEvent<PlayerRecordsDidChange>();
		builder.RebuildOnEvent<AccessLevelDidChange>();
		StateManager shared = StateManager.Shared;
		PlayersManager playersManager = shared.PlayersManager;
		PlayerRecordsClientManager playerRecordsClientManager = shared.PlayerRecordsClientManager;
		if (playerRecordsClientManager != null)
		{
			BuildRecordsPanel(builder, playersManager, playerRecordsClientManager, selectedPlayerId);
		}
		else
		{
			BuildPlayerListPanel(builder, playersManager, selectedPlayerId);
		}
	}

	private static void BuildPlayerListPanel(UIPanelBuilder builder, PlayersManager playersManager, UIState<string> selectedPlayerId)
	{
		List<UIPanelBuilder.ListItem<IPlayer>> data = (from player in playersManager.AllPlayers
			orderby player.Name
			select new UIPanelBuilder.ListItem<IPlayer>(player.PlayerId.String, player, null, player.Name)).ToList();
		builder.AddListDetail(data, selectedPlayerId, delegate(UIPanelBuilder builder2, IPlayer player)
		{
			if (player == null)
			{
				builder2.AddLabel("Please select a player.");
			}
			else
			{
				builder2.AddTitle(player.Name, null);
				BuildTrainCrewDropdown(builder2, player.PlayerId);
				if (playersManager.TryGetAccessLevel(player.PlayerId, out var accessLevel))
				{
					builder2.AddField("Role", accessLevel.ToString());
				}
				BuildPlayerActions(builder2, player);
			}
		});
	}

	private static void BuildRecordsPanel(UIPanelBuilder builder, PlayersManager playersManager, PlayerRecordsClientManager recordsManager, UIState<string> selectedPlayerId)
	{
		List<UIPanelBuilder.ListItem<RecordsPanelItem>> list = new List<UIPanelBuilder.ListItem<RecordsPanelItem>>();
		List<UIPanelBuilder.ListItem<RecordsPanelItem>> list2 = new List<UIPanelBuilder.ListItem<RecordsPanelItem>>();
		List<UIPanelBuilder.ListItem<RecordsPanelItem>> list3 = new List<UIPanelBuilder.ListItem<RecordsPanelItem>>();
		Dictionary<PlayerId, IPlayer> dictionary = playersManager.AllPlayers.ToDictionary((IPlayer p) => p.PlayerId, (IPlayer p) => p);
		foreach (KeyValuePair<PlayerId, PlayerRecord> playerRecord in recordsManager.PlayerRecords)
		{
			playerRecord.Deconstruct(out var key, out var value);
			PlayerId playerId = key;
			PlayerRecord record = value;
			string identifier = playerId.String;
			if (dictionary.TryGetValue(playerId, out var value2))
			{
				RecordsPanelItem value3 = new RecordsPanelItem(playerId, value2, record);
				list.Add(new UIPanelBuilder.ListItem<RecordsPanelItem>(identifier, value3, "Online", value2.Name));
			}
			else if (record.AccessLevel == AccessLevel.Banned)
			{
				RecordsPanelItem value4 = new RecordsPanelItem(playerId, null, record);
				list3.Add(new UIPanelBuilder.ListItem<RecordsPanelItem>(identifier, value4, "Banned", record.Name));
			}
			else
			{
				RecordsPanelItem value5 = new RecordsPanelItem(playerId, null, record);
				list2.Add(new UIPanelBuilder.ListItem<RecordsPanelItem>(identifier, value5, "Offline", record.Name));
			}
		}
		list.Sort();
		list2.Sort();
		list3.Sort();
		List<UIPanelBuilder.ListItem<RecordsPanelItem>> data = list.Concat(list2).Concat(list3).ToList();
		builder.AddListDetail(data, selectedPlayerId, delegate(UIPanelBuilder builder2, RecordsPanelItem item)
		{
			if (item == null)
			{
				builder2.AddLabel("Please select a player.");
			}
			else
			{
				BuildDetail(builder2, item);
			}
		});
	}

	private static void BuildDetail(UIPanelBuilder builder, RecordsPanelItem item)
	{
		builder.AddTitle(item.Name, null);
		IEnumerable<string> accessLevelStrings = AccessLevelOptions.Select((AccessLevel al) => al.ToString());
		int accessLevelIndex = AccessLevelOptions.IndexOf(item.Record.AccessLevel);
		_ = StateManager.Shared.PlayersManager;
		builder.AddSection("About", delegate(UIPanelBuilder builder2)
		{
			BuildTrainCrewDropdown(builder2, item.PlayerId);
			builder2.AddField("Last Connected", item.Record.LastConnected.ToLocalTime().ToString(CultureInfo.CurrentCulture));
			RemovePlayerRecord removePlayerRecordMessage = new RemovePlayerRecord(item.PlayerId.String);
			if (StateManager.CheckAuthorizedToSendMessage(removePlayerRecordMessage) && item.Player == null)
			{
				builder2.AddField("", builder2.AddButton("Remove Record", delegate
				{
					StateManager.ApplyLocal(removePlayerRecordMessage);
				}).RectTransform);
			}
		});
		builder.AddSection("Access", delegate(UIPanelBuilder uIPanelBuilder)
		{
			if (accessLevelIndex < 0)
			{
				uIPanelBuilder.AddField("Role", $"Unexpected: {item.Record.AccessLevel}");
			}
			else
			{
				RectTransform control = uIPanelBuilder.AddDropdown(accessLevelStrings.ToList(), accessLevelIndex, delegate(int newIndex)
				{
					AccessLevel accessLevel = AccessLevelOptions[newIndex];
					Log.Debug("Request Set Access Level: {recordKey} {newAccessLevel}", item.PlayerId, accessLevel);
					StateManager.ApplyLocal(new RequestSetAccessLevel(item.PlayerId.String, accessLevel));
					LeanTween.delayedCall(1f, ((UIPanelBuilder)uIPanelBuilder).Rebuild);
				});
				uIPanelBuilder.AddField("Role", control);
			}
			uIPanelBuilder.AddField("Since", item.Record.AccessLevelChanged.ToLocalTime().ToString(CultureInfo.CurrentCulture));
		});
		builder.AddSection("Steam Data", delegate(UIPanelBuilder uIPanelBuilder)
		{
			uIPanelBuilder.AddField("Steam ID", item.Record.SteamId.ToString("D"));
			uIPanelBuilder.AddField("Steam Name", SteamNameForId(item.Record.SteamId));
		});
		if (item.Player != null)
		{
			BuildPlayerActions(builder, item.Player);
		}
	}

	private static void BuildTrainCrewDropdown(UIPanelBuilder builder, PlayerId playerId)
	{
		string trainCrewId = StateManager.Shared.PlayersManager.TrainCrewIdFor(playerId);
		builder.AddTrainCrewDropdown("Assign the Train Crew for this player.", trainCrewId, StateManager.CheckAuthorizedToSendMessage(new RequestSetTrainCrewMembership(playerId.String, null, join: true)), delegate(string selectedTrainCrewId)
		{
			StateManager.ApplyLocal(new RequestSetTrainCrewMembership(playerId.String, selectedTrainCrewId, join: true));
		});
	}

	private static void BuildPlayerActions(UIPanelBuilder builder, IPlayer player)
	{
		builder.AddSection("Actions", delegate(UIPanelBuilder uIPanelBuilder)
		{
			uIPanelBuilder.ButtonStrip(delegate(UIPanelBuilder uIPanelBuilder2)
			{
				uIPanelBuilder2.AddButton("Show", delegate
				{
					CameraSelector.shared.JumpToPoint(player.GamePosition, Quaternion.identity, CameraSelector.CameraIdentifier.Strategy);
				});
				uIPanelBuilder2.AddButton("Show on Map", delegate
				{
					MapWindow.Show(player.GamePosition);
				});
			});
		});
	}

	private static string SteamNameForId(ulong steamId)
	{
		return UserData.Get(steamId).Name;
	}
}
