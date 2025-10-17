using System;
using System.Collections.Generic;
using System.Linq;
using Game.Messages;
using Game.State;
using Model;
using Model.Database;
using Model.Definition;
using Model.Definition.Data;
using Model.Ops.Definition;
using Model.Physics;
using TMPro;
using UI.Builder;
using UI.Common;
using UnityEngine;

namespace UI.Equipment;

public class EquipmentWindow : MonoBehaviour, IBuilderWindow
{
	private class CatalogEntry
	{
		public TypedContainerItem<CarDefinition> CarDefinitionInfo;
	}

	private Window _window;

	private static EquipmentWindow _instance;

	private UIPanel _panel;

	private readonly List<CatalogEntry> _catalog = new List<CatalogEntry>();

	private readonly UIState<string> _selectedItem = new UIState<string>(null);

	private string _quantityId;

	private int _quantity = 1;

	private readonly Dictionary<string, string> _carTypeNames = new Dictionary<string, string>
	{
		{ "LS", "Locomotive: Steam" },
		{ "LD", "Locomotive: Diesel" },
		{ "PB", "PB: Passenger, Coach" },
		{ "PBO", "PBO: Passenger, Observation" },
		{ "FL", "FL: Flat, Logs" },
		{ "HM", "HM: Hopper, 2-bay" },
		{ "HT", "HT: Hopper, 3-bay" },
		{ "NE", "NE: Caboose" },
		{ "FB", "FB: Flat, Bulkhead" },
		{ "TM", "TM: Tank, Single Dome" },
		{ "XM", "XM: Boxcar" }
	};

	public UIBuilderAssets BuilderAssets { get; set; }

	public static EquipmentWindow Shared => WindowManager.Shared.GetWindow<EquipmentWindow>();

	private IPrefabStore PrefabStore => TrainController.Shared.PrefabStore;

	public void Show()
	{
		_window.Title = "Atlantic Locomotive Works";
		RebuildModel();
		_panel?.Dispose();
		_panel = UIPanel.Create(_window.contentRectTransform, BuilderAssets, BuildContent);
		_window.ShowWindow();
	}

	public static void Toggle()
	{
		if (Shared._window.IsShown)
		{
			Shared._window.CloseWindow();
		}
		else
		{
			Shared.Show();
		}
	}

	private void Awake()
	{
		_window = GetComponent<Window>();
	}

	private void OnDisable()
	{
		_panel?.Dispose();
		_panel = null;
	}

