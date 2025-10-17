using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Game.Events;
using Game.State;
using Model.Ops.Timetable;
using TMPro;
using UI.Builder;
using UI.Common;
using UnityEngine;

namespace UI.Timetable;

public class TimetableWindow : MonoBehaviour, IProgrammaticWindow, IBuilderWindow
{
	private Window _window;

	private UIPanel _panel;

	private TimetableController _timetableController;

	private string[] _allStations;

	private bool _firstBuild = true;

	private const float ColWidthStation = 130f;

	private const int ColWidthTrain = 70;

	private static readonly Model.Ops.Timetable.Timetable.TrainClass[] ClassesWest = new Model.Ops.Timetable.Timetable.TrainClass[3]
	{
		Model.Ops.Timetable.Timetable.TrainClass.Third,
		Model.Ops.Timetable.Timetable.TrainClass.Second,
		Model.Ops.Timetable.Timetable.TrainClass.First
	};

	private static readonly Model.Ops.Timetable.Timetable.TrainClass[] ClassesEast = new Model.Ops.Timetable.Timetable.TrainClass[3]
	{
		Model.Ops.Timetable.Timetable.TrainClass.First,
		Model.Ops.Timetable.Timetable.TrainClass.Second,
		Model.Ops.Timetable.Timetable.TrainClass.Third
	};

	public string WindowIdentifier => "Timetable";

	public Vector2Int DefaultSize => new Vector2Int(280, 550);

	public Window.Position DefaultPosition => Window.Position.Center;

	public Window.Sizing Sizing => Window.Sizing.Resizable(new Vector2Int(280, 280));

	public UIBuilderAssets BuilderAssets { get; set; }

	public static TimetableWindow Shared => WindowManager.Shared.GetWindow<TimetableWindow>();

	public void Show()
	{
		Populate();
		_window.ShowWindow();
	}

	public static void Toggle()
	{
		if (Shared._window.IsShown)
		{
			Shared._window.CloseWindow();
		}
		else if (!StateManager.Shared.Storage.TimetableFeature)
		{
			Toast.Present("Timetable feature is not enabled in Company Settings.");
		}
		else
		{
			Shared.Show();
		}
	}

	private void Awake()
	{
		_window = GetComponent<Window>();
	}

	private void OnDisable()
	{
		_panel?.Dispose();
		_panel = null;
	}

	private void Populate()
	{
		if ((object)_timetableController == null)
		{
			_timetableController = TimetableController.Shared;
		}
		_window.Title = "Timetable";
		_panel?.Dispose();
		_panel = UIPanel.Create(_window.contentRectTransform, BuilderAssets, Build);
	}

	private void Build(UIPanelBuilder builder)
	{
		if (_firstBuild)
		{
			_firstBuild = false;
			int num = 0;
			foreach (TimetableBranch branch in _timetableController.branches)
			{
				GetTimetableStationsAndTrains(_timetableController, _timetableController.Current, branch, out var _, out var trainsWest, out var trainsEast);
				num = Mathf.Max(num, trainsEast.Count + trainsWest.Count);
			}
			if (num > 0)
			{
				int contentWidth = Mathf.RoundToInt(130f + (float)(num * 70) + 40f);
				_window.SetContentWidth(contentWidth);
			}
		}
		BuildTimetablePanel(builder, DefaultSize.x);
	}

	private static void BuildTimetablePanel(UIPanelBuilder builder, float fullWidth)
	{
		TimetableController timetableController = TimetableController.Shared;
		builder.VStack(delegate(UIPanelBuilder uIPanelBuilder)
		{
			uIPanelBuilder.HVScrollView(delegate(UIPanelBuilder builder2)
			{
				builder2.RebuildOnEvent<TimetableDidChange>();
				Model.Ops.Timetable.Timetable current = timetableController.Current;
				BuildTimetableContent(builder2, fullWidth, timetableController, current);
			});
			if (TimetableController.CanEdit)
			{
				uIPanelBuilder.HStack(delegate(UIPanelBuilder uIPanelBuilder2)
				{
					uIPanelBuilder2.AddButton("Edit Timetable", delegate
					{
						TimetableEditorWindow.Shared.Show();
					});
				}).Height(30f);
			}
		});
	}

	internal static void BuildTimetableContent(UIPanelBuilder builder, float fullWidth, TimetableController timetableController, Model.Ops.Timetable.Timetable timetable, string highlightedTrainName = null, Action<string> onClickTrain = null)
	{
		if (timetable == null || timetable.Trains == null || timetable.Trains.Count == 0)
		{
			builder.AddExpandingVerticalSpacer();
			string text = (timetableController.HasError ? "Timetable Has Errors" : "Empty Timetable");
			builder.AddLabelEmptyState(text).Width(fullWidth);
			builder.AddExpandingVerticalSpacer();
			return;
		}
		timetable = timetable.ToAbsolute();
		builder.Spacing = 8f;
		foreach (TimetableBranch branch in timetableController.branches)
		{
			bool showHeader = branch != timetableController.branches[0];
			BuildTimetableContent(builder, fullWidth, timetableController, timetable, branch, showHeader, highlightedTrainName, onClickTrain);
		}
		builder.AddExpandingVerticalSpacer();
	}

