using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Core.Diagnostics;
using Helpers;
using Model.Ops.Timetable;
using Serilog;
using TMPro;
using UI.Builder;
using UI.Common;
using UnityEngine;

namespace UI.Timetable;

public class VisualTimetableEditor : BaseTimetableEditor
{
	private string _selectedTrainName;

	private IReadOnlyList<string> _stationsEastToWest;

	private IReadOnlyList<string> _stationsWestToEast;

	private UIPanelBuilder? _timetableContentBuilder;

	private UIPanelBuilder? _timetableAuxBuilder;

	private UIPanelBuilder? _editorDetailBuilder;

	private Model.Ops.Timetable.Timetable _timetable;

	private readonly TimetableLoadSaveHelper _timetableLoadSaveHelper = new TimetableLoadSaveHelper();

	private bool _pendingCloseRequest;

	private Model.Ops.Timetable.Timetable.Train SelectedTrain
	{
		get
		{
			if (!string.IsNullOrEmpty(_selectedTrainName))
			{
				return _timetable.Trains.GetValueOrDefault(_selectedTrainName);
			}
			return null;
		}
	}

	public bool HasUnsavedChanges { get; private set; }

	public VisualTimetableEditor(TimetableController timetableController, TimetableEditorWindow timetableEditorWindow)
		: base(timetableController, timetableEditorWindow)
	{
	}

	private void DidChangeTimetable()
	{
		UpdateHasUnsavedChanges();
		_timetableContentBuilder?.Rebuild();
		_timetableAuxBuilder?.Rebuild();
		_editorDetailBuilder?.Rebuild();
	}

	private void UpdateHasUnsavedChanges()
	{
		HasUnsavedChanges = !_timetable.Equals(TimetableController.CurrentRaw);
	}

	private void HandleApplyChanges()
	{
		string text = TimetableWriter.Write(_timetable);
		Log.Information("Timetable:\n" + text);
		HasUnsavedChanges = false;
		TimetableController.SetCurrent(text);
		UpdateHasUnsavedChanges();
		_timetableContentBuilder?.Rebuild();
		_timetableAuxBuilder?.Rebuild();
	}

	public void Build(UIPanelBuilder builder)
	{
		Model.Ops.Timetable.Timetable timetable = TimetableController.CurrentRaw ?? new Model.Ops.Timetable.Timetable();
		timetable = (_timetable = timetable.Clone());
		if (_selectedTrainName == null || !timetable.Trains.ContainsKey(_selectedTrainName))
		{
			_selectedTrainName = timetable.Trains.Values.FirstOrDefault()?.Name;
		}
		UpdateHasUnsavedChanges();
		if (_stationsEastToWest == null)
		{
			_stationsEastToWest = (from ts in TimetableController.GetAllStations(null, includeDisabled: true, includeDuplicates: false)
				select ts.code).ToList();
		}
		if (_stationsWestToEast == null)
		{
			_stationsWestToEast = _stationsEastToWest.Reverse().ToList();
		}
		builder.HStack(delegate(UIPanelBuilder uIPanelBuilder)
		{
			uIPanelBuilder.VStack(delegate(UIPanelBuilder uIPanelBuilder2)
			{
				uIPanelBuilder2.HVScrollView(delegate(UIPanelBuilder uIPanelBuilder3)
				{
					_timetableContentBuilder = uIPanelBuilder3;
					TimetableWindow.BuildTimetableContent(uIPanelBuilder3, TimetableEditorWindow.Shared.DefaultSize.x - 300, TimetableController, _timetable, SelectedTrain?.Name, delegate(string selectName)
					{
						_selectedTrainName = selectName;
						DidChangeTimetable();
					});
				});
				uIPanelBuilder2.HStack(delegate(UIPanelBuilder uIPanelBuilder3)
				{
					_timetableAuxBuilder = uIPanelBuilder3;
					uIPanelBuilder3.AddButton("Apply Timetable", HandleApplyChanges).Tooltip("Apply Timetable", "Apply editor changes to the current game timetable.").Disable(!HasUnsavedChanges);
					AddLoadSaveDropdown(uIPanelBuilder3);
					uIPanelBuilder3.AddButton("?", delegate
					{
						LinkDispatcher.Open(EntityType.Help, "timetables");
					});
					uIPanelBuilder3.Spacer();
					uIPanelBuilder3.AddButton("Add Train", delegate
					{
						ShowAddTrain();
					});
				}).Height(30f);
			});
			uIPanelBuilder.AddVRule();
			uIPanelBuilder.VScrollView(delegate(UIPanelBuilder uIPanelBuilder2)
			{
				_editorDetailBuilder = uIPanelBuilder2;
				BuildTrainEditorContent(uIPanelBuilder2, _timetable);
			}, new RectOffset(0, 8, 0, 0)).Width(300f);
		});
	}

