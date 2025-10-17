using System;
using System.Collections.Generic;
using System.IO;
using Analytics;
using Game.State;
using HeathenEngineering.SteamworksIntegration.API;
using Network;
using Serilog;
using TMPro;
using UI.Common;
using UnityEngine;

namespace UI.Menu;

public class MenuManager : MonoBehaviour
{
	[Header("References")]
	[SerializeField]
	private NavigationController navigationController;

	[SerializeField]
	private GlobalGameManager gameManager;

	[SerializeField]
	private TMP_Text versionLabel;

	[Header("Menu Prefabs")]
	[SerializeField]
	private SoftMenu softMenuPrefab;

	[SerializeField]
	private MainMenu mainMenu;

	[SerializeField]
	private NewGameMenu newGameMenu;

	[SerializeField]
	private LoadGameMenu loadGameMenu;

	[SerializeField]
	private MultiplayerHostMenu multiplayerHostMenu;

	[SerializeField]
	private MultiplayerJoinMenu multiplayerJoinMenu;

	[SerializeField]
	private PreferencesMenu preferencesMenu;

	[SerializeField]
	private CreditsMenu creditsMenu;

	private static SceneDescriptor MapSceneDescriptor => SceneDescriptor.BushnellWhittier;

	private void Start()
	{
		global::Analytics.Analytics.Post("ShowMainMenu");
		MainMenu view = MakeMainMenu();
		navigationController.Push(view);
		string versionString = GetVersionString();
		if (versionLabel != null)
		{
			versionLabel.text = versionString;
		}
	}

	private static string GetVersionString()
	{
		string text = "Steam Error";
		string text2 = null;
		try
		{
			text = App.Client.BuildId.ToString();
			text2 = App.Client.CurrentBetaName;
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Error getting build info from Steam");
		}
		string text3 = "Railroader " + Application.version + " (" + text + ")";
		if (!string.IsNullOrEmpty(text2))
		{
			text3 = text3 + " " + text2;
		}
		return text3;
	}

	private MainMenu MakeMainMenu()
	{
		MainMenu obj = UnityEngine.Object.Instantiate(mainMenu, base.transform, worldPositionStays: false);
		obj.OnMainMenuAction = delegate(MainMenu.MainMenuAction action)
		{
			switch (action)
			{
			case MainMenu.MainMenuAction.Singleplayer:
			{
				LoadGameMenu loadGameMenu = UnityEngine.Object.Instantiate(this.loadGameMenu);
				loadGameMenu.OnNewGame = (Action)Delegate.Combine(loadGameMenu.OnNewGame, (Action)delegate
				{
					ShowNewGameMenu(delegate(string saveName, NewGameSetup setup)
					{
						StartGameSinglePlayer(new GameSetup(saveName, setup));
					});
				});
				loadGameMenu.OnLoadGame = (Action<string>)Delegate.Combine(loadGameMenu.OnLoadGame, (Action<string>)delegate(string saveName)
				{
					StartGameSinglePlayer(new GameSetup(saveName));
				});
				navigationController.Push(loadGameMenu);
				break;
			}
			case MainMenu.MainMenuAction.Multiplayer:
			{
				SoftMenu view2 = MakeMultiplayerMenu();
				navigationController.Push(view2);
				break;
			}
			case MainMenu.MainMenuAction.Settings:
				navigationController.Push(UnityEngine.Object.Instantiate(preferencesMenu));
				break;
			case MainMenu.MainMenuAction.Help:
			{
				SoftMenu view = MakeHelpMenu();
				navigationController.Push(view);
				break;
			}
			case MainMenu.MainMenuAction.Editor:
			{
				SceneDescriptor bushnellWhittier = SceneDescriptor.BushnellWhittier;
				List<SceneDescriptor> list = new List<SceneDescriptor>
				{
					SceneDescriptor.Editor,
					bushnellWhittier,
					SceneDescriptor.EnvironmentEnviro
				};
				Launch(DummyGameSetup(), default(StartSingleplayerSetup), new GlobalGameManager.SceneLoadSetup(list, bushnellWhittier));
				break;
			}
			case MainMenu.MainMenuAction.Credits:
				navigationController.Push(UnityEngine.Object.Instantiate(creditsMenu));
				break;
			default:
				throw new ArgumentOutOfRangeException("action", action, null);
			}
		};
		return obj;
	}

	private static GameSetup DummyGameSetup()
	{
		return new GameSetup(null, new NewGameSetup("Atlantic Railway", "ATL", GameMode.Sandbox, null, null));
	}

	private SoftMenu MakeHelpMenu()
	{
		SoftMenu softMenu = UnityEngine.Object.Instantiate(softMenuPrefab);
		softMenu.Configure("Help");
		softMenu.AddButton("Discord", delegate
		{
			Application.OpenURL("https://discord.gg/7JXdc56Fpc");
		});
		softMenu.AddButton("YouTube", delegate
		{
			Application.OpenURL("https://www.youtube.com/@railroader");
		});
		softMenu.AddButton("Steam Community", delegate
		{
			Application.OpenURL("https://steamcommunity.com/app/1683150/discussions/");
		});
		softMenu.AddButton("Logs Folder", delegate
		{
			Application.OpenURL("file://" + Path.GetDirectoryName(Application.consoleLogPath));
		});
		return softMenu;
	}

