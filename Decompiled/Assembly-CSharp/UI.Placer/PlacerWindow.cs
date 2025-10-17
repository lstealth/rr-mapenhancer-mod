using System;
using System.Collections.Generic;
using System.Linq;
using KeyValue.Runtime;
using Model;
using Model.Database;
using Model.Definition;
using Model.Definition.Data;
using Model.Ops;
using TMPro;
using UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Placer;

public class PlacerWindow : MonoBehaviour
{
	public enum Filter
	{
		None = -1,
		Locomotives,
		Freight,
		Passenger,
		Misc
	}

	private struct ConsistEntry
	{
		public TypedContainerItem<CarDefinition> DefinitionInfo;

		public CarDefinition Definition => DefinitionInfo.Definition;
	}

	public RectTransform libraryScrollContent;

	public LibraryRow libraryRowTemplate;

	public RectTransform consistScrollContent;

	public LibraryRow consistRowTemplate;

	public Button placeButton;

	public TMP_Text summaryLabel;

	public Toggle[] filterToggles;

	private Window _window;

	private readonly List<ConsistEntry> _consist = new List<ConsistEntry>();

	private Filter _filter = Filter.None;

	private IPrefabStore PrefabStore => TrainController.Shared.PrefabStore;

	private static PlacerWindow instance => WindowManager.Shared.GetWindow<PlacerWindow>();

	private void _Show()
	{
		RebuildLibrary();
		RebuildConsist();
		UpdateFilterButtons();
		_window.Title = "Consist Placer";
		_window.ShowWindow();
	}

	public static void Toggle()
	{
		if (instance._window.IsShown)
		{
			instance._window.CloseWindow();
		}
		else
		{
			instance._Show();
		}
	}

	private void Start()
	{
		_window = GetComponent<Window>();
		_window.CloseWindow();
		libraryRowTemplate.gameObject.SetActive(value: false);
	}

	public void PlaceConsist()
	{
		List<CarDescriptor> list = new List<CarDescriptor>();
		Dictionary<string, Value> dictionary = new Dictionary<string, Value> { 
		{
			"owned",
			Value.Bool(value: true)
		} };
		foreach (ConsistEntry item in _consist)
		{
			TypedContainerItem<CarDefinition> definitionInfo = item.DefinitionInfo;
			Dictionary<string, Value> properties = dictionary;
			list.Add(new CarDescriptor(definitionInfo, default(CarIdent), null, null, flipped: false, properties));
			if (item.DefinitionInfo.Definition.TryGetTenderIdentifier(out var tenderIdentifier))
			{
				TypedContainerItem<CarDefinition> definitionInfo2 = PrefabStore.CarDefinitionInfoForIdentifier(tenderIdentifier);
				properties = dictionary;
				list.Add(new CarDescriptor(definitionInfo2, default(CarIdent), null, null, flipped: false, properties));
			}
		}
		_window.CloseWindow();
		ConsistPlacer.Instance().Present(list);
	}

	public void ClearConsist()
	{
		_consist.Clear();
		RebuildConsist();
	}

	public void ShuffleConsist()
	{
		for (int i = 0; i < _consist.Count; i++)
		{
			if (!IsShufflable(_consist[i]))
			{
				continue;
			}
			int num = i;
			int num2 = i;
			for (int j = i + 1; j < _consist.Count; j++)
			{
				if (!IsShufflable(_consist[j]))
				{
					num2 = j - 1;
					break;
				}
			}
			if (num2 == i)
			{
				num2 = _consist.Count - 1;
			}
			if (num2 - num <= 1)
			{
				continue;
			}
			Debug.Log($"Shuffling from {num} to {num2}");
			for (int k = num; k <= num2; k++)
			{
				int num3 = UnityEngine.Random.Range(num, num2 + 1);
				if (num3 != k)
				{
					List<ConsistEntry> consist = _consist;
					int index = k;
					List<ConsistEntry> consist2 = _consist;
					int index2 = num3;
					ConsistEntry consistEntry = _consist[num3];
					ConsistEntry consistEntry2 = _consist[k];
					ConsistEntry consistEntry3 = (consist[index] = consistEntry);
					consistEntry3 = (consist2[index2] = consistEntry2);
				}
			}
			i = num2;
		}
		RebuildConsist();
		static bool IsShufflable(ConsistEntry entry)
		{
			if (!entry.Definition.Archetype.IsFreight())
			{
				return entry.Definition.IsPassengerCar();
			}
			return true;
		}
	}

	public void FilterDidChange(int value)
	{
		if (filterToggles[value].isOn)
		{
			_filter = (Filter)value;
		}
		else
		{
			_filter = Filter.None;
		}
		RebuildLibrary();
	}

