using System;
using Character;
using Model.Ops.Definition;
using Track;
using UnityEngine;

namespace Game.Progression;

public class SetupDescriptor : MonoBehaviour
{
	[Serializable]
	public class CarPlacement
	{
		public string[] carIdentifier;

		public TrackMarker marker;

		public bool wreck;

		[Range(0f, 1f)]
		public float oiled = 1f;

		[Tooltip("Percent of load to be added to these cars. For engines and tenders, sets fuel/water level. No effect on other equipment if load is null.")]
		[Range(0f, 1f)]
		public float loadPercent;

		[Tooltip("If non-null and loadPercent > 0, will be added to the car(s) in this cut.")]
		public Load load;
	}

	public string identifier;

	public int initialMoney;

	public SpawnPoint spawnPoint;

	public CarPlacement[] placements;

	public bool showTutorial;
}
