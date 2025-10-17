using System;
using System.Text;
using HeathenEngineering.SteamworksIntegration;
using Helpers;
using Steamworks;
using TMPro;
using UI.LazyScrollList;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu;

public class LobbyRow : MonoBehaviour, ILazyScrollListCell
{
	public class Info
	{
		public readonly string LobbyName;

		public readonly string Version;

		public readonly string CountText;

		public readonly bool AllowNewPlayers;

		public readonly bool HasPassword;

		public readonly LobbyData Lobby;

		public readonly Action<LobbyData> OnJoin;

		public Info(LobbyData lobby, Action<LobbyData> onJoin)
		{
			Lobby = lobby;
			OnJoin = onJoin;
			LobbyName = lobby.Name;
			Version = lobby["ver"];
			CountText = $"{lobby.Members.Length}";
			AllowNewPlayers = lobby["allowNew"] == "1";
			HasPassword = lobby["passworded"] == "1";
		}

		public Info(string lobbyName, Action<LobbyData> onJoin)
		{
			Lobby = CSteamID.Nil;
			LobbyName = lobbyName;
			OnJoin = onJoin;
			Version = null;
			CountText = "7";
		}
	}

	[SerializeField]
	private TMP_Text titleLabel;

	[SerializeField]
	private TMP_Text countLabel;

	[SerializeField]
	private TMP_Text flagsLabel;

	[SerializeField]
	private Button joinButton;

	private Info _info;

	public int ListIndex { get; private set; }

	public RectTransform RectTransform => GetComponent<RectTransform>();

	private void Awake()
	{
		joinButton.onClick.AddListener(delegate
		{
			_info.OnJoin?.Invoke(_info.Lobby);
		});
	}

	public void Configure(int listIndex, object obj)
	{
		ListIndex = listIndex;
		_info = (Info)obj;
		LobbyData lobby = _info.Lobby;
		_ = $"{lobby.Members.Length}";
		Configure(_info.LobbyName, _info.Version, _info.CountText, _info.AllowNewPlayers, _info.HasPassword);
	}

	private void Configure(string lobbyName, string version, string countText, bool allowNewPlayers, bool passworded)
	{
		StringBuilder stringBuilder = new StringBuilder("<noparse>" + lobbyName.StripHtml() + "</noparse>");
		bool flag = version == Application.version;
		if (!flag)
		{
			stringBuilder.Append(" <size=12>" + version + "</size>");
		}
		titleLabel.text = stringBuilder.ToString();
		flagsLabel.text = ((allowNewPlayers && !passworded) ? "<sprite name=\"Unlocked\">" : "<sprite name=\"Locked\">");
		countLabel.text = countText;
		joinButton.interactable = flag;
	}
}
