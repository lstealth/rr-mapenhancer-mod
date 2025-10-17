using System;
using System.Collections.Generic;
using System.Linq;
using Game.Messages;
using Game.Reputation;
using KeyValue.Runtime;
using Model;
using Model.Database;
using Model.Definition;
using Model.Definition.Data;
using Model.Ops;
using Network;
using Serilog;
using Track;
using UnityEngine;

namespace Game.State;

public static class EquipmentPurchase
{
	public static void HandleRequest(IPlayer sender, RequestPurchaseEquipment request)
	{
		TrainController shared = TrainController.Shared;
		IPrefabStore prefabStore = shared.PrefabStore;
		StateManager shared2 = StateManager.Shared;
		try
		{
			List<CarDescriptor> list = CarDescriptorsFromRequest(request, prefabStore);
			Interchange chosenInterchange;
			List<(Location, List<CarDescriptor>)> list2 = shared.FindTracksForCars(list, out chosenInterchange);
			if (list2 == null)
			{
				Multiplayer.Broadcast("No tracks available for purchase.");
				Log.Error("Unable to find space for purchase: {descriptors}", list);
				return;
			}
			int discount;
			int num = list.Select((CarDescriptor desc) => PurchasePriceForCarPrototype(desc.DefinitionInfo.Definition, out discount)).Sum();
			if (!shared2.CanAfford(num))
			{
				Multiplayer.Broadcast($"Not enough funds for purchase. Balance {shared2.Balance:C0} is less than {num:C0}.");
				return;
			}
			Log.Information("Purchase by {purchaser}: {descriptors} for {amount}", sender, list, num);
			try
			{
				foreach (var (loc, descriptors) in list2)
				{
					shared.PlaceTrain(loc, descriptors, null, 0.25f);
				}
			}
			catch (Exception exception)
			{
				Multiplayer.Broadcast("Error placing purchased equipment.");
				Debug.LogException(exception);
				Log.Error(exception, "Error placing purchased equipment: {location}, {descriptors}", list2, list);
				throw;
			}
			Hyperlink hyperlink = Hyperlink.To(sender);
			string name = list[0].DefinitionInfo.Metadata.Name;
			Hyperlink hyperlink2 = Hyperlink.To(chosenInterchange.Industry);
			Multiplayer.Broadcast($"{hyperlink} ordered a shiny new {name} delivered to {hyperlink2}.");
			StateManager.Shared.ApplyToBalance(-num, Ledger.Category.Equipment, null, name);
		}
		catch (Exception exception2)
		{
			Debug.LogException(exception2);
			Log.Error(exception2, "Error in HandlePurchaseEquipment");
		}
	}

	public static int PurchasePriceForCarPrototype(CarDefinition prototype, out int discount)
	{
		int basePrice = prototype.BasePrice;
		discount = Mathf.FloorToInt((float)basePrice * ReputationTracker.Shared.EquipmentDiscount());
		return basePrice - discount;
	}

	public static int TradeInValueForCar(Car car)
	{
		float num = Mathf.Lerp(0.25f, 0.75f, car.Condition * car.RepairCap);
		return Mathf.RoundToInt((float)car.Definition.BasePrice * num);
	}

	public static bool CarCanBeSold(Car car)
	{
		if (car.IsOwnedByPlayer)
		{
			return car.Archetype != CarArchetype.Tender;
		}
		return false;
	}

	private static List<CarDescriptor> CarDescriptorsFromRequest(RequestPurchaseEquipment request, IPrefabStore prefabStore)
	{
		string reportingMark = StateManager.Shared.RailroadMark;
		List<CarDescriptor> list = new List<CarDescriptor>();
		for (int i = 0; i < request.PrototypeIds.Count; i++)
		{
			TypedContainerItem<CarDefinition> typedContainerItem = prefabStore.CarDefinitionInfoForIdentifier(request.PrototypeIds[i]);
			list.Add(MakeDescriptor(typedContainerItem));
			if (typedContainerItem.Definition.TryGetTenderIdentifier(out var tenderIdentifier))
			{
				TypedContainerItem<CarDefinition> carDefinitionInfo = prefabStore.CarDefinitionInfoForIdentifier(tenderIdentifier);
				list.Add(MakeDescriptor(carDefinitionInfo));
			}
		}
		return list;
		CarDescriptor MakeDescriptor(TypedContainerItem<CarDefinition> definitionInfo)
		{
			Dictionary<string, Value> properties = new Dictionary<string, Value> { 
			{
				"owned",
				Value.Bool(value: true)
			} };
			return new CarDescriptor(definitionInfo, new CarIdent(reportingMark, null), null, null, flipped: false, properties);
		}
	}
}