	private void BuildContent(UIPanelBuilder builder)
	{
		List<UIPanelBuilder.ListItem<CatalogEntry>> data = _catalog.Select(delegate(CatalogEntry catalogEntry)
		{
			string section = SectionName(catalogEntry.CarDefinitionInfo);
			return new UIPanelBuilder.ListItem<CatalogEntry>(catalogEntry.CarDefinitionInfo.Identifier, catalogEntry, section, catalogEntry.CarDefinitionInfo.Metadata.Name);
		}).ToList();
		builder.AddListDetail(data, _selectedItem, delegate(UIPanelBuilder uIPanelBuilder, CatalogEntry entry)
		{
			uIPanelBuilder.Spacing = 0f;
			if (entry == null)
			{
				uIPanelBuilder.AddLabel("Please select a catalog entry.");
			}
			else
			{
				TypedContainerItem<CarDefinition> carDefinitionInfo = entry.CarDefinitionInfo;
				if (_quantityId != carDefinitionInfo.Identifier)
				{
					_quantity = 1;
					_quantityId = carDefinitionInfo.Identifier;
				}
				if (carDefinitionInfo.Definition is SteamLocomotiveDefinition def)
				{
					BuildDetails(uIPanelBuilder, carDefinitionInfo.Identifier, carDefinitionInfo.Metadata, def);
				}
				else if (carDefinitionInfo.Definition is DieselLocomotiveDefinition def2)
				{
					BuildDetails(uIPanelBuilder, carDefinitionInfo.Identifier, carDefinitionInfo.Metadata, def2);
				}
				else
				{
					BuildDetails(uIPanelBuilder, carDefinitionInfo.Identifier, carDefinitionInfo.Metadata, carDefinitionInfo.Definition);
				}
				uIPanelBuilder.AddExpandingVerticalSpacer();
				int currentBalance = StateManager.Shared.Balance;
				int discountAmount;
				int quotedPrice = EquipmentPurchase.PurchasePriceForCarPrototype(entry.CarDefinitionInfo.Definition, out discountAmount);
				int total = quotedPrice * _quantity;
				int expectedBalance = currentBalance - total;
				bool hasFunds = expectedBalance >= 0;
				uIPanelBuilder.VStack(delegate(UIPanelBuilder uIPanelBuilder2)
				{
					uIPanelBuilder2.Spacing = 8f;
					AddLine("Price:", quotedPrice + discountAmount);
					if (discountAmount > 0)
					{
						AddLine("Discount:", discountAmount);
					}
					AddLine($"Total: {_quantity} x {quotedPrice:C0} =", total);
					uIPanelBuilder2.Spacer(2f);
					AddLine("Current Balance:", currentBalance);
					AddLine("Expected Balance:", expectedBalance);
					uIPanelBuilder2.Spacer(2f);
					void AddLine(string label, int value)
					{
						uIPanelBuilder2.AddLabel($"<style=p>{label}</style>  {value:C0}").HorizontalTextAlignment(HorizontalAlignmentOptions.Right);
					}
				});
				uIPanelBuilder.Spacer(8f);
				UIPanelBuilder outerBuilder = uIPanelBuilder;
				uIPanelBuilder.ButtonStrip(delegate(UIPanelBuilder uIPanelBuilder2)
				{
					bool flag = StateManager.CheckAuthorizedToSendMessage(new RequestPurchaseEquipment(new List<string>()));
					bool flag2 = hasFunds && flag;
					uIPanelBuilder2.Spacer();
					uIPanelBuilder2.AddButton("-", delegate
					{
						OffsetQuantity(-1, outerBuilder);
					}).Disable(_quantity <= 1);
					uIPanelBuilder2.AddLabel($"<size=110%>{_quantity}</size>").HorizontalTextAlignment(HorizontalAlignmentOptions.Center).VerticalTextAlignment(VerticalAlignmentOptions.Middle)
						.TextWrap(TextOverflowModes.Overflow, TextWrappingModes.NoWrap)
						.Width(25f)
						.Height(30f);
					uIPanelBuilder2.AddButton("+", delegate
					{
						OffsetQuantity(1, outerBuilder);
					}).Disable(_quantity >= 99);
					uIPanelBuilder2.Spacer(8f);
					uIPanelBuilder2.AddButton("Purchase", delegate
					{
						Purchase(entry, _quantity);
					}).Disable(!flag2);
				});
			}
		});
	}

	private void OffsetQuantity(int i, UIPanelBuilder builder)
	{
		_quantity = Mathf.Max(1, _quantity + i);
		builder.Rebuild();
	}

	private void BuildTractiveEffortInfo(UIPanelBuilder builder, float teLb, float weight)
	{
		if (teLb != 0f)
		{
			teLb = Mathf.RoundToInt(teLb / 100f) * 100;
			builder.AddField("Tractive Effort", $"{teLb:N0} lb").Tooltip("Tractive Effort", "Force in pounds the engine can generate when starting.");
			builder.AddField("Factor of Adhesion", $"{weight / teLb:N1}").Tooltip("Factor of Adhesion", "Engines with a higher factor of adhesion are less prone to slipping.");
		}
	}

	private void BuildDetails(UIPanelBuilder builder, string identifier, ObjectMetadata metadata, SteamLocomotiveDefinition def)
	{
		builder.AddTitle(metadata.Name, metadata.Description);
		AddPhotograph(builder, identifier);
		float startingTractiveEffort = GetStartingTractiveEffort(metadata, def);
		BuildTractiveEffortInfo(builder, startingTractiveEffort, def.WeightOnDrivers);
		BuildDetails(builder, (string)null, (ObjectMetadata)null, (CarDefinition)def);
		BuildDetailsTender(builder, def.TenderIdentifier);
	}

	private static float GetStartingTractiveEffort(ObjectMetadata meta, SteamLocomotiveDefinition def)
	{
		if (def.PublishedTractiveEffort > 0)
		{
			return def.PublishedTractiveEffort;
		}
		return TrainMath.TractiveEffort(new TrainMath.SteamEngineCharacteristics(2, def.PistonDiameterInches, def.PistonStrokeInches, def.Wheelsets[def.MainDriverIndex].Diameter * 39.37008f, def.TotalHeatingSurface, (def.PublishedTractiveEffort == 0) ? ((float?)null) : new float?(def.PublishedTractiveEffort)), 0f, def.MaximumBoilerPressure);
	}

