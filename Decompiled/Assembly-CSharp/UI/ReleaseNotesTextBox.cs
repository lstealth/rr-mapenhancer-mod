using System;
using System.Collections.Generic;
using System.IO;
using Markroader;
using Serilog;
using TMPro;
using UnityEngine;

namespace UI;

public class ReleaseNotesTextBox : MonoBehaviour
{
	[SerializeField]
	private TMP_Text text;

	private void OnEnable()
	{
		PopulateReleaseNotes();
	}

	private void PopulateReleaseNotes()
	{
		string releaseNotesPath = GetReleaseNotesPath();
		string str;
		try
		{
			str = File.ReadAllText(releaseNotesPath);
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Error reading release notes from {path}", releaseNotesPath);
			str = "Unable to load release notes.";
		}
		PopulateReleaseNotes(str);
	}

	private void PopulateReleaseNotes(string str)
	{
		double realtimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
		List<Element> elements = Parser.Parse(str).Elements;
		double realtimeSinceStartupAsDouble2 = Time.realtimeSinceStartupAsDouble;
		Log.Debug("Tokenized in {dt}, {count} tokens", realtimeSinceStartupAsDouble2 - realtimeSinceStartupAsDouble, elements.Count);
		int num = 0;
		for (int i = 0; i < elements.Count; i++)
		{
			if (elements[i].Type == ElementType.H3)
			{
				num++;
				if (num == 16)
				{
					elements.RemoveRange(i, elements.Count - i);
					break;
				}
			}
		}
		str = TMPMarkupRenderer.Render(elements);
		text.text = str;
	}

	private string GetReleaseNotesPath()
	{
		return Path.Combine(Path.GetFullPath("."), "ReleaseNotes-Public.md");
	}
}
