using System;
using Core;
using Game.State;
using Helpers;
using Network;
using Serilog;
using TMPro;
using UI;
using UI.CarEditor;
using UI.Common;
using UI.Menu;
using UI.PlayerList;
using UI.PreferencesWindow;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
	private enum QuitOption
	{
		Discard,
		Save,
		Cancel
	}

	private static bool _paused;

	[SerializeField]
	private GameObject canvas;

	[SerializeField]
	private Button saveButton;

	[SerializeField]
	private Button quitButton;

	[SerializeField]
	private PlayerListController playerListController;

	[SerializeField]
	private GlobalGameManager gameManager;

	private void Start()
	{
		UpdateForPaused();
	}

	private void OnEnable()
	{
		GameInput.RegisterEscapeHandler(GameInput.EscapeHandler.Pause, delegate
		{
			SetPaused(!_paused);
			return true;
		});
	}

	private void OnDisable()
	{
		GameInput.UnregisterEscapeHandler(GameInput.EscapeHandler.Pause);
	}

	private void SetPaused(bool paused)
	{
		if (paused != _paused)
		{
			_paused = paused;
			GameInput.shared.SetPaused(_paused);
			UpdateForPaused();
		}
	}

	private void UpdateForPaused()
	{
		canvas.SetActive(_paused);
		int num;
		int num2;
		if (_paused)
		{
			num = ((Multiplayer.Mode == ConnectionMode.Singleplayer) ? 1 : 0);
			if (num != 0)
			{
				num2 = 0;
				goto IL_0029;
			}
		}
		else
		{
			num = 0;
		}
		num2 = 1;
		goto IL_0029;
		IL_0029:
		Time.timeScale = num2;
		AudioListener.volume = ((num == 0) ? 1 : 0);
		saveButton.gameObject.SetActive(StateManager.IsHost);
		TMP_Text tMP_Text = quitButton.TMPText();
		tMP_Text.text = Multiplayer.Mode switch
		{
			ConnectionMode.MultiplayerServer => "Shutdown Server", 
			ConnectionMode.MultiplayerClient => "Disconnect", 
			_ => "Quit", 
		};
	}

	private static void _SaveGame()
	{
		StateManager.DebugAssertIsHost();
		SaveManager.Shared.Save(null);
		Console.Log("Game saved.");
	}

	public void ReturnToGame()
	{
		SetPaused(paused: false);
	}

	public void SaveGame()
	{
		_SaveGame();
		SetPaused(paused: false);
	}

	public void Preferences()
	{
		SetPaused(paused: false);
		PreferencesWindow.Show();
	}

	public void QuitGame()
	{
		if (StateManager.IsHost && !DefinitionEditorModeController.IsEditing && TryGetQuitPromptMessage(out var message))
		{
			ModalAlertController.Present("Do you want to save your game?", message, new(QuitOption, string)[3]
			{
				(QuitOption.Discard, "Discard"),
				(QuitOption.Save, "Save Game"),
				(QuitOption.Cancel, "Cancel")
			}, delegate(QuitOption b)
			{
				if (b != QuitOption.Cancel)
				{
					_Quit(b == QuitOption.Save);
				}
			});
		}
		else
		{
			_Quit(save: false);
		}
	}

	private static bool TryGetQuitPromptMessage(out string message)
	{
		try
		{
			SaveManager.Shared.GetLastSaveTimes(out var lastSaveTime, out var lastAutoSaveTime);
			if (!lastSaveTime.HasValue)
			{
				goto IL_0071;
			}
			DateTime valueOrDefault = lastSaveTime.GetValueOrDefault();
			if (!lastAutoSaveTime.HasValue)
			{
				goto IL_0071;
			}
			DateTime valueOrDefault2 = lastAutoSaveTime.GetValueOrDefault();
			if (valueOrDefault < valueOrDefault2)
			{
				message = "Autosaved " + AgoString(valueOrDefault2) + ".";
			}
			else
			{
				message = "Last saved " + AgoString(valueOrDefault) + ".";
			}
			goto end_IL_0000;
			IL_0071:
			if (lastSaveTime.HasValue)
			{
				DateTime valueOrDefault3 = lastSaveTime.GetValueOrDefault();
				DateTime dateTime = DateTime.Now - TimeSpan.FromSeconds(10.0);
				if (valueOrDefault3 > dateTime)
				{
					message = null;
					return false;
				}
				message = "Last saved " + AgoString(valueOrDefault3) + ".";
			}
			else if (lastAutoSaveTime.HasValue)
			{
				DateTime valueOrDefault4 = lastAutoSaveTime.GetValueOrDefault();
				message = "Autosaved " + AgoString(valueOrDefault4) + ".";
			}
			else
			{
				message = "This game has not been saved.";
			}
			end_IL_0000:;
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Exception while getting quit message");
			message = "Unsaved changed will be lost.";
		}
		return true;
	}

	private static string AgoString(DateTime t)
	{
		return (DateTime.Now - t).AgoString();
	}

	private void _Quit(bool save)
	{
		Log.Debug("QuitGame save = {save}", save);
		if (save)
		{
			_SaveGame();
		}
		SetPaused(paused: false);
		ReturnToMainMenu();
	}

	public void ReturnToMainMenu()
	{
		gameManager.ReturnToMainMenu();
	}
}