	private void BuildTrainEditorContent(UIPanelBuilder builder, Model.Ops.Timetable.Timetable timetable)
	{
		Model.Ops.Timetable.Timetable.Train selectedTrain = SelectedTrain;
		if (selectedTrain == null)
		{
			builder.AddExpandingVerticalSpacer();
			builder.AddLabelEmptyState("Select a train");
			builder.AddExpandingVerticalSpacer();
			return;
		}
		builder.FieldLabelWidth = 90f;
		builder.AddField("Symbol", builder.AddInputField(selectedTrain.Name, HandleRenameSelectedTrain, "Train Name"));
		builder.AddField("Direction", builder.AddDropdown(new List<string> { "Westbound", "Eastbound" }, (int)selectedTrain.Direction, delegate(int index)
		{
			SetDirection((Model.Ops.Timetable.Timetable.Direction)index);
		}));
		builder.AddField("Class", builder.AddDropdown(new List<string> { "First", "Second", "Third" }, (int)selectedTrain.TrainClass, delegate(int index)
		{
			SetTrainClass((Model.Ops.Timetable.Timetable.TrainClass)index);
		}));
		builder.AddField("Type", builder.AddDropdown(new List<string> { "Freight", "Passenger" }, (int)selectedTrain.TrainType, delegate(int index)
		{
			SetTrainType((Model.Ops.Timetable.Timetable.TrainType)index);
		}));
		builder.Spacer(8f);
		builder.ButtonStrip(delegate(UIPanelBuilder uIPanelBuilder)
		{
			uIPanelBuilder.Spacer();
			uIPanelBuilder.AddButtonCompact("Remove", HandleRemoveSelectedTrain);
		});
		builder.Spacer(16f);
		HashSet<string> illogicalStations = selectedTrain.GetIllogicalStations(TimetableController.branches);
		IReadOnlyList<string> readOnlyList = ((selectedTrain.Direction == Model.Ops.Timetable.Timetable.Direction.West) ? _stationsEastToWest : _stationsWestToEast);
		int priorTime = 0;
		for (int num = 0; num < readOnlyList.Count; num++)
		{
			string text = readOnlyList[num];
			if (TryGetInterStationText(selectedTrain, readOnlyList, num, priorTime, out var priorStationText))
			{
				builder.Spacer(8f);
				builder.AddLabel("<align=right><size=80%>" + priorStationText).Tooltip("Estimated Timing", "Estimated time in minutes to travel between stations based on track speed. Time in parenthesis is the difference between the fastest estimate and the timetable time.");
				builder.Spacer(8f);
			}
			bool isIllogical = illogicalStations.Contains(text);
			priorTime = AddCellForStation(builder, timetable, text, priorTime, isIllogical);
		}
		builder.AddExpandingVerticalSpacer();
		builder.FieldLabelWidth = null;
	}

	private bool TryGetInterStationText(Model.Ops.Timetable.Timetable.Train train, IReadOnlyList<string> stations, int queryStationIndex, int priorTime, out string priorStationText)
	{
		priorStationText = null;
		string text = null;
		if (!train.TryGetTimetableEntry(stations[queryStationIndex], out var stationEntry, out var index))
		{
			return false;
		}
		if (queryStationIndex <= 0)
		{
			return false;
		}
		for (int num = queryStationIndex - 1; num >= 0; num--)
		{
			string text2 = stations[num];
			foreach (Model.Ops.Timetable.Timetable.Entry entry in train.Entries)
			{
				if (entry.Station == text2)
				{
					text = text2;
					break;
				}
			}
			if (text != null)
			{
				break;
			}
		}
		if (text == null)
		{
			return false;
		}
		string station = stationEntry.Station;
		if (!TimetableController.TryGetTimingForStations(station, text, out var minutesBetweenFast, out var minutesBetweenSlow))
		{
			return false;
		}
		int num2 = 0;
		if (train.TryGetAbsoluteTimeForEntry(index, TimetableTimeType.Arrival, out var minutes))
		{
			if (minutes < priorTime)
			{
				minutes += 1440;
			}
			num2 = minutes - priorTime - minutesBetweenFast;
		}
		string text3 = ((num2 >= 0) ? $"+{num2}" : string.Format("{0} {1}", num2, "<sprite name=Warning>"));
		string text4 = ((minutesBetweenFast == minutesBetweenSlow) ? $"{minutesBetweenFast}" : $"{minutesBetweenFast}-{minutesBetweenSlow}");
		priorStationText = text + " to " + station + " est. " + text4 + " min (" + text3 + ")";
		return true;
	}

