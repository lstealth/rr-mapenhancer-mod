using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace UI.Timetable;

public class PredefinedTimetableStore
{
	private static IList<string> _textFiles;

	public static IList<string> AvailableTimetables()
	{
		if (_textFiles == null)
		{
			_textFiles = FindTimetableFiles();
		}
		return _textFiles;
	}

	private static IList<string> FindTimetableFiles()
	{
		string path = Path.Combine(Application.streamingAssetsPath, "Timetables");
		new List<string>();
		if (Directory.Exists(path))
		{
			return Directory.GetFiles(path, "*.txt").ToList();
		}
		return Array.Empty<string>();
	}
}
