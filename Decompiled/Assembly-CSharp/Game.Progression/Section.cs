using System;
using Model.Ops;
using Model.Ops.Definition;
using Serilog;
using UnityEngine;

namespace Game.Progression;

public class Section : MonoBehaviour
{
	[Serializable]
	public class DeliveryPhase
	{
		[Tooltip("Cost to order cars in this phase. Paid by player prior to cars being ordered.")]
		public int cost;

		public Delivery[] deliveries;

		[Tooltip("Industry component which is enabled when this phase of this section is pending.")]
		public ProgressionIndustryComponent industryComponent;
	}

	[Serializable]
	public class Delivery
	{
		public enum Direction
		{
			LoadToIndustry,
			LoadFromIndustry
		}

		public CarTypeFilter carTypeFilter;

		public int count;

		public Load load;

		public Direction direction;
	}

	[Tooltip("Unique identifier for this section.")]
	public string identifier;

	[Header("Display Information")]
	public string displayName;

	[TextArea]
	public string description;

	[Header("Requirements to Unlock")]
	[Tooltip("Sections which must be unlocked prior to this one becoming available.")]
	public Section[] prerequisiteSections;

	[Tooltip("Deliveries that must be completed in order to unlock this section.")]
	public DeliveryPhase[] deliveryPhases;

	public MapFeature[] enableFeaturesOnUnlock;

	public MapFeature[] enableFeaturesOnAvailable;

	public MapFeature[] disableFeaturesOnUnlock;

	public bool Unlocked { get; set; }

	public bool Available { get; set; }

	public int PaidCount { get; set; }

	public int FulfilledCount { get; set; }

	public int PhaseCount => deliveryPhases.Length;

	public InterchangeTransfer[] InterchangeTransfers { get; private set; }

	private void Awake()
	{
		InterchangeTransfers = GetComponentsInChildren<InterchangeTransfer>();
	}

	public void ApplyCompleted()
	{
		try
		{
			InterchangeTransfer[] interchangeTransfers = InterchangeTransfers;
			for (int i = 0; i < interchangeTransfers.Length; i++)
			{
				interchangeTransfers[i].Apply();
			}
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Exception applying completed section {id}", identifier);
		}
	}
}
