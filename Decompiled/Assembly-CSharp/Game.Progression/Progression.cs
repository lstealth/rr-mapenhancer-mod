using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Game.Reputation;
using Game.State;
using KeyValue.Runtime;
using Model.Ops;
using Network;
using Serilog;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Progression;

public class Progression : MonoBehaviour
{
	public string identifier;

	public MapFeatureManager mapFeatureManager;

	[FormerlySerializedAs("enableAtStart")]
	[SerializeField]
	private MapFeature[] enableFeaturesAtStart;

	private KeyValueObject _keyValueObject;

	private IDisposable _keyChangeObserver;

	private static Progression _instance;

	private const string KeyUnlocked = "unlocked";

	public Section[] Sections { get; private set; }

	public static Progression Shared => _instance;

	private IEnumerable<string> UnlockedSectionIds
	{
		get
		{
			return _keyValueObject["unlocked"].ArrayValue.Select((Value v) => v.StringValue);
		}
		set
		{
			_keyValueObject["unlocked"] = Value.Array(value.Select(Value.String).ToList());
		}
	}

	public void Configure(KeyValueObject keyValueObject)
	{
		_instance = this;
		Sections = GetComponentsInChildren<Section>();
		CheckSections();
		_keyValueObject = keyValueObject;
		_keyChangeObserver = _keyValueObject.ObserveKeyChanges(delegate
		{
			UpdateSectionStates();
		});
		if (StateManager.IsHost)
		{
			MapFeature[] array = enableFeaturesAtStart;
			foreach (MapFeature feature in array)
			{
				mapFeatureManager.SetFeatureEnabled(feature, unlocked: true);
			}
		}
		Messenger.Default.Register<GameModeDidChange>(this, delegate
		{
			UpdateSectionStates();
		});
		Messenger.Default.Register<ProgressionStateDidChange>(this, delegate
		{
			UpdateSectionStates();
		});
		UpdateSectionStates();
	}

	public void Unconfigure()
	{
		Messenger.Default.Unregister(this);
		_keyChangeObserver?.Dispose();
		_keyChangeObserver = null;
		_instance = null;
	}

	private void CheckSections()
	{
		List<ProgressionIndustryComponent> list = Sections.SelectMany((Section s) => (from p in s.deliveryPhases
			select p.industryComponent into ic
			where ic != null
			select ic).ToHashSet()).ToList();
		HashSet<ProgressionIndustryComponent> hashSet = list.ToHashSet();
		if (list.Count == hashSet.Count)
		{
			return;
		}
		foreach (ProgressionIndustryComponent item in hashSet)
		{
			list.Remove(item);
		}
		Log.Error("Progression {id} contains components multiple times: {comps}", identifier, list.Select((ProgressionIndustryComponent ic) => ic.Identifier));
	}

	private static string KeyPaid(string sectionId)
	{
		return "paid-" + sectionId;
	}

	private static string KeyFulfilled(string sectionId)
	{
		return "fulfilled-" + sectionId;
	}

	private int GetPaidDeliveriesForSectionId(string sectionId)
	{
		return _keyValueObject[KeyPaid(sectionId)].IntValue;
	}

	private void SetPaidDeliveriesForSectionId(string sectionId, int paidCount)
	{
		_keyValueObject[KeyPaid(sectionId)] = Value.Int(paidCount);
	}

	private int GetFulfilledDeliveriesForSectionId(string sectionId)
	{
		return _keyValueObject[KeyFulfilled(sectionId)].IntValue;
	}

	private void SetFulfilledDeliveriesForSectionId(string sectionId, int fulfilledCount)
	{
		_keyValueObject[KeyFulfilled(sectionId)] = Value.Int(fulfilledCount);
	}

	[ContextMenu("Debug: Advance")]
	public void Advance()
	{
		Section[] sections = Sections;
		foreach (Section section in sections)
		{
			if (!section.Unlocked && section.Available)
			{
				Advance(section);
				break;
			}
		}
	}

	public void Advance(Section section)
	{
		StateManager.AssertIsHost();
		if (!section.Available)
		{
			throw new Exception("Not available.");
		}
		int paidDeliveriesForSectionId = GetPaidDeliveriesForSectionId(section.identifier);
		if (paidDeliveriesForSectionId >= section.PhaseCount)
		{
			throw new Exception("Already paid");
		}
		SetPaidDeliveriesForSectionId(section.identifier, paidDeliveriesForSectionId + 1);
		PhaseCompleted(section, paidDeliveriesForSectionId);
	}

	public void Revert(Section section)
	{
		RevertHelper(section);
		UpdateSectionStates();
		StateManager.Shared.SendFireEvent(default(ProgressionStateDidChange));
	}

