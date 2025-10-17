using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using Game;
using HeathenEngineering.SteamworksIntegration;
using Helpers;
using Network;
using Network.Steam;
using Serilog;
using Steamworks;
using TMPro;
using UI.Common;
using UI.LazyScrollList;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu;

public class MultiplayerJoinMenu : MonoBehaviour, INavigationView
{
	public struct JoinInfo
	{
		public string Username;

		public CSteamID SteamLobbyId;

		public JoinInfo(CSteamID steamLobbyId, string username)
		{
			SteamLobbyId = steamLobbyId;
			Username = username;
		}
	}

	[SerializeField]
	private TMP_Text statusLabel;

	[SerializeField]
	private Button refreshButton;

	[SerializeField]
	private Button backButton;

	[SerializeField]
	private UI.LazyScrollList.LazyScrollList scrollList;

	[SerializeField]
	private TMP_InputField usernameField;

	[SerializeField]
	private TMP_InputField filterField;

	public Action<JoinInfo> OnJoin;

	private ClientLobbyHelper _clientLobbyHelper;

	private IReadOnlyCollection<LobbyData> _lobbies = (IReadOnlyCollection<LobbyData>)(object)Array.Empty<LobbyData>();

	public RectTransform RectTransform => GetComponent<RectTransform>();

	private void Awake()
	{
		backButton.onClick.AddListener(delegate
		{
			this.NavigationController().Pop();
		});
		refreshButton.onClick.AddListener(RefreshLobbies);
	}

	private void OnEnable()
	{
		_clientLobbyHelper = Multiplayer.ClientLobbyHelper;
		RefreshLobbies();
		usernameField.text = Preferences.MultiplayerClientUsername;
	}

	private void Rebuild()
	{
		IReadOnlyCollection<LobbyData> lobbies = _lobbies;
		int count = lobbies.Count;
		bool flag = !string.IsNullOrEmpty(filterField.text);
		List<object> list = ((IEnumerable<LobbyData>)lobbies).Select((Func<LobbyData, object>)((LobbyData l) => new LobbyRow.Info(l, JoinLobbyById))).ToList();
		string text = ((count == 0) ? (flag ? ("No railroads with exact reporting mark \"" + filterField.text + "\"") : "No railroads currently available.") : ((!flag) ? ("Showing " + count.Pluralize("railroad") + ".") : string.Format("Showing {0} of {1} {2}.", list.Count, count, "railroad".Pluralize(list.Count))));
		if (count == 50 && !flag)
		{
			text += " (Try searching!)";
		}
		statusLabel.text = text;
		scrollList.SetData(list);
		Debug.Log($"MultiplayerJoinMenu: Rebuild() with {count} lobbies");
	}

	private async void RefreshLobbies()
	{
		statusLabel.text = "Updating...";
		_lobbies = (IReadOnlyCollection<LobbyData>)(object)Array.Empty<LobbyData>();
		Rebuild();
		try
		{
			_lobbies = (IReadOnlyCollection<LobbyData>)(object)(await _clientLobbyHelper.FetchLobbies(filterField.text));
			Rebuild();
		}
		catch (Exception exception)
		{
			statusLabel.text = "Error finding games.";
			Log.Error(exception, "Error refreshing lobbies");
			Debug.LogException(exception);
		}
	}

	public void SearchFieldChanged()
	{
		RefreshLobbies();
	}

	private void JoinLobbyById(LobbyData lobby)
	{
		string text = StringSanitizer.SanitizeName(usernameField.text);
		if (string.IsNullOrEmpty(text))
		{
			Toast.Present("Please enter a player name to connect using.");
			return;
		}
		Preferences.MultiplayerClientUsername = text;
		OnJoin?.Invoke(new JoinInfo(lobby.SteamId, text));
	}

	public void WillAppear()
	{
	}

	public void WillDisappear()
	{
	}

	public void DidPop()
	{
	}
}
