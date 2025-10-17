using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Core.Diagnostics;
using Model.Ops.Timetable;
using UI.Builder;
using UI.Common;
using UnityEngine;

namespace UI.Timetable;

public class TimetableLoadSaveHelper
{
	private enum Mode
	{
		Load,
		Save
	}

	private readonly string _path;

	private string _filename;

	private string FilePath
	{
		get
		{
			if (_filename != null)
			{
				return Path.Combine(_path, _filename);
			}
			return _path;
		}
	}

	public TimetableLoadSaveHelper()
	{
		_path = Path.Combine(Application.persistentDataPath, "Timetables");
	}

	private void LoadSave(Mode mode, Model.Ops.Timetable.Timetable timetable, Action<Model.Ops.Timetable.Timetable> loadCallback)
	{
		if (mode == Mode.Load)
		{
			_filename = null;
		}
		ModalAlertController.Present(delegate(UIPanelBuilder builder, Action dismissAction)
		{
			UIPanelBuilder alertBuilder = builder;
			builder.Spacing = 16f;
			string text;
			string text2;
			switch (mode)
			{
			case Mode.Load:
				text = "Load from Disk";
				text2 = "Select a file to load a timetable from.";
				break;
			case Mode.Save:
				text = "Save to Disk";
				text2 = "Select a file to overwrite or enter a new filename.";
				break;
			default:
				throw new ArgumentOutOfRangeException("mode", mode, null);
			}
			builder.AddLabel("<b>" + text + "</b>\n" + text2);
			string text3 = NormalizePath(_path);
			builder.AddLabel("<size=12><#8D8A81>Timetable Directory: " + text3);
			builder.VScrollView(delegate(UIPanelBuilder uIPanelBuilder)
			{
				foreach (string filename in Filenames())
				{
					uIPanelBuilder.HStack(delegate(UIPanelBuilder uIPanelBuilder2)
					{
						uIPanelBuilder2.AddLabel(filename).FlexibleWidth();
						uIPanelBuilder2.AddButtonCompact("Select", delegate
						{
							_filename = filename;
							alertBuilder.Rebuild();
						}).Width(80f);
					});
				}
			}).Height(250f);
			builder.AddField("Filename", mode switch
			{
				Mode.Save => builder.AddInputField(_filename, delegate(string filename)
				{
					_filename = filename;
				}, "timetable.txt"), 
				Mode.Load => builder.AddLabel(string.IsNullOrEmpty(_filename) ? "<i>Select a file</i>" : _filename), 
				_ => throw new ArgumentOutOfRangeException("mode", mode, null), 
			});
			builder.AlertButtons(delegate(UIPanelBuilder uIPanelBuilder)
			{
				switch (mode)
				{
				case Mode.Load:
					uIPanelBuilder.AddButton("Load", delegate
					{
						Load(loadCallback);
						dismissAction();
					}).Disable(!File.Exists(FilePath));
					break;
				case Mode.Save:
					uIPanelBuilder.AddButton("Save", delegate
					{
						Save(timetable);
						dismissAction();
					});
					break;
				default:
					throw new ArgumentOutOfRangeException("mode", mode, null);
				}
				uIPanelBuilder.AddButton("Cancel", dismissAction);
			});
		});
	}

	private static string NormalizePath(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return path;
		}
		path = path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
		return path;
	}

	public void PromptToSaveTimetable(Model.Ops.Timetable.Timetable timetable)
	{
		LoadSave(Mode.Save, timetable, null);
	}

	public void PromptToLoadTimetable(Action<Model.Ops.Timetable.Timetable> callback)
	{
		LoadSave(Mode.Load, null, callback);
	}

	private IEnumerable<string> Filenames()
	{
		if (!Directory.Exists(_path))
		{
			return Array.Empty<string>();
		}
		return Directory.GetFiles(_path, "*", SearchOption.TopDirectoryOnly).Select(Path.GetFileName);
	}

	private void Save(Model.Ops.Timetable.Timetable timetable)
	{
		try
		{
			if (!Directory.Exists(_path))
			{
				Directory.CreateDirectory(_path);
			}
			string contents = TimetableWriter.Write(timetable);
			File.WriteAllText(FilePath, contents);
			Toast.Present("Saved.");
		}
		catch (Exception ex)
		{
			ModalAlertController.PresentOkay("Error saving " + _filename, ex.Message);
		}
	}

	private void Load(Action<Model.Ops.Timetable.Timetable> loadCallback)
	{
		try
		{
			string path = Path.Combine(_path, _filename);
			if (!File.Exists(path))
			{
				Toast.Present("File not found.");
				return;
			}
			string document = File.ReadAllText(path);
			StringDiagnosticCollector stringDiagnosticCollector = new StringDiagnosticCollector();
			if (TimetableController.Shared.TryRead(document, out var output, stringDiagnosticCollector))
			{
				loadCallback(output);
			}
			else
			{
				ModalAlertController.PresentOkay("Error loading timetable", stringDiagnosticCollector.ToString());
			}
		}
		catch (Exception ex)
		{
			Toast.Present("Error loading: " + ex.Message);
		}
	}
}
