using System;
using Game;
using Network.Steam;
using UI.Builder;
using UI.Common;

namespace UI.Menu;

public class MultiplayerHostMenu : BuilderMenuBase
{
	public struct HostInfo
	{
		public string LobbyName;

		public string Username;

		public string Password;

		public readonly LobbyType LobbyType;

		public HostInfo(string lobbyName, string username, string password, LobbyType lobbyType)
		{
			LobbyName = lobbyName;
			Username = username;
			Password = password;
			LobbyType = lobbyType;
		}
	}

	private HostInfo _hostInfo;

	public Action<HostInfo> OnStartServer { get; set; }

	protected override void BuildPanelContent(UIPanelBuilder builder)
	{
		builder.AddField("Public Game Name", builder.AddInputField(_hostInfo.LobbyName, delegate(string n)
		{
			_hostInfo.LobbyName = n;
		}));
		builder.AddField("Your Player Name", builder.AddInputField(_hostInfo.Username, delegate(string n)
		{
			_hostInfo.Username = n;
		}));
		builder.AddExpandingVerticalSpacer();
		builder.HStack(delegate(UIPanelBuilder uIPanelBuilder)
		{
			uIPanelBuilder.AddButton("Back", delegate
			{
				this.NavigationController().Pop();
			});
			uIPanelBuilder.Spacer().FlexibleWidth(1f);
			uIPanelBuilder.AddButton("Start Server", StartServer);
		});
	}

	private void Awake()
	{
		_hostInfo = new HostInfo(Preferences.MultiplayerLobbyName, Preferences.MultiplayerClientUsername, null, LobbyType.Public);
	}

	private void StartServer()
	{
		if (string.IsNullOrEmpty(_hostInfo.LobbyName) || _hostInfo.LobbyName.Length < 3)
		{
			Toast.Present("Please enter a longer public name for the game.");
			return;
		}
		if (string.IsNullOrEmpty(_hostInfo.Username))
		{
			Toast.Present("Please enter a player name to connect using.");
			return;
		}
		Preferences.MultiplayerLobbyName = _hostInfo.LobbyName;
		Preferences.MultiplayerLobbyType = (int)_hostInfo.LobbyType;
		Preferences.MultiplayerClientUsername = _hostInfo.Username;
		OnStartServer?.Invoke(_hostInfo);
	}
}
