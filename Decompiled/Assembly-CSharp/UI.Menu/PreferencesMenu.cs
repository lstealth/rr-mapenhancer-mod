using System.Collections.Generic;
using Serilog;
using UI.Builder;
using UI.Common;
using UI.PreferencesWindow;
using UnityEngine;

namespace UI.Menu;

public class PreferencesMenu : BuilderMenuBase
{
	protected override void BuildPanelContent(UIPanelBuilder builder)
	{
		PreferencesBuilder.Build(builder);
		builder.Spacer(16f);
		builder.HStack(delegate(UIPanelBuilder uIPanelBuilder)
		{
			uIPanelBuilder.AddButton("Back", delegate
			{
				this.NavigationController().Pop();
			});
			uIPanelBuilder.Spacer().FlexibleWidth(1f);
			uIPanelBuilder.AddButton("Reset Preferences", ResetPreferences);
		});
	}

	private void ResetPreferences()
	{
		ModalAlertController.Present("Reset Preferences?", "Are you sure you want to reset all preferences, including video & audio settings and custom key bindings? This cannot be undone. Save games will not be affected.", new List<(bool, string)>
		{
			(false, "Cancel"),
			(true, "Reset")
		}, delegate(bool delete)
		{
			if (delete)
			{
				Log.Information("Deleting all preferences.");
				PlayerPrefs.DeleteAll();
				ModalAlertController.PresentOkay("Preferences Reset", "Preferences have been reset. Railroader will now quit. Restart the game to apply the changes.", Application.Quit);
			}
		});
	}
}