	private int AddCellForStation(UIPanelBuilder builder, Model.Ops.Timetable.Timetable timetable, string stationCode, int priorTime, bool isIllogical)
	{
		if (!TimetableController.TryGetStation(stationCode, out var station))
		{
			return priorTime;
		}
		string stationName = station.DisplayName;
		Model.Ops.Timetable.Timetable.Train selectedTrain = SelectedTrain;
		Model.Ops.Timetable.Timetable.Entry entry;
		int entryIndex;
		bool hasEntry = selectedTrain.TryGetTimetableEntry(stationCode, out entry, out entryIndex);
		if (!hasEntry && !station.IsEnabled)
		{
			return priorTime;
		}
		builder.HStack(delegate(UIPanelBuilder uIPanelBuilder)
		{
			bool interactable = !isIllogical || hasEntry;
			uIPanelBuilder.AddToggle(() => hasEntry, delegate(bool toggleChecked)
			{
				CheckStation(stationCode, toggleChecked);
				uIPanelBuilder.Rebuild();
			}, interactable).Width(20f);
			byte b = byte.MaxValue;
			if (!hasEntry)
			{
				b = 102;
			}
			string text = "";
			if (station.passengerStop != null)
			{
				text += " <size=60%>S</size>";
			}
			if (!station.IsEnabled)
			{
				b = 51;
				text += " (Unavailable)";
			}
			if (isIllogical)
			{
				b = 51;
			}
			string text2 = $"<alpha=#{b:X2}>{stationName}{text}";
			uIPanelBuilder.AddLabel(text2).FlexibleWidth();
		});
		if (!hasEntry)
		{
			return priorTime;
		}
		int minutes;
		bool num = selectedTrain.TryGetAbsoluteTimeForEntry(entryIndex, TimetableTimeType.Arrival, out minutes);
		int minutes2;
		bool flag = selectedTrain.TryGetAbsoluteTimeForEntry(entryIndex, TimetableTimeType.Departure, out minutes2);
		if (!num)
		{
			if (flag)
			{
				minutes = minutes2;
			}
			else
			{
				Model.Ops.Timetable.Timetable.Entry entry2 = selectedTrain.Entries[entryIndex];
				minutes = priorTime + (entry2.ArrivalTime?.Minutes ?? 0);
				minutes2 = minutes + entry2.DepartureTime.Minutes;
				if (!entry2.ArrivalTime.HasValue)
				{
					minutes = minutes2;
				}
			}
		}
		AddTimeField(builder, "Arrival", entry, minutes, priorTime, delegate(TimetableTime? timetableTime)
		{
			SetArrivalTime(stationCode, timetableTime);
		}, isArrivalTime: true);
		AddTimeField(builder, "Departure", entry, minutes2, minutes, delegate(TimetableTime? timetableTime)
		{
			SetDepartureTime(stationCode, timetableTime.Value);
		}, isArrivalTime: false);
		builder.HStack(delegate(UIPanelBuilder builder2)
		{
			AddFieldLabel(builder2, "Meets");
			string value = string.Join(", ", entry.Meets);
			builder2.AddInputField(value, delegate(string newValue)
			{
				HandleSetMeets(entryIndex, newValue);
			}, "None").Tooltip("Meets", "Comma-separated train symbols that this train meets at this station before departing.").FlexibleWidth();
		}, 8f);
		return minutes2;
	}