	private void RebuildLibrary()
	{
		libraryScrollContent.DestroyChildrenExcept(libraryRowTemplate);
		foreach (TypedContainerItem<CarDefinition> item in (from d in PrefabStore.AllCarDefinitionInfos.Where(FiltersAllow)
			orderby d.Definition.Archetype.PlacerOrder(), d.Definition.CarType
			select d).ThenBy((TypedContainerItem<CarDefinition> d) => d.Metadata.Name, StringComparer.Ordinal).ToList())
		{
			try
			{
				InstantiateCell(item);
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}
	}

	private void RebuildConsist()
	{
		consistScrollContent.DestroyChildrenExcept(consistRowTemplate);
		int num = 0;
		float num2 = 0f;
		float num3 = 0f;
		for (int i = 0; i < _consist.Count; i++)
		{
			ConsistEntry entry = _consist[i];
			try
			{
				InstantiateCell(entry, i);
				num += entry.Definition.WeightEmpty;
				num2 += entry.Definition.Length;
				num3 += 1f;
				if (entry.DefinitionInfo.Definition.TryGetTenderIdentifier(out var tenderIdentifier))
				{
					TypedContainerItem<CarDefinition> typedContainerItem = PrefabStore.CarDefinitionInfoForIdentifier(tenderIdentifier);
					num += typedContainerItem.Definition.WeightEmpty;
					num2 += typedContainerItem.Definition.Length;
					num3 += 1f;
				}
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}
		num2 += 1f * num3;
		placeButton.enabled = _consist.Count > 0;
		summaryLabel.text = $"{num3} cars, {num2 * 3.28084f:N0} ft, {num / 2000:N0} tons";
	}

	private void InstantiateCell(TypedContainerItem<CarDefinition> item)
	{
		LibraryRow libraryRow = UnityEngine.Object.Instantiate(libraryRowTemplate, libraryScrollContent);
		libraryRow.gameObject.SetActive(value: true);
		ConfigureRow(libraryRow, item);
		libraryRow.defaultActionButton.GetComponentInChildren<TMP_Text>().text = "+";
		libraryRow.OnDefaultAction = (Action)Delegate.Combine(libraryRow.OnDefaultAction, (Action)delegate
		{
			AddCar(item, (!GameInput.IsShiftDown) ? 1 : 5);
		});
	}

	private void InstantiateCell(ConsistEntry entry, int index)
	{
		LibraryRow libraryRow = UnityEngine.Object.Instantiate(consistRowTemplate, consistScrollContent);
		libraryRow.gameObject.SetActive(value: true);
		ConfigureRow(libraryRow, entry.DefinitionInfo);
		libraryRow.defaultActionButton.GetComponentInChildren<TMP_Text>().text = "-";
		libraryRow.OnDefaultAction = (Action)Delegate.Combine(libraryRow.OnDefaultAction, (Action)delegate
		{
			RemoveConsistEntry(index);
		});
	}

	private void ConfigureRow(LibraryRow row, TypedContainerItem<CarDefinition> definitionInfo)
	{
		string identifier = definitionInfo.Metadata.Name;
		if (string.IsNullOrEmpty(identifier))
		{
			identifier = definitionInfo.Identifier;
		}
		string text = identifier;
		if (definitionInfo.Definition.Archetype != CarArchetype.LocomotiveSteam && definitionInfo.Definition.Archetype != CarArchetype.LocomotiveDiesel)
		{
			text = "<b><size=80%>" + definitionInfo.Definition.CarType + "</size></b> " + identifier;
		}
		int num = definitionInfo.Definition.WeightEmpty;
		string tenderIdentifier;
		bool num2 = definitionInfo.Definition.TryGetTenderIdentifier(out tenderIdentifier);
		if (num2)
		{
			ObjectMetadata metadata;
			CarDefinition carDefinition = PrefabStore.DefinitionForIdentifier<CarDefinition>(tenderIdentifier, out metadata);
			num += carDefinition.WeightEmpty;
		}
		string text2 = $"{num / 2000:N0}T";
		if (num2)
		{
			text2 += " Engine + Tender";
		}
		row.titleLabel.text = text;
		row.subtitleLabel.text = text2;
	}

	private bool FiltersAllow(TypedContainerItem<CarDefinition> defInfo)
	{
		CarArchetype archetype = defInfo.Definition.Archetype;
		if (!defInfo.Definition.VisibleInPlacer)
		{
			return false;
		}
		return _filter switch
		{
			Filter.Locomotives => archetype.IsLocomotive(), 
			Filter.Freight => archetype.IsFreight(), 
			Filter.Passenger => defInfo.Definition.IsPassengerCar() || archetype == CarArchetype.Baggage, 
			Filter.Misc => archetype.IsMisc(), 
			_ => archetype != CarArchetype.Tender, 
		};
	}

	private void AddCar(TypedContainerItem<CarDefinition> defInfo, int count)
	{
		for (int i = 0; i < count; i++)
		{
			_consist.Add(new ConsistEntry
			{
				DefinitionInfo = defInfo
			});
		}
		RebuildConsist();
	}

	private void RemoveConsistEntry(int index)
	{
		_consist.RemoveAt(index);
		RebuildConsist();
	}

	private void UpdateFilterButtons()
	{
		for (int i = 0; i < filterToggles.Length; i++)
		{
			bool isOn = i == (int)_filter;
			filterToggles[i].isOn = isOn;
		}
	}
}