	private void BuildDetailsTender(UIPanelBuilder builder, string tenderIdentifier)
	{
		if (string.IsNullOrEmpty(tenderIdentifier))
		{
			return;
		}
		ObjectMetadata metadata;
		CarDefinition tenderDef = PrefabStore.DefinitionForIdentifier<CarDefinition>(tenderIdentifier, out metadata);
		if (tenderDef != null)
		{
			builder.AddSection("Tender", delegate(UIPanelBuilder builder2)
			{
				BuildDetails(builder2, null, null, tenderDef);
			});
		}
	}

	private void BuildDetails(UIPanelBuilder builder, string identifier, ObjectMetadata metadata, DieselLocomotiveDefinition def)
	{
		builder.AddTitle(metadata.Name, metadata.Description);
		AddPhotograph(builder, identifier);
		BuildTractiveEffortInfo(builder, def.StartingTractiveEffort, def.WeightEmpty);
		BuildDetails(builder, (string)null, (ObjectMetadata)null, (CarDefinition)def);
	}

	private void BuildDetails(UIPanelBuilder builder, string identifier, ObjectMetadata metadata, CarDefinition def)
	{
		if (metadata != null)
		{
			builder.AddTitle(metadata.Name, def.CarType + " - " + def.Archetype.DisplayName());
			AddPhotograph(builder, identifier);
		}
		if (def.Archetype.IsLocomotive())
		{
			builder.AddField("Curve Radius", def.MinimumCurveRadius switch
			{
				CurveRadius.ExtraSmall => "XS", 
				CurveRadius.Small => "S", 
				CurveRadius.Medium => "M", 
				CurveRadius.Large => "L", 
				CurveRadius.ExtraLarge => "XL", 
				_ => throw new ArgumentOutOfRangeException(), 
			});
		}
		builder.AddField("Weight Empty", $"{def.WeightEmpty:N0} lb");
		foreach (var item2 in def.DisplayOrderLoadSlots())
		{
			LoadSlot item = item2.slot;
			string text = item.LoadUnits.QuantityString(item.MaximumCapacity);
			if (item.RequiredLoadIdentifier != null)
			{
				Load load = CarPrototypeLibrary.instance.LoadForId(item.RequiredLoadIdentifier);
				if (load != null)
				{
					text = text + " " + load.description;
				}
			}
			builder.AddField("Capacity", text);
		}
		string text2 = def.CarType switch
		{
			"FB" => "Use in captive service to supply customers that require pulpwood.", 
			"FL" => "Use in captive service to supply customers that require logs.", 
			"HM" => "Use in railroad fuel service to deliver coal to your coaling towers and conveyors.", 
			"HT" => "Use in railroad fuel service to deliver coal to your coaling towers and conveyors.", 
			"TM" => "Use in railroad fuel service to deliver diesel fuel to your diesel fuel stands.", 
			"PB" => "Regular passenger service is critical to keeping your railroad's reputation high.", 
			"PBO" => "Observation cars generate a 20% bonus to passenger fares when operated as the last car in a train.", 
			_ => null, 
		};
		if (text2 != null)
		{
			builder.AddField("Hint", text2);
		}
	}

	private void AddPhotograph(UIPanelBuilder builder, string identifier)
	{
		builder.AddBuilderPhoto(identifier);
	}

	private string SectionName(TypedContainerItem<CarDefinition> carDefinitionInfo)
	{
		string carType = carDefinitionInfo.Definition.CarType;
		if (_carTypeNames.TryGetValue(carType, out var value))
		{
			return value;
		}
		return carType;
	}

	private void Purchase(CatalogEntry selectedEntry, int quantity)
	{
		StateManager.ApplyLocal(new RequestPurchaseEquipment(Enumerable.Repeat(selectedEntry.CarDefinitionInfo.Identifier, quantity).ToList()));
		_window.CloseWindow();
	}

	private void RebuildModel()
	{
		List<TypedContainerItem<CarDefinition>> list = PrefabStore.AllCarDefinitionInfos.OrderBy((TypedContainerItem<CarDefinition> e) => e.Definition.Archetype.PlacerOrder()).ThenBy((TypedContainerItem<CarDefinition> b) => b.Definition.BasePrice).Where(ShouldShow)
			.ToList();
		_catalog.Clear();
		foreach (TypedContainerItem<CarDefinition> item in list)
		{
			_catalog.Add(new CatalogEntry
			{
				CarDefinitionInfo = item
			});
		}
	}

	private static bool ShouldShow(TypedContainerItem<CarDefinition> info)
	{
		if (info.Definition.VisibleInPlacer)
		{
			return info.Definition.BasePrice > 0;
		}
		return false;
	}
}