	private void AddTimeField(UIPanelBuilder builder, string labelText, Model.Ops.Timetable.Timetable.Entry entry, int thisAbsoluteTime, int priorAbsoluteTime, Action<TimetableTime?> setTime, bool isArrivalTime)
	{
		List<string> absoluteRelative = new List<string> { "@", "+" };
		if (isArrivalTime)
		{
			absoluteRelative.Add("Not Specified");
		}
		TimetableTime time = ((!isArrivalTime) ? entry.DepartureTime : (entry.ArrivalTime ?? TimetableTime.Relative(0)));
		bool existingTimeIsNull = isArrivalTime && !entry.ArrivalTime.HasValue;
		builder.HStack(delegate(UIPanelBuilder builder2)
		{
			AddFieldLabel(builder2, labelText);
			int currentSelectedIndex = ((!time.IsAbsolute) ? 1 : 0);
			if (isArrivalTime && !entry.ArrivalTime.HasValue)
			{
				currentSelectedIndex = 2;
			}
			RectTransform rectTransform = builder2.AddDropdown(absoluteRelative, currentSelectedIndex, delegate(int selectedMode)
			{
				if (selectedMode == 2)
				{
					setTime(null);
				}
				else
				{
					bool flag = selectedMode == 0;
					if (time.IsAbsolute != flag || existingTimeIsNull)
					{
						TimetableTime value2 = ((!flag) ? TimetableTime.Relative(thisAbsoluteTime - priorAbsoluteTime) : TimetableTime.Absolute(thisAbsoluteTime));
						setTime(value2);
					}
				}
			});
			if (isArrivalTime && !entry.ArrivalTime.HasValue)
			{
				rectTransform.FlexibleWidth();
			}
			else
			{
				rectTransform.Width(50f);
				string value = (time.IsAbsolute ? time.TimeString() : time.Minutes.ToString());
				builder2.AddInputField(value, delegate(string newText)
				{
					if (TryParseTime(newText, time, out var newTime))
					{
						setTime(newTime);
					}
					else
					{
						Log.Error("Error parsing time: \"{text}\"", newText);
						Toast.Present(time.IsAbsolute ? "Time must be in HH:MM 24-hour format." : "Relative time must be a number of minutes.");
					}
				}, time.IsAbsolute ? "HH:MM" : "Minutes", 5).FlexibleWidth();
				if (!time.IsAbsolute)
				{
					builder2.AddLabel(TimetableTime.Absolute(thisAbsoluteTime).TimeString()).HorizontalTextAlignment(HorizontalAlignmentOptions.Right).VerticalTextAlignment(VerticalAlignmentOptions.Middle)
						.Width(60f);
				}
			}
		}, 8f);
	}

	private static void AddFieldLabel(UIPanelBuilder builder, string label)
	{
		builder.AddLabel("<style=LeadingLabel>" + label).Width(90f);
	}

	private static bool TryParseTime(string inputText, TimetableTime oldTime, out TimetableTime newTime)
	{
		if (oldTime.IsAbsolute)
		{
			return TimetableReader.TryParseTime(inputText, out newTime);
		}
		int result2;
		bool result = int.TryParse(inputText, out result2);
		newTime = TimetableTime.Relative(result2);
		return result;
	}

	private void SetArrivalTime(string stationCode, TimetableTime? arrivalTime)
	{
		Debug.Log($"SetArrivalTime: {stationCode} {arrivalTime}");
		if (!SelectedTrain.TryGetTimetableEntry(stationCode, out var stationEntry, out var index))
		{
			throw new Exception("No entry for station code");
		}
		stationEntry.ArrivalTime = arrivalTime;
		SelectedTrain.Entries[index] = stationEntry;
		DidChangeTimetable();
	}

	private void SetDepartureTime(string stationCode, TimetableTime departureTime)
	{
		Debug.Log($"SetDepartureTime: {stationCode} {departureTime}");
		if (!SelectedTrain.TryGetTimetableEntry(stationCode, out var stationEntry, out var index))
		{
			throw new Exception("No entry for station code");
		}
		stationEntry.DepartureTime = departureTime;
		SelectedTrain.Entries[index] = stationEntry;
		DidChangeTimetable();
	}

	private void CheckStation(string stationCode, bool toggleChecked)
	{
		Debug.Log($"CheckStation: {stationCode} {toggleChecked}");
		Model.Ops.Timetable.Timetable.Entry stationEntry;
		if (toggleChecked)
		{
			if (SelectedTrain.TryGetTimetableEntry(stationCode, out stationEntry, out var _))
			{
				return;
			}
			SelectedTrain.AddEntry(new Model.Ops.Timetable.Timetable.Entry(stationCode, null, TimetableTime.Relative(0), Array.Empty<string>()), _stationsEastToWest);
		}
		else
		{
			if (!SelectedTrain.TryGetTimetableEntry(stationCode, out stationEntry, out var index2))
			{
				return;
			}
			SelectedTrain.Entries.RemoveAt(index2);
		}
		DidChangeTimetable();
	}