	private SoftMenu MakeMultiplayerMenu()
	{
		SoftMenu softMenu = UnityEngine.Object.Instantiate(softMenuPrefab);
		softMenu.Configure("Multiplayer");
		softMenu.AddButton("Join", delegate
		{
			MultiplayerJoinMenu multiplayerJoinMenu = UnityEngine.Object.Instantiate(this.multiplayerJoinMenu);
			multiplayerJoinMenu.OnJoin = (Action<MultiplayerJoinMenu.JoinInfo>)Delegate.Combine(multiplayerJoinMenu.OnJoin, (Action<MultiplayerJoinMenu.JoinInfo>)delegate(MultiplayerJoinMenu.JoinInfo joinInfo)
			{
				StartJoin(joinInfo);
			});
			navigationController.Push(multiplayerJoinMenu);
		});
		softMenu.AddButton("Host", delegate
		{
			LoadGameMenu loadGameMenu = UnityEngine.Object.Instantiate(this.loadGameMenu);
			loadGameMenu.Configure("Select Game", "Continue");
			loadGameMenu.OnNewGame = (Action)Delegate.Combine(loadGameMenu.OnNewGame, (Action)delegate
			{
				ShowNewGameMenu(delegate(string saveName, NewGameSetup setup)
				{
					ShowHostMenu(new GameSetup(saveName, setup));
				});
			});
			loadGameMenu.OnLoadGame = (Action<string>)Delegate.Combine(loadGameMenu.OnLoadGame, (Action<string>)delegate(string saveName)
			{
				ShowHostMenu(new GameSetup(saveName));
			});
			navigationController.Push(loadGameMenu);
		});
		return softMenu;
	}

	private void ShowNewGameMenu(Action<string, NewGameSetup> completion)
	{
		NewGameMenu newGameMenu = UnityEngine.Object.Instantiate(this.newGameMenu);
		newGameMenu.OnContinue = (Action<string, NewGameSetup>)Delegate.Combine(newGameMenu.OnContinue, completion);
		navigationController.Push(newGameMenu);
	}

	private void ShowHostMenu(GameSetup setup)
	{
		MultiplayerHostMenu multiplayerHostMenu = UnityEngine.Object.Instantiate(this.multiplayerHostMenu);
		multiplayerHostMenu.OnStartServer = (Action<MultiplayerHostMenu.HostInfo>)Delegate.Combine(multiplayerHostMenu.OnStartServer, (Action<MultiplayerHostMenu.HostInfo>)delegate(MultiplayerHostMenu.HostInfo hostInfo)
		{
			StartServer(setup, hostInfo);
		});
		navigationController.Push(multiplayerHostMenu);
	}

	private void PostStartGameAnalytics(GameSetup? setup, string action)
	{
		Dictionary<string, object> dictionary;
		if (setup.HasValue && setup.GetValueOrDefault().NewGameSetup.HasValue)
		{
			NewGameSetup value = setup.Value.NewGameSetup.Value;
			dictionary = new Dictionary<string, object>
			{
				{
					"mode",
					value.Mode.ToString()
				},
				{ "progressionId", value.ProgressionId },
				{ "setupId", value.SetupId }
			};
		}
		else
		{
			dictionary = new Dictionary<string, object>();
		}
		dictionary["action"] = action;
		global::Analytics.Analytics.Post("StartGame", dictionary);
	}

	private void StartGameSinglePlayer(GameSetup setup)
	{
		PostStartGameAnalytics(setup, "startSingle");
		Launch(setup, default(StartSingleplayerSetup), MapSceneDescriptor);
	}

	private void StartServer(GameSetup setup, MultiplayerHostMenu.HostInfo info)
	{
		PostStartGameAnalytics(setup, "startMulti");
		Launch(setup, new StartMultiplayerHostSetup(info.LobbyName, info.LobbyType), MapSceneDescriptor);
	}

	private void StartJoin(MultiplayerJoinMenu.JoinInfo info)
	{
		PostStartGameAnalytics(null, "join");
		Launch(null, new JoinMultiplayerSetup(info.SteamLobbyId, null), MapSceneDescriptor);
	}

	private void Launch(GameSetup? gameSetup, INetworkSetup networkSetup, SceneDescriptor descriptor)
	{
		List<SceneDescriptor> list = new List<SceneDescriptor>
		{
			descriptor,
			SceneDescriptor.EnvironmentEnviro
		};
		Launch(gameSetup, networkSetup, new GlobalGameManager.SceneLoadSetup(list, descriptor));
	}

	private void Launch(GameSetup? gameSetup, INetworkSetup networkSetup, GlobalGameManager.SceneLoadSetup sceneLoadSetup)
	{
		gameManager.Launch(sceneLoadSetup, gameSetup, networkSetup);
	}
}
