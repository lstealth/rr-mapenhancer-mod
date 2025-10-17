using System;
using System.Collections.Generic;
using UnityEngine;

namespace Model.Ops.Timetable;

[Serializable]
public class TimetableBranch
{
	public string name;

	[Tooltip("Stations on the timetable, east to west.")]
	public List<TimetableStation> stations;
}
