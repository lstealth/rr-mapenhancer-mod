using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Serilog;
using UnityEngine;

namespace Model.Ops;

public static class ForeignRoadsReader
{
	private const string Filename = "ReportingMarks.txt";

	private static string InternalPath => Path.Combine(Application.streamingAssetsPath, "ReportingMarks.txt");

	private static string ExternalPath => Path.Combine(Application.persistentDataPath, "ReportingMarks.txt");

	public static string[] ReadForeignRoads()
	{
		string[] array = ReadForeignRoads(ExternalPath);
		if (array.Length != 0)
		{
			Log.Information("Read {count} foreign roads externally.", array.Length);
			return array;
		}
		string[] array2 = ReadForeignRoads(InternalPath);
		if (array2.Length != 0)
		{
			Log.Information("Read {count} foreign roads internally.", array2.Length);
			return array2;
		}
		Log.Warning("Couldn't load foreign roads.");
		return new string[12]
		{
			"GCL", "RDG", "SOU", "N&W", "SAL", "L&N", "ACL", "PRR", "CRR", "NC&STL",
			"WNC", "NYC"
		};
	}

	private static string[] ReadForeignRoads(string path)
	{
		try
		{
			if (!File.Exists(path))
			{
				return Array.Empty<string>();
			}
			return (from line in File.ReadAllLines(path)
				select line.Trim().ToUpper() into line
				where !string.IsNullOrEmpty(line) && !line.StartsWith("#")
				select Regex.Replace(line, "[^A-Z&]", "")).ToArray();
		}
		catch (Exception)
		{
			Debug.LogError("Failed to load roads from " + path + ":");
			return Array.Empty<string>();
		}
	}
}
