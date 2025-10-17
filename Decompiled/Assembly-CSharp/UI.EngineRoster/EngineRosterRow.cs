using System.Collections.Generic;
using Game.State;
using Model;
using Model.AI;
using Model.Definition;
using Model.Ops;
using TMPro;
using Track;
using UI.CarInspector;
using UI.LazyScrollList;
using UI.Map;
using UI.Tooltips;
using UnityEngine;
using UnityEngine.UI;

namespace UI.EngineRoster;

public class EngineRosterRow : MonoBehaviour, ILazyScrollListCell
{
	[SerializeField]
	private RectTransform rectTransform;

	[SerializeField]
	private TMP_Text nameLabel;

	[SerializeField]
	private TMP_Text infoLabel;

	[SerializeField]
	private TMP_Text crewLabel;

	[SerializeField]
	private Toggle favoriteToggle;

	[SerializeField]
	private Toggle selectedToggle;

	[SerializeField]
	private UITooltipProvider nameTooltip;

	[SerializeField]
	private UITooltipProvider crewTooltip;

	[SerializeField]
	private UITooltipProvider infoTooltip;

	private BaseLocomotive _engine;

	private EngineRosterPanel _parent;

	private AutoEngineerPersistence _persistence;

	private readonly List<string> _crewComponents = new List<string>(3);

	public int ListIndex { get; private set; }

	public RectTransform RectTransform => rectTransform;

	public void Configure(BaseLocomotive engine, bool isFavorite, bool isSelected, EngineRosterPanel parent)
	{
		_parent = parent;
		_engine = engine;
		_persistence = new AutoEngineerPersistence(_engine.KeyValueObject);
		favoriteToggle.SetIsOnWithoutNotify(isFavorite);
		selectedToggle.SetIsOnWithoutNotify(isSelected);
		Refresh();
	}

	public void Refresh()
	{
		nameLabel.text = _engine.DisplayName;
		_crewComponents.Clear();
		bool flag = _persistence.Orders.Enabled;
		if (flag)
		{
			_crewComponents.Add("AE");
		}
		if (_engine.IsMuEnabled)
		{
			_crewComponents.Add("MU");
		}
		if (_engine.TryGetTrainName(out var trainName))
		{
			_crewComponents.Add(trainName);
		}
		string text = string.Join(" | ", _crewComponents);
		crewLabel.text = text;
		int num = Mathf.RoundToInt(_engine.VelocityMphAbs);
		string text2 = (_engine.IsDerailed ? "Derailed" : ((num == 0 && flag) ? _persistence.PlannerStatus : ((num != 0) ? $"{num} mph" : "Stopped")));
		infoLabel.text = text2;
		ObjectMetadata metadata = _engine.DefinitionInfo.Metadata;
		nameTooltip.TooltipInfo = new TooltipInfo(metadata.Name, metadata.Description);
		crewTooltip.TooltipInfo = new TooltipInfo(text, null);
		infoTooltip.TooltipInfo = new TooltipInfo(text2, null);
	}

	private string GetTrainCrewName()
	{
		PlayersManager playersManager = StateManager.Shared.PlayersManager;
		string result = null;
		if (playersManager.TrainCrewForId(_engine.trainCrewId, out var trainCrew))
		{
			result = trainCrew.Name;
		}
		return result;
	}

	public void ActionToggleFavorite()
	{
		_parent.ToggleFavorite(_engine);
	}

	public void ActionJumpTo()
	{
		CameraSelector.shared.FollowCar(_engine);
	}

	public void ActionSelect(bool select)
	{
		_parent.SelectEngine(_engine, selectedToggle.isOn);
	}

	public void ActionInspect()
	{
		UI.CarInspector.CarInspector.Show(_engine);
	}

	public void ActionMap()
	{
		MapWindow.Show(_engine.GetCenterPosition(Graph.Shared));
	}

	public void Configure(int listIndex, object data)
	{
		ListIndex = listIndex;
		RosterRowData rosterRowData = (RosterRowData)data;
		Configure(rosterRowData.Engine, rosterRowData.IsFavorite, rosterRowData.IsSelected, rosterRowData.Parent);
	}
}
