using System;
using System.Collections.Generic;
using Model.Ops.Definition;
using UnityEngine;

namespace Model.Ops;

[CreateAssetMenu(fileName = "Team Track Profile", menuName = "Railroader/Team Track Profile", order = 0)]
public class TeamTrackProfile : ScriptableObject
{
	[Serializable]
	public struct Entry
	{
		[Tooltip("Identifier for this load. Must be unique.")]
		public string tag;

		[Tooltip("True if the team track exports this load (orders empties to be loaded).")]
		public bool export;

		public Load load;

		[Tooltip("Time in days that this entry takes to load or unload.")]
		[Range(0.5f, 2f)]
		public float loadingTime;

		public CarTypeFilter carTypeFilter;
	}

	public List<Entry> entries;
}
