using System;
using System.Collections.Generic;
using System.Linq;
using Game.Persistence;
using Game.State;
using Helpers;
using UI.Builder;
using UI.Common;
using UnityEngine;

namespace UI.Menu;

[RequireComponent(typeof(RectTransform))]
public class NewGameMenu : BuilderMenuBase
{
	public Action<string, NewGameSetup> OnContinue;

	private GrammarRailroadNameGenerator _generator;

	private string _railroadName;

	private string _reportingMark;

	private string _saveName;

	private string _progressionId;

	private string _setupId;

	private GameMode _gameMode = GameMode.Company;

	public const string ProgressionIdEWH = "ewh";

	private string GameModeDescription => _gameMode switch
	{
		GameMode.Company => "<style=p>Bring your railroad back to life in the wake of a flood. Manage your trains, money, and customers.</style>", 
		GameMode.Sandbox => "<style=p>Play in a semi-structured full map. Create and delete engines and cars freely while also serving customers.</style>", 
		_ => throw new ArgumentOutOfRangeException(), 
	};

	private void Awake()
	{
		if (_generator == null)
		{
			_generator = new GrammarRailroadNameGenerator();
			GenerateRandomName();
		}
		_saveName = WorldStore.NewGameName();
	}

	protected override void BuildPanelContent(UIPanelBuilder builder)
	{
		UIPanelBuilder panelBuilder = builder;
		builder.HStack(delegate(UIPanelBuilder uIPanelBuilder)
		{
			uIPanelBuilder.AddLabel("What's the name of your railroad?").FlexibleWidth(1f);
			uIPanelBuilder.AddButtonCompact("Random", delegate
			{
				GenerateRandomName();
				panelBuilder.Rebuild();
			}).Width(110f);
		});
		builder.AddField("Railroad Name", builder.AddInputField(_railroadName, delegate(string str)
		{
			_railroadName = str.StripHtml();
		}, null, 50));
		builder.AddField("Reporting Mark", builder.AddInputFieldReportingMark(_reportingMark, delegate(string str)
		{
			_reportingMark = str;
		}));
		builder.Spacer(16f);
		builder.AddLabel("Game Options");
		builder.Spacer(8f);
		builder.AddField("Mode", builder.ButtonStrip(delegate(UIPanelBuilder uIPanelBuilder)
		{
			uIPanelBuilder.AddButtonSelectable("Company", _gameMode == GameMode.Company, delegate
			{
				_gameMode = GameMode.Company;
				panelBuilder.Rebuild();
			});
			uIPanelBuilder.AddButtonSelectable("Sandbox", _gameMode == GameMode.Sandbox, delegate
			{
				_gameMode = GameMode.Sandbox;
				_progressionId = null;
				panelBuilder.Rebuild();
			});
		}));
		if (_gameMode == GameMode.Company)
		{
			builder.Spacer(8f);
			builder.AddField("Map", BuildMapSelect(builder));
		}
		builder.AddField("", GameModeDescription);
		builder.AddExpandingVerticalSpacer();
		builder.AddField("Name Your Save", builder.AddInputField(_saveName, delegate(string str)
		{
			_saveName = str;
		}));
		builder.Spacer(16f);
		builder.HStack(delegate(UIPanelBuilder uIPanelBuilder)
		{
			uIPanelBuilder.AddButton("Back", delegate
			{
				this.NavigationController().Pop();
			});
			uIPanelBuilder.Spacer().FlexibleWidth(1f);
			uIPanelBuilder.AddButton("Start", StartButtonClicked);
		});
	}

	private RectTransform BuildMapSelect(UIPanelBuilder builder)
	{
		List<string> values = new List<string> { "East Whittier Start" };
		List<string> progressionIds = new List<string> { "ewh" };
		int num = progressionIds.IndexOf(_progressionId);
		if (num < 0 && _gameMode == GameMode.Company)
		{
			num = 0;
			SelectProgressionId(progressionIds[num]);
		}
		return builder.AddDropdown(values, num, delegate(int i)
		{
			SelectProgressionId(progressionIds[i]);
		});
	}

	private void SelectProgressionId(string progressionId)
	{
		_progressionId = progressionId;
		string setupId = ((!(_progressionId == "ewh")) ? null : "ewh-steam");
		_setupId = setupId;
	}

	private RectTransform BuildEquipmentSelect(UIPanelBuilder builder)
	{
		List<string> values = new List<string> { "Steam", "Diesel" };
		return builder.AddDropdown(values, 0, delegate
		{
		});
	}

	private void GenerateRandomName()
	{
		_railroadName = _generator.Generate();
		_reportingMark = ReportingMarkFromName();
	}

	private string ReportingMarkFromName()
	{
		return new string(_railroadName.Where(char.IsUpper).Take(4).ToArray());
	}

	private void StartButtonClicked()
	{
		bool flag = _gameMode == GameMode.Company;
		NewGameSetup arg = new NewGameSetup(_railroadName, _reportingMark, _gameMode, flag ? _progressionId : null, flag ? _setupId : null);
		string saveName = _saveName;
		if (string.IsNullOrWhiteSpace(arg.RailroadName) || string.IsNullOrWhiteSpace(arg.ReportingMark) || string.IsNullOrWhiteSpace(saveName))
		{
			Toast.Present("Please complete all fields.");
		}
		else if (WorldStore.Exists(saveName))
		{
			Toast.Present("A save by that name already exists.");
		}
		else
		{
			OnContinue?.Invoke(saveName, arg);
		}
	}
}
