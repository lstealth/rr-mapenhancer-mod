using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Model;
using Model.Definition;
using Model.Ops;
using Serilog;
using UI.Builder;
using UI.CarInspector;

namespace UI.CompanyWindow;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct EquipmentPanelBuilder
{
	private enum Section
	{
		Locomotive,
		Passenger,
		Caboose,
		Other
	}

	public static void Build(UIPanelBuilder builder, UIState<string> selectedCar)
	{
		TrainController shared = TrainController.Shared;
		List<UIPanelBuilder.ListItem<Car>> items = shared.Cars.Where(ShouldShowCar).OrderBy(SectionForCar).ThenBy((Car car) => car.SortName)
			.Select(delegate(Car car)
			{
				string section = StringForSection(SectionForCar(car));
				string text = car.DisplayName + " - " + TagNameForCar(car);
				return new UIPanelBuilder.ListItem<Car>(car.id, car, section, text);
			})
			.ToList();
		builder.AddListDetail(items, selectedCar, delegate(UIPanelBuilder builder2, Car car)
		{
			if (car == null)
			{
				builder2.AddLabel((items.Count > 0) ? "Please select a car." : "No equipment!");
			}
			else
			{
				string subtitle = TagNameForCar(car);
				builder2.AddTitle(car.DisplayName, subtitle);
				if (car.IsInBardo)
				{
					builder2.AddField("Off Railroad", "This equipment will return once its waybill is fulfilled.");
					builder2.Spacer(10f);
					string bardo = car.Bardo;
					if (OpsController.Shared.TryDecodeBardo(bardo, out var opsCarPosition, out var returnTo, out var consignee))
					{
						OpsCarPositionDisplayable destinationDisplayable = new OpsCarPositionDisplayable(opsCarPosition);
						builder2.AddField("Location", consignee);
						builder2.AddField("Returns To", builder2.AddLocationField(returnTo, destinationDisplayable, delegate
						{
							CameraSelector.shared.JumpTo(destinationDisplayable);
						}));
					}
					else
					{
						Log.Error("Couldn't resolve position for car {car} in bardo {bardoId}", car, bardo);
					}
				}
				else
				{
					builder2.AddTrainCrewDropdown(car);
					builder2.AddSection("Condition & Repair", delegate(UIPanelBuilder builder3)
					{
						builder3.AddConditionField(car);
						builder3.AddMileageField(car);
						builder3.AddRepairDestination(car);
					});
					builder2.AddSection("Sell", delegate(UIPanelBuilder builder3)
					{
						builder3.AddSellDestination(car);
					});
					builder2.ButtonStrip(delegate(UIPanelBuilder uIPanelBuilder)
					{
						uIPanelBuilder.AddButton("Select", delegate
						{
							SelectCar(car);
						});
						uIPanelBuilder.AddButton("Inspect", delegate
						{
							InspectCar(car);
						});
						uIPanelBuilder.AddButton("Follow", delegate
						{
							FollowCar(car);
						});
					});
				}
			}
		});
	}

	private static string TagNameForCar(Car car)
	{
		if (!string.IsNullOrEmpty(car.DefinitionInfo.Metadata.Name))
		{
			return car.DefinitionInfo.Metadata.Name;
		}
		return car.CarType;
	}

	private static void FollowCar(Car car)
	{
		CameraSelector.shared.FollowCar(car);
	}

	private static void InspectCar(Car car)
	{
		UI.CarInspector.CarInspector.Show(car);
	}

	private static void SelectCar(Car car)
	{
		TrainController.Shared.SelectedCar = car;
	}

	private static bool ShouldShowCar(Car car)
	{
		if (car.IsOwnedByPlayer)
		{
			return car.Archetype != CarArchetype.Tender;
		}
		return false;
	}

	private static Section SectionForCar(Car car)
	{
		return car.Archetype switch
		{
			CarArchetype.LocomotiveSteam => Section.Locomotive, 
			CarArchetype.LocomotiveDiesel => Section.Locomotive, 
			CarArchetype.Coach => Section.Passenger, 
			CarArchetype.Baggage => Section.Passenger, 
			CarArchetype.Caboose => Section.Caboose, 
			_ => Section.Other, 
		};
	}

	private static string StringForSection(Section section)
	{
		return section switch
		{
			Section.Locomotive => "Locomotives", 
			Section.Passenger => "Passenger", 
			Section.Caboose => "Caboose", 
			Section.Other => "Other", 
			_ => throw new ArgumentOutOfRangeException("section", section, null), 
		};
	}
}