	private void SetTrainClass(Model.Ops.Timetable.Timetable.TrainClass trainClass)
	{
		SelectedTrain.TrainClass = trainClass;
		DidChangeTimetable();
	}

	private void SetTrainType(Model.Ops.Timetable.Timetable.TrainType trainType)
	{
		SelectedTrain.TrainType = trainType;
		DidChangeTimetable();
	}

	private void SetDirection(Model.Ops.Timetable.Timetable.Direction direction)
	{
		SelectedTrain.Direction = direction;
		SelectedTrain.SortEntries(_stationsEastToWest);
		DidChangeTimetable();
	}

	private void HandleSetMeets(int entryIndex, string newMeets)
	{
		newMeets = newMeets.Trim();
		if (!TimetableReader.IsValidMeetString(newMeets))
		{
			Toast.Present("Meets must be comma separated train symbols.");
			return;
		}
		Model.Ops.Timetable.Timetable.Entry value = SelectedTrain.Entries[entryIndex];
		value.Meets = (string.IsNullOrEmpty(newMeets) ? Array.Empty<string>() : Regex.Split(newMeets, "\\s*,\\s*"));
		SelectedTrain.Entries[entryIndex] = value;
		DidChangeTimetable();
	}

	private void ShowAddTrain(string symbol = "")
	{
		ModalAlertController.Present("Add Train", "Timetable train symbol must contain only letters and numbers.", symbol, new List<(bool, string)>
		{
			(false, "Cancel"),
			(true, "Add")
		}, delegate((bool, string) tuple)
		{
			if (tuple.Item1)
			{
				string item = tuple.Item2;
				HandleAddTrain(item);
			}
		});
	}

	private bool TryValidateTrainSymbol(ref string symbol, Action<string> onValidateFailure)
	{
		symbol = NormalizeTrainSymbol(symbol);
		string sym = symbol;
		if (!TimetableReader.IsValidTrainSymbol(symbol))
		{
			ModalAlertController.PresentOkay("Invalid train symbol", "Train symbols must contain only letters and numbers.", delegate
			{
				onValidateFailure(sym);
			});
			return false;
		}
		foreach (KeyValuePair<string, Model.Ops.Timetable.Timetable.Train> train in _timetable.Trains)
		{
			train.Deconstruct(out var _, out var value);
			if (value.Name.Equals(symbol, StringComparison.OrdinalIgnoreCase))
			{
				ModalAlertController.PresentOkay("Train already exists", "Train symbol \"" + symbol + "\" is already in use in this timetable.", delegate
				{
					onValidateFailure(sym);
				});
				return false;
			}
		}
		return true;
	}

	private static string NormalizeTrainSymbol(string symbol)
	{
		return symbol.ToUpper().Trim();
	}

	private void HandleAddTrain(string symbol)
	{
		symbol = NormalizeTrainSymbol(symbol);
		if (_timetable.Trains.ContainsKey(symbol))
		{
			Toast.Present("Train " + symbol + " already exists.");
			_selectedTrainName = symbol;
			DidChangeTimetable();
		}
		else if (TryValidateTrainSymbol(ref symbol, ShowAddTrain))
		{
			int result;
			Model.Ops.Timetable.Timetable.Train value = new Model.Ops.Timetable.Timetable.Train(direction: (int.TryParse(symbol, out result) && result % 2 == 0) ? Model.Ops.Timetable.Timetable.Direction.East : Model.Ops.Timetable.Timetable.Direction.West, name: symbol, trainClass: Model.Ops.Timetable.Timetable.TrainClass.First, trainType: Model.Ops.Timetable.Timetable.TrainType.Passenger, entries: new List<Model.Ops.Timetable.Timetable.Entry>());
			_timetable.Trains[symbol] = value;
			_selectedTrainName = symbol;
			DidChangeTimetable();
		}
	}

