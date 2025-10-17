using System;
using Serilog;
using UI;
using UI.CarInspector;
using UI.Common;
using UI.CompanyWindow;
using UI.Guide;
using UI.Timetable;
using UnityEngine;

namespace Helpers;

public static class LinkDispatcher
{
	public static void Open(Hyperlink link)
	{
		Open(link.Address);
	}

	public static void Open(string link)
	{
		Log.Information("LinkDispatcher.Open: '{link}'", link);
		EntityReference r;
		if (link.StartsWith("http:") || link.StartsWith("https:"))
		{
			Application.OpenURL(link);
		}
		else if (!EntityReference.TryParseURI(link, out r))
		{
			Log.Error("Failed to parse link: {link}", link);
		}
		else
		{
			Open(r);
		}
	}

	public static void Open(EntityType entityType, string id)
	{
		Open(new EntityReference(entityType, id));
	}

	private static void Open(EntityReference entityReference)
	{
		switch (entityReference.Type)
		{
		case EntityType.Help:
			GuideWindow.Show(entityReference.Id);
			break;
		case EntityType.Industry:
			CompanyWindow.Shared.ShowIndustry(entityReference.Id);
			break;
		case EntityType.PassengerStop:
			Log.Error("Open passenger stop link not supported");
			break;
		case EntityType.Car:
		{
			if (TrainController.Shared.TryGetCarForId(entityReference.Id, out var car))
			{
				if (GameInput.IsShiftDown)
				{
					TrainController.Shared.SelectedCar = car;
				}
				else
				{
					CarInspector.Show(car);
				}
			}
			else
			{
				Toast.Present("Car not found.");
			}
			break;
		}
		case EntityType.Crew:
			CompanyWindow.Shared.ShowCrew(entityReference.Id);
			break;
		case EntityType.Player:
			CompanyWindow.Shared.ShowPlayer(entityReference.Id);
			break;
		case EntityType.Position:
		{
			if (entityReference.TryParseVector4(out var vector))
			{
				CameraSelector.shared.JumpToPoint(vector, Quaternion.Euler(0f, vector.w, 0f), CameraSelector.CameraIdentifier.Strategy);
				break;
			}
			throw new ArgumentException("Failed to parse position from link");
		}
		case EntityType.Timetable:
			TimetableWindow.Shared.Show();
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}
}
