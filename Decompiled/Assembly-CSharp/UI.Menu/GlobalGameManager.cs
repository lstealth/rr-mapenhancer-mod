using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Game.State;
using HeathenEngineering;
using HeathenEngineering.SteamworksIntegration;
using HeathenEngineering.SteamworksIntegration.API;
using Network;
using Serilog;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI.Menu;

[CreateAssetMenu(fileName = "Global Game Manager", menuName = "Train Game/Global Game Manager", order = 0)]
public class GlobalGameManager : ScriptableObject
{
	private enum State
	{
		Menu,
		Game,
		ReturningToMainMenu
	}

	public readonly struct SceneLoadSetup
	{
		public readonly List<SceneDescriptor> Descriptors;

		public readonly SceneDescriptor Active;

		public SceneLoadSetup(List<SceneDescriptor> list, SceneDescriptor active)
		{
			Descriptors = list;
			Active = active;
		}
	}

	private PersistentLoader _persistentLoader;

	private State _state;

	private List<SceneDescriptor> _loadedMapSceneDescriptors;

	public void OnPersistentLoaderAwake(PersistentLoader persistentLoader)
	{
		_persistentLoader = persistentLoader;
		LobbyData lobbyData = CommandLine.GetSteamLobbyInvite();
		if (lobbyData.IsValid)
		{
			JoinLobbyGameCold(lobbyData.SteamId);
			return;
		}
		_persistentLoader.ShowLoadingScreen(show: false);
		SceneDescriptor.MainMenu.LoadAsync(LoadSceneMode.Additive);
		Overlay.Client.EventGameLobbyJoinRequested.AddListener(delegate(LobbyData lobbyData2, UserData userData)
		{
			JoinLobbyGame(lobbyData2.SteamId);
		});
	}

	private async Task JoinLobbyGameCold(CSteamID lobbySteamId)
	{
		await Task.Delay(500);
		JoinLobbyGame(lobbySteamId);
	}

	private void JoinLobbyGame(CSteamID lobbyId)
	{
		Log.Debug("JoinLobbyGame({lobbyId})", lobbyId);
		if (_state != State.Menu)
		{
			Log.Error("Can't accept invite, already in game. {state}", _state);
			return;
		}
		List<SceneDescriptor> list = new List<SceneDescriptor>
		{
			SceneDescriptor.BushnellWhittier,
			SceneDescriptor.EnvironmentEnviro
		};
		string password = "";
		Launch(new SceneLoadSetup(list, list[0]), null, new JoinMultiplayerSetup(lobbyId, password));
	}

	public async void Launch(SceneLoadSetup sceneLoadSetup, GameSetup? gameSetup, INetworkSetup networkSetup)
	{
		try
		{
			_state = State.Game;
			_persistentLoader.ShowLoadingScreen(show: true);
			await _LoadMap(sceneLoadSetup, gameSetup, networkSetup);
			_persistentLoader.ShowLoadingScreen(show: false);
		}
		catch (Exception exception)
		{
			_persistentLoader.ShowLoadingScreen(show: false);
			Debug.LogError("Error loading map:");
			Debug.LogException(exception);
			ReturnToMainMenu();
		}
	}

	private async Task _LoadMap(SceneLoadSetup sceneLoadSetup, GameSetup? gameSetup, INetworkSetup networkSetup)
	{
		_loadedMapSceneDescriptors = sceneLoadSetup.Descriptors;
		Messenger.Default.Send(default(MapWillLoadEvent));
		ProgressHelper progress = new ProgressHelper(_persistentLoader);
		Multiplayer.PrepareHostIfNeeded(networkSetup);
		try
		{
			progress.ShowProgressText(0f, "Two cars to a couple...");
			progress.SetBounds(0f, 0.1f);
			await UnloadSceneAsyncAsync(SceneDescriptor.MainMenu, progress.ShowProgress);
			progress.SetBounds(0.1f, 0.1f);
			await LoadSceneAsyncAsync(SceneDescriptor.GameUI, LoadSceneMode.Additive, progress.ShowProgress);
			progress.SetBounds(0.2f, 0.8f);
			foreach (SceneDescriptor descriptor in sceneLoadSetup.Descriptors)
			{
				await LoadSceneAsyncAsync(descriptor, LoadSceneMode.Additive, progress.ShowProgress);
			}
			SceneManager.SetActiveScene(sceneLoadSetup.Active.Scene);
		}
		catch (Exception innerException)
		{
			Debug.LogException(new Exception("Error loading map", innerException));
			throw;
		}
		try
		{
			progress.ShowProgressText(0.95f, "Half a car...");
			StateManager.Shared.ApplyGameSetup(gameSetup);
			await Multiplayer.ConnectClient(networkSetup);
		}
		catch (Exception innerException2)
		{
			Debug.LogException(new Exception("Error handling post-map load network setup", innerException2));
			throw;
		}
	}