	private void RevertHelper(Section section)
	{
		Section[] sections = Sections;
		foreach (Section section2 in sections)
		{
			if (section2.prerequisiteSections.Contains(section) && section2.Unlocked)
			{
				Revert(section2);
			}
		}
		Log.Information("Revert: {section}", section.identifier);
		string text = section.identifier;
		SetPaidDeliveriesForSectionId(text, 0);
		SetFulfilledDeliveriesForSectionId(text, 0);
		UnlockedSectionIds = UnlockedSectionIds.Except(new string[1] { text }).ToHashSet();
	}

	private void PayToStartPhase(Section section, int phaseIndex, IPlayer sender)
	{
		StateManager.AssertIsHost();
		Log.Information("PayToStartPhase {section} {phaseIndex} {sender}", section.identifier, phaseIndex, sender);
		if (!section.Available)
		{
			throw new Exception("Section is not available");
		}
		if (section.PaidCount != phaseIndex)
		{
			throw new Exception("phaseIndex is not next in sequence");
		}
		Section.DeliveryPhase deliveryPhase = section.deliveryPhases[phaseIndex];
		int balance = StateManager.Shared.GetBalance();
		float discountPercent;
		int num = CostForPhase(deliveryPhase, out discountPercent);
		if (balance < num)
		{
			throw new DisplayableException("Insufficient balance");
		}
		SetPaidDeliveriesForSectionId(section.identifier, phaseIndex + 1);
		StateManager.Shared.ApplyToBalance(-num, Ledger.Category.Progression, null, section.displayName);
		if (deliveryPhase.deliveries.Length == 0)
		{
			Multiplayer.Broadcast((section.PhaseCount > 1) ? $"{sender.Name} has paid for phase {phaseIndex + 1} of {section.displayName}!" : (sender.Name + " has paid for " + section.displayName + "!"));
			PhaseCompleted(section, phaseIndex);
		}
		else
		{
			UpdateSectionStates();
			StateManager.Shared.SendFireEvent(default(ProgressionStateDidChange));
			Multiplayer.Broadcast($"{sender.Name} has ordered cars for phase {phaseIndex + 1} of {section.displayName}!");
		}
	}

	private Section SectionForId(string sectionId)
	{
		return Sections.FirstOrDefault((Section s) => s.identifier == sectionId);
	}

	private void UpdateSectionStates()
	{
		HashSet<Section> hashSet = UnlockedSectionIds.Select(SectionForId).ToHashSet();
		Section[] sections = Sections;
		foreach (Section section in sections)
		{
			bool unlocked = hashSet.Contains(section);
			section.Unlocked = unlocked;
		}
		sections = Sections;
		foreach (Section section2 in sections)
		{
			section2.Available = !section2.Unlocked && PrerequisitesMet(section2);
			section2.PaidCount = GetPaidDeliveriesForSectionId(section2.identifier);
			section2.FulfilledCount = GetFulfilledDeliveriesForSectionId(section2.identifier);
		}
		StringBuilder stringBuilder = new StringBuilder();
		sections = Sections;
		foreach (Section section3 in sections)
		{
			stringBuilder.Append(section3.name + ":" + (section3.Unlocked ? " Unlocked" : "") + (section3.Available ? " Available" : ""));
			if (section3.PaidCount > 0)
			{
				stringBuilder.Append($" paid={section3.PaidCount}");
			}
			if (section3.FulfilledCount > 0)
			{
				stringBuilder.Append($" fulfilled={section3.FulfilledCount}");
			}
			stringBuilder.AppendLine();
		}
		Debug.Log($"Progression States:\n{stringBuilder}");
		Dictionary<string, bool> dictionary = new Dictionary<string, bool>();
		sections = Sections;
		int j;
		foreach (Section section4 in sections)
		{
			MapFeature[] enableFeaturesOnAvailable = section4.enableFeaturesOnAvailable;
			foreach (MapFeature mapFeature in enableFeaturesOnAvailable)
			{
				dictionary[mapFeature.identifier] = section4.Available;
			}
			enableFeaturesOnAvailable = section4.enableFeaturesOnUnlock;
			foreach (MapFeature mapFeature2 in enableFeaturesOnAvailable)
			{
				dictionary[mapFeature2.identifier] = section4.Unlocked;
			}
			enableFeaturesOnAvailable = section4.disableFeaturesOnUnlock;
			for (j = 0; j < enableFeaturesOnAvailable.Length; j++)
			{
				MapFeature mapFeature3 = enableFeaturesOnAvailable[j];
				dictionary[mapFeature3.identifier] = !section4.Unlocked;
			}
		}
		mapFeatureManager.SetFeatureEnables(dictionary);
		bool flag = false;
		sections = Sections;
		foreach (Section section5 in sections)
		{
			Dictionary<ProgressionIndustryComponent, int> dictionary2 = new Dictionary<ProgressionIndustryComponent, int>();
			int? num = ActivePhaseIndexForSection(section5);
			for (int k = 0; k < section5.deliveryPhases.Length; k++)
			{
				ProgressionIndustryComponent industryComponent = section5.deliveryPhases[k].industryComponent;
				if (!(industryComponent == null))
				{
					if (!dictionary2.TryGetValue(industryComponent, out var value))
					{
						value = -1;
					}
					int? num2 = num;
					j = k;
					if (num2 == j)
					{
						dictionary2[industryComponent] = num.Value;
					}
					else
					{
						dictionary2[industryComponent] = value;
					}
				}
			}
			foreach (KeyValuePair<ProgressionIndustryComponent, int> item in dictionary2)
			{
				item.Deconstruct(out var key, out j);
				ProgressionIndustryComponent progressionIndustryComponent = key;
				bool flag2 = j != num;
				if (flag2 != progressionIndustryComponent.ProgressionDisabled)
				{
					flag = true;
				}
				progressionIndustryComponent.ProgressionDisabled = flag2;
			}
			if (num.HasValue)
			{
				int value2 = num.Value;
				ConfigureIndustry(section5, value2);
			}
		}
		if (flag)
		{
			Messenger.Default.Send(default(IndustriesDidChange));
		}
	}

