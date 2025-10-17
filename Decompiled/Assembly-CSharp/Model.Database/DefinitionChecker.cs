using System.Collections.Generic;
using System.Linq;
using AssetPack.Runtime;
using Model.Definition;
using Model.Definition.Components;
using Model.Definition.Data;
using Serilog;

namespace Model.Database;

internal class DefinitionChecker
{
	private readonly List<string> _warnings;

	private readonly List<string> _errors;

	private readonly string _objectIdentifier;

	private readonly string _packIdentifier;

	private readonly AssetPackRuntimeStore _store;

	private static readonly string[] PricedFreightCarTypes = new string[6] { "FB", "FL", "HM", "HT", "TM", "XM" };

	public IReadOnlyCollection<string> Warnings => _warnings;

	public IReadOnlyCollection<string> Errors => _errors;

	public DefinitionChecker(string objectIdentifier, string packIdentifier, AssetPackRuntimeStore store)
	{
		_objectIdentifier = objectIdentifier;
		_packIdentifier = packIdentifier;
		_store = store;
		_warnings = new List<string>();
		_errors = new List<string>();
	}

	public void Check(ObjectDefinition definition)
	{
		if (definition is SteamLocomotiveDefinition definition2)
		{
			CheckSteamLocomotive(definition2);
		}
		else if (definition is CarDefinition definition3)
		{
			CheckCar(definition3);
		}
	}

	private void CheckCar(CarDefinition definition)
	{
		if (definition.Archetype.IsFreight())
		{
			if (definition.LoadSlots.Count == 0)
			{
				Error($"LoadSlots is empty on freight car {definition.Archetype}, {definition.CarType}");
			}
			if (definition.BasePrice > 0 && !PricedFreightCarTypes.Contains(definition.CarType))
			{
				Warning($"Freight car has non-zero price: {definition.BasePrice}");
			}
		}
		if (definition.Archetype == CarArchetype.Tender)
		{
			if (definition.BasePrice > 0)
			{
				Warning($"Tender price should be zero: {definition.BasePrice}");
			}
			CheckHasFuelSlots(definition);
		}
		if (string.IsNullOrEmpty(definition.ModelIdentifier))
		{
			Error("ModelIdentifier is empty");
		}
		else if (!_store.Catalog().assets.ContainsKey(definition.ModelIdentifier))
		{
			Error("ModelIdentifier '" + definition.ModelIdentifier + "' not found in asset pack");
		}
		foreach (LoadSlot loadSlot in definition.LoadSlots)
		{
			LoadUnits? loadUnits = loadSlot.RequiredLoadIdentifier switch
			{
				"coal" => LoadUnits.Pounds, 
				"water" => LoadUnits.Gallons, 
				"logs" => LoadUnits.Quantity, 
				_ => null, 
			};
			if (loadUnits.HasValue)
			{
				Assert(loadSlot.LoadUnits == loadUnits.Value, $"Slot requires {loadSlot.RequiredLoadIdentifier} but units are {loadSlot.LoadUnits}");
			}
			Assert(loadSlot.MaximumCapacity > 0.1f, $"Slot capacity is very small: {loadSlot.MaximumCapacity:F3}");
		}
	}

	private void CheckSteamLocomotive(SteamLocomotiveDefinition definition)
	{
		if (definition.BasePrice == 0)
		{
			Warning("Base price of locomotive is zero.");
		}
		if (string.IsNullOrEmpty(definition.TenderIdentifier))
		{
			List<LoadSlot> loadSlots = definition.LoadSlots;
			if (loadSlots.Count != 2)
			{
				Error($"Locomotive has no tender and {loadSlots.Count} (!= 2) LoadSlots");
			}
			else
			{
				CheckHasFuelSlots(definition);
			}
		}
		else if (_store.ContainerItemForObjectIdentifier(definition.TenderIdentifier) == null)
		{
			Error("Can't find tender in this pack: '" + definition.TenderIdentifier + "'");
		}
		CheckCar(definition);
	}

	private void CheckHasFuelSlots(CarDefinition definition)
	{
		List<LoadSlot> loadSlots = definition.LoadSlots;
		if (loadSlots.Count != 2)
		{
			Error($"Tender should have 2 slots, found {loadSlots.Count}");
		}
		else
		{
			Assert(loadSlots.Count((LoadSlot slot) => slot.RequiredLoadIdentifier == "coal") == 1, "Must have coal slot.");
			Assert(loadSlots.Count((LoadSlot slot) => slot.RequiredLoadIdentifier == "water") == 1, "Must have one water.");
		}
		List<LoadTargetComponent> source = definition.Components.OfType<LoadTargetComponent>().ToList();
		Assert(source.Count((LoadTargetComponent t) => t.SlotIndex == 0) == 1, "Expected one LoadTarget for coal");
		Assert(source.Count((LoadTargetComponent t) => t.SlotIndex == 1) == 1, "Expected one LoadTarget for water");
	}

	private void Assert(bool condition, string message)
	{
		if (!condition)
		{
			Error(message);
		}
	}

	private void Error(string message)
	{
		_errors.Add(message);
	}

	private void Warning(string message)
	{
		_warnings.Add(message);
	}

	public void PrintToLog()
	{
		if (_warnings.Count != 0 || _errors.Count != 0)
		{
			Log.Information("Check report for {objectIdentifier} in {packIdentifier} {location}", _objectIdentifier, _packIdentifier, _store.Location);
			if (_errors.Count > 0)
			{
				Log.Error("Found errors on {objectIdentifier}: {errors}", _objectIdentifier, _errors);
			}
			if (_warnings.Count > 0)
			{
				Log.Warning("Found warnings on {objectIdentifier}: {warnings}", _objectIdentifier, _warnings);
			}
		}
	}

	public void PrintToConsole()
	{
		if (_warnings.Count == 0 && _errors.Count == 0)
		{
			return;
		}
		Console.Log($"Check report for {_objectIdentifier} in {_packIdentifier} {_store.Location}");
		foreach (string error in _errors)
		{
			Console.Log("ERR: " + error);
		}
		foreach (string warning in _warnings)
		{
			Console.Log("WARN: " + warning);
		}
	}
}