	private static async Task LoadSceneAsyncAsync(SceneDescriptor sceneDescriptor, LoadSceneMode loadSceneMode, Action<float> progressFunc)
	{
		try
		{
			if (sceneDescriptor.IsLoaded)
			{
				Log.Debug("Scene {sceneDescriptor} will not be loaded; already loaded", sceneDescriptor);
				progressFunc(1f);
				return;
			}
			AsyncOperation op = sceneDescriptor.LoadAsync(loadSceneMode);
			while (!op.isDone)
			{
				progressFunc(op.progress);
				await Task.Yield();
			}
		}
		catch (Exception innerException)
		{
			throw new Exception($"Scene {sceneDescriptor} could not be unloaded", innerException);
		}
	}

	private static async Task UnloadSceneAsyncAsync(SceneDescriptor sceneDescriptor, Action<float> progressFunc)
	{
		try
		{
			if (!sceneDescriptor.IsLoaded)
			{
				Log.Debug("Scene {sceneDescriptor} will not be unloaded; not loaded", sceneDescriptor);
				progressFunc(1f);
				return;
			}
			AsyncOperation op = sceneDescriptor.UnloadAsync();
			while (op != null && !op.isDone)
			{
				progressFunc(op.progress);
				await Task.Delay(100);
			}
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Error unloading scene {sceneDescriptor}", sceneDescriptor);
		}
	}

	public async void ReturnToMainMenu()
	{
		try
		{
			await _ReturnToMainMenu();
		}
		catch (Exception exception)
		{
			Debug.LogError("Error returning to main menu:");
			Debug.LogException(exception);
		}
	}

	private async Task _ReturnToMainMenu()
	{
		if (_state != State.Game)
		{
			Log.Debug("MainMenu already loaded.");
			return;
		}
		_state = State.ReturningToMainMenu;
		try
		{
			Messenger.Default.Send(default(MapWillUnloadEvent));
		}
		catch
		{
		}
		try
		{
			if (Multiplayer.IsClientActive && Multiplayer.Client != null)
			{
				Multiplayer.Client.Disconnect();
			}
			Multiplayer.StopServer();
			_persistentLoader.ShowLoadingScreen(show: true);
			ProgressHelper progress = new ProgressHelper(_persistentLoader);
			Debug.Log("Unload map");
			progress.ShowProgressText(0f, "Tyin' down...");
			progress.SetBounds(0f, 0.3f);
			foreach (SceneDescriptor item in Enumerable.Reverse(_loadedMapSceneDescriptors))
			{
				await UnloadSceneAsyncAsync(item, progress.ShowProgress);
			}
			_loadedMapSceneDescriptors.Clear();
			Debug.Log("Unload UI");
			progress.SetBounds(0.3f, 0.3f);
			await UnloadSceneAsyncAsync(SceneDescriptor.GameUI, progress.ShowProgress);
			Debug.Log("Load main menu");
			progress.SetBounds(0.6f, 0.4f);
			await LoadSceneAsyncAsync(SceneDescriptor.MainMenu, LoadSceneMode.Additive, progress.ShowProgress);
			Debug.Log("Done");
			SceneManager.SetActiveScene(SceneDescriptor.MainMenu.Scene);
			_persistentLoader.ShowLoadingScreen(show: false);
		}
		finally
		{
			_state = State.Menu;
			Messenger.Default.Send(default(MapDidUnloadEvent));
		}
	}
}
