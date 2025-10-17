using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game;
using Game.State;
using Model;
using TMPro;
using UI.Common;
using UnityEngine;

namespace UI.PlayerList;

public class PlayerListController : MonoBehaviour
{
	[SerializeField]
	private TMP_Text playerListHeaderText;

	[SerializeField]
	private PlayerRow playerRowTemplate;

	[SerializeField]
	private TrainCrewHeader trainCrewHeaderTemplate;

	[SerializeField]
	private RectTransform playerListContainer;

	private Coroutine _periodicUpdateCoroutine;

	private void Start()
	{
		playerRowTemplate.gameObject.SetActive(value: false);
		trainCrewHeaderTemplate.gameObject.SetActive(value: false);
	}

	private void OnEnable()
	{
		_periodicUpdateCoroutine = StartCoroutine(PeriodicUpdate());
	}

	private void OnDisable()
	{
		if (_periodicUpdateCoroutine != null)
		{
			StopCoroutine(_periodicUpdateCoroutine);
			_periodicUpdateCoroutine = null;
		}
	}

	private IEnumerator PeriodicUpdate()
	{
		while (true)
		{
			try
			{
				UpdateRemotePlayers();
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
			yield return new WaitForSecondsRealtime(0.5f);
		}
	}

	private void UpdateRemotePlayers()
	{
		if (!PlayersManager.PlayerId.IsValid)
		{
			return;
		}
		playerListContainer.DestroyChildrenExcept(new Component[2] { playerRowTemplate, trainCrewHeaderTemplate });
		int num = 0;
		PlayersManager playersManager = StateManager.Shared.PlayersManager;
		TrainController shared = TrainController.Shared;
		if (shared == null)
		{
			return;
		}
		Dictionary<PlayerId, IPlayer> players = new Dictionary<PlayerId, IPlayer>(16);
		foreach (IPlayer allPlayer in playersManager.AllPlayers)
		{
			players[allPlayer.PlayerId] = allPlayer;
		}
		HashSet<PlayerId> hashSet = players.Keys.ToHashSet();
		int count = players.Count;
		PlayerId myPlayerId = PlayersManager.PlayerId;
		List<TrainCrew> list = playersManager.TrainCrews.ToList();
		list.Sort(delegate(TrainCrew a, TrainCrew b)
		{
			bool flag = a.MemberPlayerIds.Contains(myPlayerId);
			bool flag2 = b.MemberPlayerIds.Contains(myPlayerId);
			if (flag != flag2)
			{
				if (!flag)
				{
					return 1;
				}
				return -1;
			}
			bool flag3 = a.MemberPlayerIds.Count > 0;
			bool flag4 = b.MemberPlayerIds.Count > 0;
			return (flag3 != flag4) ? ((!flag3) ? 1 : (-1)) : string.Compare(a.Name, b.Name, StringComparison.Ordinal);
		});
		foreach (TrainCrew item in list)
		{
			item.MemberPlayerIds.RemoveWhere((PlayerId playerId) => !players.ContainsKey(playerId));
			AddTrainCrewRow(item, shared);
			foreach (PlayerId memberPlayerId in item.MemberPlayerIds)
			{
				IPlayer player = playersManager.PlayerForId(memberPlayerId);
				if (player != null)
				{
					AddPlayerRow(player);
					hashSet.Remove(memberPlayerId);
					num++;
				}
			}
		}
		if (hashSet.Count > 0)
		{
			if (list.Count > 0)
			{
				AddHeaderRowForNoCrew(hashSet.Count);
			}
			foreach (PlayerId item2 in hashSet)
			{
				IPlayer player2 = playersManager.PlayerForId(item2);
				AddPlayerRow(player2);
				num++;
			}
		}
		playerListHeaderText.gameObject.SetActive(count > 0);
		string text = ((count == 1) ? "Player" : "Players");
		string text2 = ((list.Count == 1) ? "Train Crew" : "Train Crews");
		playerListHeaderText.text = $"{count} {text}, {list.Count} {text2}";
	}

	private void AddTrainCrewRow(TrainCrew trainCrew, TrainController trainController)
	{
		List<string> list = (from car in trainController.Cars
			where car.trainCrewId == trainCrew.Id
			select car.DisplayName into n
			orderby n
			select n).ToList();
		string text = string.Join(", ", list);
		int count = trainCrew.MemberPlayerIds.Count;
		TrainCrewHeader trainCrewHeader = UnityEngine.Object.Instantiate(trainCrewHeaderTemplate, playerListContainer);
		trainCrewHeader.gameObject.SetActive(value: true);
		trainCrewHeader.nameLabel.text = trainCrew.Name;
		trainCrewHeader.descriptionLabel.text = (list.Any() ? text : string.Format("{0} train crew {1}", count, (count == 1) ? "member" : "members"));
		trainCrewHeader.TrainCrewId = trainCrew.Id;
	}

	private void AddHeaderRowForNoCrew(int count)
	{
		TrainCrewHeader trainCrewHeader = UnityEngine.Object.Instantiate(trainCrewHeaderTemplate, playerListContainer);
		trainCrewHeader.gameObject.SetActive(value: true);
		trainCrewHeader.nameLabel.text = "Off Duty";
		trainCrewHeader.descriptionLabel.text = string.Format("{0} {1} not on a train crew.", count, (count == 1) ? "player is" : "players are");
	}

	private void AddPlayerRow(IPlayer player)
	{
		PlayerRow playerRow = UnityEngine.Object.Instantiate(playerRowTemplate, playerListContainer);
		playerRow.gameObject.SetActive(value: true);
		playerRow.nameLabel.text = player.Name;
		playerRow.trailingLabel.text = (player.IsRemote ? null : "(You)");
	}
}