	private static void BuildTimetableContent(UIPanelBuilder builder, float fullWidth, TimetableController timetableController, Model.Ops.Timetable.Timetable timetable, TimetableBranch branch, bool showHeader, string highlightedTrainName = null, Action<string> onClickTrain = null)
	{
		GetTimetableStationsAndTrains(timetableController, timetable, branch, out var allStations, out var trainsWest, out var trainsEast);
		int count = trainsEast.Count;
		int count2 = trainsWest.Count;
		if (count == 0 && count2 == 0)
		{
			return;
		}
		if (showHeader)
		{
			builder.AddLabel("<style=h3>" + branch.name + "</style>");
		}
		List<TableRow> list = new List<TableRow>();
		List<float> list2 = new List<float>();
		for (int i = 0; i < count2; i++)
		{
			list2.Add(70f);
		}
		list2.Add(130f);
		for (int j = 0; j < count; j++)
		{
			list2.Add(70f);
		}
		List<TableCellEntry> list3 = new List<TableCellEntry>();
		if (count2 > 0)
		{
			list3.Add(new TableCellEntry((count2 == 1) ? DirectionString("West", "Read Down") : DirectionString("Westbound", "Read Down"), count2, 15));
		}
		list3.Add(new TableCellEntry(null, 1, 11));
		if (count > 0)
		{
			list3.Add(new TableCellEntry((count == 1) ? DirectionString("East", "Read Up") : DirectionString("Eastbound", "Read Up"), count, 15));
		}
		list.Add(new TableRow(list3, 40f));
		list3 = null;
		List<TableCellEntry> list4 = new List<TableCellEntry>();
		Model.Ops.Timetable.Timetable.TrainClass[] classesWest = ClassesWest;
		foreach (Model.Ops.Timetable.Timetable.TrainClass trainClass in classesWest)
		{
			int num = trainsWest.Count((Model.Ops.Timetable.Timetable.Train t) => t.TrainClass == trainClass);
			if (num != 0)
			{
				list4.Add(new TableCellEntry("<style=tt-header><size=90%>" + LabelTextForClass(trainClass, num) + "</style>", num, 14));
			}
		}
		list4.Add(new TableCellEntry(null, 1, 10));
		classesWest = ClassesEast;
		foreach (Model.Ops.Timetable.Timetable.TrainClass trainClass2 in classesWest)
		{
			int num2 = trainsEast.Count((Model.Ops.Timetable.Timetable.Train t) => t.TrainClass == trainClass2);
			if (num2 != 0)
			{
				list4.Add(new TableCellEntry("<style=tt-header><size=90%>" + LabelTextForClass(trainClass2, num2) + "</style>", num2, 14));
			}
		}
		list.Add(new TableRow(list4, 30f));
		list4 = null;
		List<TableCellEntry> trainHeaders = new List<TableCellEntry>();
		foreach (Model.Ops.Timetable.Timetable.Train item2 in trainsWest)
		{
			AddTrainCell(builder, item2);
		}
		trainHeaders.Add(new TableCellEntry("<style=tt-header>Stations</style>", 1, 14));
		foreach (Model.Ops.Timetable.Timetable.Train item3 in trainsEast)
		{
			AddTrainCell(builder, item3);
		}
		list.Add(new TableRow(trainHeaders, 30f));
		trainHeaders = null;
		StringBuilder sb = new StringBuilder();
		TimetableStation station;
		List<TableCellEntry> trainRow;
		foreach (TimetableStation item4 in allStations)
		{
			station = item4;
			bool flag = false;
			foreach (Model.Ops.Timetable.Timetable.Train value in timetable.Trains.Values)
			{
				foreach (Model.Ops.Timetable.Timetable.Entry entry in value.Entries)
				{
					if (entry.Station == station.code)
					{
						flag = true;
						break;
					}
				}
				if (flag)
				{
					break;
				}
			}
			if (flag)
			{
				trainRow = new List<TableCellEntry>();
				AddTrainCells(trainsWest);
				string displayName = station.DisplayName;
				trainRow.Add(new TableCellEntry(displayName, 1, 10));
				AddTrainCells(trainsEast);
				list.Add(new TableRow(trainRow, 30f));
			}
		}
		list.Add(new TableRow(new List<TableCellEntry>
		{
			new TableCellEntry(null, list2.Count, 1)
		}, 4f));
		float num3 = (fullWidth - list2.Sum()) / 2f;
		TableBuilderConfig config = new TableBuilderConfig
		{
			TextOverflowMode = TextOverflowModes.Overflow,
			TextWrappingMode = TextWrappingModes.NoWrap,
			TextMargin = new Vector4(8f, 0f, 12f, 0f),
			BorderOpacity = 0.5f,
			LeadingInset = ((num3 < 0f) ? 0f : num3)
		};
		builder.AddTable(list, list2, config);
		void AddCell(Model.Ops.Timetable.Timetable.Entry entry, Model.Ops.Timetable.Timetable.Direction direction)
		{
			sb.Clear();
			if (entry.HasSingleArrivalAndDeparture)
			{
				AppendTime(entry.DepartureTime.TimeString());
			}
			else
			{
				string text = "<size=60%>A</size>" + entry.ArrivalTime.Value.TimeString();
				string text2 = "<size=60%>D</size>" + entry.DepartureTime.TimeString();
				string timeString;
				string timeString2;
				if (direction != Model.Ops.Timetable.Timetable.Direction.West)
				{
					string text3 = text;
					timeString = text2;
					timeString2 = text3;
				}
				else
				{
					string text3 = text2;
					timeString = text;
					timeString2 = text3;
				}
				sb.Append("<size=90%>");
				AppendTime(timeString);
				sb.Append("<line-height=80%>\n</line-height>");
				AppendTime(timeString2);
				sb.Append("</size>");
			}
			TableCellEntry item = new TableCellEntry($"<align=right>{sb}</align>", 1, 10);
			if (entry.Meets.Count > 0)
			{
				string text4 = string.Join(", ", entry.Meets);
				item = item.WithTrailingText("<size=60%>" + text4);
			}
			trainRow.Add(item);
		}
		void AddTrainCell(UIPanelBuilder uIPanelBuilder, Model.Ops.Timetable.Timetable.Train train)
		{
			Action onClick = ((onClickTrain != null) ? ((Action)delegate
			{
				onClickTrain(train.Name);
			}) : null);
			trainHeaders.Add(new TableCellEntry("<align=right><style=tt-train>" + train.Name + "</style></align>", 1, 14, train.Name == highlightedTrainName, onClick).WithAutoSize(12, 22));
		}
		void AddTrainCells(List<Model.Ops.Timetable.Timetable.Train> trains)
		{
			foreach (Model.Ops.Timetable.Timetable.Train train in trains)
			{
				bool flag2 = false;
				foreach (Model.Ops.Timetable.Timetable.Entry entry2 in train.Entries)
				{
					if (entry2.Station == station.code)
					{
						AddCell(entry2, train.Direction);
						flag2 = true;
						break;
					}
				}
				if (!flag2)
				{
					trainRow.Add(new TableCellEntry(null, 1, 10));
				}
			}
		}
		void AppendTime(string timeString)
		{
			sb.Append("<mspace=0.6em>" + timeString + "</mspace>");
		}
		static string DirectionString(string direction, string read)
		{
			return "<style=tt-header>" + direction + "</style><line-height=0>\n<voffset=-13><size=70%><alpha=#60>" + read + "</line-height>";
		}
		static string LabelTextForClass(Model.Ops.Timetable.Timetable.TrainClass trainClass3, int num4)
		{
			string text = trainClass3 switch
			{
				Model.Ops.Timetable.Timetable.TrainClass.First => "First", 
				Model.Ops.Timetable.Timetable.TrainClass.Second => "Second", 
				Model.Ops.Timetable.Timetable.TrainClass.Third => "Third", 
				_ => throw new ArgumentOutOfRangeException("trainClass", trainClass3, null), 
			};
			if (num4 != 1)
			{
				return text + " Class";
			}
			return text;
		}
	}

	private static void GetTimetableStationsAndTrains(TimetableController timetableController, Model.Ops.Timetable.Timetable timetable, TimetableBranch branch, out IReadOnlyList<TimetableStation> allStations, out List<Model.Ops.Timetable.Timetable.Train> trainsWest, out List<Model.Ops.Timetable.Timetable.Train> trainsEast)
	{
		allStations = timetableController.GetAllStations(branch);
		HashSet<string> branchStationCodes = (from ts in allStations
			where ts.junctionType == TimetableStation.JunctionType.None
			select ts.code).ToHashSet();
		trainsWest = (from t in timetable.Trains.Values
			where t.Direction == Model.Ops.Timetable.Timetable.Direction.West
			where t.StationsIntersectWithStationCodes(branchStationCodes)
			orderby t.TrainClass descending, t.SortOrderWithinClass descending
			select t).ToList();
		trainsEast = (from t in timetable.Trains.Values
			where t.Direction == Model.Ops.Timetable.Timetable.Direction.East
			where t.StationsIntersectWithStationCodes(branchStationCodes)
			orderby t.TrainClass, t.SortOrderWithinClass
			select t).ToList();
	}
}