	private int? ActivePhaseIndexForSection(Section section)
	{
		if (!section.Available)
		{
			return null;
		}
		if (section.FulfilledCount == section.PaidCount)
		{
			return null;
		}
		int num = section.PaidCount - 1;
		if (num < 0 || num >= section.deliveryPhases.Length)
		{
			return null;
		}
		return num;
	}

	private void ConfigureIndustry(Section section, int phaseIndex)
	{
		Section.DeliveryPhase deliveryPhase = section.deliveryPhases[phaseIndex];
		if (deliveryPhase.deliveries.Length != 0)
		{
			deliveryPhase.industryComponent.Configure(section, phaseIndex, deliveryPhase, delegate
			{
				PhaseCompleted(section, phaseIndex);
			}, _keyValueObject);
		}
	}

	private bool PrerequisitesMet(Section section)
	{
		return section.prerequisiteSections.All((Section s) => s.Unlocked);
	}

	private void PhaseCompleted(Section section, int phaseIndex)
	{
		if (!StateManager.IsHost)
		{
			return;
		}
		if (section.FulfilledCount >= phaseIndex + 1)
		{
			Log.Debug("Section {section} phase {phaseIndex} called but phase is already fulfilled.", section, phaseIndex);
			return;
		}
		Log.Information("Section {section} phase {phaseIndex} fulfilled!", section, phaseIndex);
		section.FulfilledCount = phaseIndex + 1;
		SetFulfilledDeliveriesForSectionId(section.identifier, section.FulfilledCount);
		string message;
		if (section.FulfilledCount == section.PhaseCount)
		{
			Log.Information("Section {section} unlocked! {fulfilledCount} fulfilled out of {phaseCount}", section, section.FulfilledCount, section.PhaseCount);
			UnlockedSectionIds = UnlockedSectionIds.Concat(new string[1] { section.identifier }).ToHashSet();
			section.ApplyCompleted();
			message = ((section.PhaseCount <= 1) ? (section.displayName + " has been completed!") : ("The final phase of " + section.displayName + " has been completed!"));
		}
		else
		{
			message = $"Phase {phaseIndex + 1} of {section.displayName} has been completed!";
		}
		Multiplayer.Broadcast(message);
		UpdateSectionStates();
		StateManager.Shared.SendFireEvent(default(ProgressionStateDidChange));
	}

	public void HandlePayToStartPhase(string sectionIdentifier, int phaseIndex, IPlayer sender)
	{
		if (!StateManager.IsHost)
		{
			return;
		}
		Section section = Sections.FirstOrDefault((Section s) => s.identifier == sectionIdentifier);
		if (section == null)
		{
			Multiplayer.SendError(sender, "Internal error, check log");
			Log.Error("Unrecognized section identifier: {id}", sectionIdentifier);
			return;
		}
		try
		{
			PayToStartPhase(section, phaseIndex, sender);
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Error starting phase {phase}", phaseIndex);
			if (ex is DisplayableException ex2)
			{
				Multiplayer.SendError(sender, ex2.DisplayMessage);
			}
			else
			{
				Multiplayer.SendError(sender, "Unable to start phase");
			}
		}
	}

	public int CostForPhase(Section.DeliveryPhase phase, out float discountPercent)
	{
		discountPercent = ReputationTracker.Shared.PhaseDiscount();
		return Mathf.RoundToInt((float)phase.cost * (1f - discountPercent));
	}
}
