using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Game.State;
using Model;
using Model.Database;
using Model.Definition;
using Model.Definition.Data;
using Track;
using UnityEngine;

namespace UI.Console.Commands;

[StructLayout(LayoutKind.Sequential, Size = 1)]
[ConsoleCommand("/pc", "Place consist")]
public struct PlaceConsistCommand : IConsoleCommand
{
	private static readonly Dictionary<char, string> CharToIdentifier = new Dictionary<char, string>
	{
		{ 'a', "ls-462-p18" },
		{ 'c', "ne-caboose01" },
		{ 'd', "gm-gondola02" },
		{ 'e', "fm-flatcar02" },
		{ 'f', "fm-flatcar01" },
		{ 'g', "gb-gondola01" },
		{ 'h', "hm-hopper01" },
		{ 'l', "pb-pullman-heavyweight-early" },
		{ 'm', "ls-460-t21" },
		{ 'n', "gcs-sc1-280-engine" },
		{ 'p', "ls-462-p43" },
		{ 't', "tm-tankcar01" },
		{ 'u', "tm-tankcar02" },
		{ 'x', "xm-boxcar01" },
		{ 'y', "xm-boxcar02" },
		{ 'z', "xm-boxcar03" }
	};

	public string Execute(string[] comps)
	{
		if (StateManager.Shared.GameMode == GameMode.Company)
		{
			return "Not available in this game mode.";
		}
		if (comps.Length < 2)
		{
			return "Usage: /pc " + string.Join("", CharToIdentifier.Keys);
		}
		List<CarDescriptor> descriptors = DescriptorsForString(comps[1]);
		if (comps.Length == 3)
		{
			string text = comps[2];
			TrainController shared = TrainController.Shared;
			TrackSegment segment = shared.graph.GetSegment(text);
			if (segment == null)
			{
				return "Segment not found: " + text;
			}
			float requiredLength = TrainController.ApproximateLength(descriptors);
			Location? location = shared.FindLocationForCut(new List<TrackSegment> { segment }, requiredLength);
			if (!location.HasValue)
			{
				return "Couldn't find room on track.";
			}
			shared.PlaceTrain(location.Value, descriptors);
		}
		else
		{
			Console.shared.Collapse();
			ConsistPlacer.Instance().Present(descriptors);
		}
		return null;
	}

	public static List<CarDescriptor> DescriptorsForString(string carChars)
	{
		List<CarDescriptor> list = new List<CarDescriptor>();
		IPrefabStore prefabStore = TrainController.Shared.PrefabStore;
		int num = 0;
		foreach (char c in carChars)
		{
			if (c >= '0' && c <= '9')
			{
				int num2 = c - 48;
				num *= 10;
				num += num2;
				continue;
			}
			if (CharToIdentifier.TryGetValue(c, out var value))
			{
				num = Mathf.Clamp(num, 1, 99);
				for (int j = 0; j < num; j++)
				{
					TypedContainerItem<CarDefinition> typedContainerItem = prefabStore.CarDefinitionInfoForIdentifier(value);
					list.Add(new CarDescriptor(typedContainerItem));
					if (typedContainerItem.Definition.TryGetTenderIdentifier(out var tenderIdentifier))
					{
						list.Add(new CarDescriptor(prefabStore.CarDefinitionInfoForIdentifier(tenderIdentifier)));
					}
				}
				num = 0;
				continue;
			}
			throw new Exception(string.Format("Invalid car: '{0}'. Valid options: {1}", c, string.Join("", CharToIdentifier.Keys)));
		}
		return list;
	}
}
