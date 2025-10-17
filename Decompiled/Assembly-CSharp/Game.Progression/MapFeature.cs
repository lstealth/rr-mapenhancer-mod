using Model.Ops;
using UnityEngine;

namespace Game.Progression;

public class MapFeature : MonoBehaviour
{
	[Tooltip("Unique identifier for this feature.")]
	public string identifier;

	[Header("Display Information")]
	public string displayName;

	public string description;

	public bool defaultEnableInSandbox;

	[Header("Requirements to Unlock")]
	[Tooltip("features which must be unlocked prior to this one.")]
	public MapFeature[] prerequisites;

	[Header("Enables upon unlocking")]
	[Tooltip("Track groups to enable (create/show) upon unlock.")]
	public string[] trackGroupsEnableOnUnlock;

	[Tooltip("Track groups to make available for use upon unlock.")]
	public string[] trackGroupsAvailableOnUnlock;

	[Header("Game Object Unlocks")]
	[Tooltip("Game objects to enable (create/show) when unlocked. These should be exclusive to this feature.")]
	public GameObject[] gameObjectsEnableOnUnlock;

	[Header("Area Industry Unlocks")]
	public Area[] areasEnableOnUnlock;

	[Tooltip("Specific industries that should not be unlocked which are inside areasEnableOnUnlock.")]
	public Industry[] unlockExcludeIndustries;

	[Tooltip("Specific industries that should be unlocked which are outside areasEnableOnUnlock.")]
	public Industry[] unlockIncludeIndustries;

	public IndustryComponent[] unlockIncludeIndustryComponents;

	public string DisplayName
	{
		get
		{
			if (!string.IsNullOrEmpty(displayName))
			{
				return displayName;
			}
			return base.name;
		}
	}

	public bool Unlocked { get; set; }

	public override string ToString()
	{
		return identifier + ": " + DisplayName;
	}
}