	private void HandleRemoveSelectedTrain()
	{
		if (SelectedTrain == null)
		{
			return;
		}
		Model.Ops.Timetable.Timetable.Train train = SelectedTrain;
		ModalAlertController.Present("Remove Train?", "Train " + train.Name + " will be removed from the timetable. This can not be undone.", new List<(bool, string)>
		{
			(false, "Cancel"),
			(true, "Remove")
		}, delegate(bool remove)
		{
			if (remove)
			{
				_timetable.Trains.Remove(train.Name);
				_selectedTrainName = null;
				DidChangeTimetable();
			}
		});
	}

	private void HandleRenameSelectedTrain(string newName)
	{
		newName = NormalizeTrainSymbol(newName);
		Model.Ops.Timetable.Timetable.Train selectedTrain = SelectedTrain;
		if (selectedTrain != null && !(selectedTrain.Name == newName))
		{
			if (!TryValidateTrainSymbol(ref newName, delegate
			{
			}))
			{
				DidChangeTimetable();
				return;
			}
			_timetable.Trains.Remove(selectedTrain.Name);
			selectedTrain.Name = newName;
			_timetable.Trains[selectedTrain.Name] = selectedTrain;
			DidChangeTimetable();
		}
	}

	private void AddLoadSaveDropdown(UIPanelBuilder builder)
	{
		List<DropdownMenu.RowData> list = new List<DropdownMenu.RowData>
		{
			new DropdownMenu.RowData("Clear Timetable", "Remove all trains from this timetable."),
			new DropdownMenu.RowData("Save to Disk", "Save this timetable to a file on disk."),
			new DropdownMenu.RowData("Load from Disk", "Load a timetable from a file on disk.")
		};
		IList<string> predefinedTimetablePaths = PredefinedTimetableStore.AvailableTimetables();
		foreach (string item in predefinedTimetablePaths)
		{
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(item);
			list.Add(new DropdownMenu.RowData("Load " + fileNameWithoutExtension, "Load a predefined timetable."));
		}
		builder.AddOptionsDropdown(list, delegate(int option)
		{
			switch (option)
			{
			case 0:
				_timetable.Trains.Clear();
				_selectedTrainName = null;
				DidChangeTimetable();
				break;
			case 1:
				HandleSaveToDisk();
				break;
			case 2:
				HandleLoadFromDisk();
				break;
			default:
			{
				string path = predefinedTimetablePaths[option - 3];
				HandleLoadStarter(path);
				break;
			}
			}
		});
	}

	private void HandleSaveToDisk()
	{
		_timetableLoadSaveHelper.PromptToSaveTimetable(_timetable);
	}

	private void HandleLoadFromDisk()
	{
		_timetableLoadSaveHelper.PromptToLoadTimetable(delegate(Model.Ops.Timetable.Timetable timetable)
		{
			_timetable = timetable;
			DidChangeTimetable();
		});
	}

	private void HandleLoadStarter(string path)
	{
		if (_timetable != null && _timetable.Trains.Count > 0)
		{
			ModalAlertController.Present("Replace timetable?", $"This will replace {_timetable.Trains.Count} scheduled trains with the starter set.", new List<(bool, string)>
			{
				(true, "Replace"),
				(false, "Cancel")
			}, delegate(bool replace)
			{
				if (replace)
				{
					LoadPredefined(path);
				}
			});
		}
		else
		{
			LoadPredefined(path);
		}
	}

	private void LoadPredefined(string path)
	{
		string document = File.ReadAllText(path);
		StringDiagnosticCollector stringDiagnosticCollector = new StringDiagnosticCollector();
		if (TimetableController.Shared.TryRead(document, out var output, stringDiagnosticCollector))
		{
			_timetable = output;
		}
		else
		{
			ModalAlertController.PresentOkay("Error loading timetable", stringDiagnosticCollector.ToString());
		}
		DidChangeTimetable();
	}

	public void HandleWindowRequestClose(Action closeAction)
	{
		if (!HasUnsavedChanges)
		{
			closeAction();
		}
		else
		{
			if (_pendingCloseRequest)
			{
				return;
			}
			_pendingCloseRequest = true;
			ModalAlertController.Present("Close without applying changes?", "Changes to this timetable have not been applied and will be lost.", new List<(int, string)>
			{
				(0, "Cancel"),
				(1, "Discard"),
				(2, "Apply")
			}, delegate(int choice)
			{
				_pendingCloseRequest = false;
				switch (choice)
				{
				case 1:
					closeAction();
					break;
				case 2:
					HandleApplyChanges();
					closeAction();
					break;
				case 0:
					break;
				}
			});
		}
	}
}
