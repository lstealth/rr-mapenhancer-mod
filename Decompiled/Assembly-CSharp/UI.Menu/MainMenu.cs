using System;
using System.Linq;
using Analytics;
using UnityEngine;

namespace UI.Menu;

public class MainMenu : SoftMenu
{
	public enum MainMenuAction
	{
		Singleplayer,
		Multiplayer,
		Settings,
		Editor,
		Tests,
		Help,
		Credits
	}

	public Action<MainMenuAction> OnMainMenuAction;

	protected override void Awake()
	{
		base.Awake();
		Configure(null, wantsBackButton: false);
		AddButton("Singleplayer", delegate
		{
			OnMainMenuAction?.Invoke(MainMenuAction.Singleplayer);
		});
		AddButton("Multiplayer", delegate
		{
			OnMainMenuAction?.Invoke(MainMenuAction.Multiplayer);
		});
		AddButton("Preferences", delegate
		{
			OnMainMenuAction?.Invoke(MainMenuAction.Settings);
		});
		AddButton("Help", delegate
		{
			OnMainMenuAction?.Invoke(MainMenuAction.Help);
		});
		if (ShouldShowEditor())
		{
			AddButton("Editor", delegate
			{
				OnMainMenuAction?.Invoke(MainMenuAction.Editor);
			});
		}
		AddButton("Credits", delegate
		{
			OnMainMenuAction?.Invoke(MainMenuAction.Credits);
		});
		AddButton("Quit", delegate
		{
			Application.Quit();
		});
	}

	private bool ShouldShowEditor()
	{
		return Environment.GetCommandLineArgs().Contains("/editor");
	}

	public void EarlyAccessClicked()
	{
		EarlyAccessSplash earlyAccessSplash = UnityEngine.Object.FindObjectOfType<EarlyAccessSplash>();
		if (!(earlyAccessSplash == null))
		{
			earlyAccessSplash.Show();
		}
	}
}
