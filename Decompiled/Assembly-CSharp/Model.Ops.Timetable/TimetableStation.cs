using System;
using Game.Progression;
using UnityEngine;

namespace Model.Ops.Timetable;

[Serializable]
public class TimetableStation
{
	public enum JunctionType
	{
		None,
		JunctionStation,
		JunctionDuplicate
	}

	[Tooltip("Two letter timetable code: 'BR'")]
	public string code;

	[Tooltip("Display name: 'Bryson'")]
	public string name;

	[Tooltip("Optional reference to a passenger stop, if any.")]
	public PassengerStop passengerStop;

	[Tooltip("Optional reference to a map feature, to indicate whether this station is available.")]
	public MapFeature mapFeature;

	[Tooltip("Seconds to traverse from this station to the next one. Last station value is ignored.")]
	public int traverseTimeToNext;

	public JunctionType junctionType;

	public bool IsBranchJunctionDuplicate => junctionType == JunctionType.JunctionDuplicate;

	public string DisplayName
	{
		get
		{
			if (!string.IsNullOrEmpty(name))
			{
				return name;
			}
			return passengerStop.TimetableName;
		}
	}

	public bool IsEnabled
	{
		get
		{
			if (mapFeature != null)
			{
				return mapFeature.Unlocked;
			}
			if (passengerStop != null)
			{
				return !passengerStop.ProgressionDisabled;
			}
			return true;
		}
	}
}
