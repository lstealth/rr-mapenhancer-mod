using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Game;
using Game.Events;
using Game.Messages;
using Game.State;
using Model;
using Model.AI;
using Model.Ops.Timetable;
using Serilog;
using TMPro;
using UI.Builder;
using UI.Common;
using UnityEngine.Pool;

namespace UI.CompanyWindow;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct CrewsPanelBuilder
{
	private static bool CanEditTrainCrews => StateManager.HasTrainmasterAccess;

	public static void Build(UIPanelBuilder builder, UIState<string> selectedTrainCrewId)
	{
		builder.RebuildOnEvent<TrainCrewsDidChange>();
		StateManager shared = StateManager.Shared;
		TrainController trainController = TrainController.Shared;
		PlayersManager playersManager = shared.PlayersManager;
		PlayerId myPlayerId = PlayersManager.PlayerId;
		TimetableController timetableController = TimetableController.Shared;
		List<BaseLocomotive> aeActiveLocomotives = CollectionPool<List<BaseLocomotive>, BaseLocomotive>.Get();
		foreach (Car car in trainController.Cars)
		{
			if (!(car is BaseLocomotive baseLocomotive))
			{
				continue;
			}
			try
			{
				Orders orders = new AutoEngineerPersistence(baseLocomotive.KeyValueObject).Orders;
				if (orders.Enabled && orders.MaxSpeedMph != 0)
				{
					aeActiveLocomotives.Add(baseLocomotive);
				}
			}
			catch (Exception exception)
			{
				Log.Error(exception, "Exception checking car {loco}", car);
			}
		}
		List<UIPanelBuilder.ListItem<TrainCrew>> items = (from tc in playersManager.TrainCrews
			orderby (!HasMembers(tc)) ? 1 : 0, tc.Name
			select tc).Select(delegate(TrainCrew crew)
		{
			string text = crew.Name;
			if (timetableController.TryGetTrainForTrainCrew(crew, out var timetableTrain))
			{
				text = text + " (Train " + timetableTrain.DisplayStringShort + ")";
			}
			return new UIPanelBuilder.ListItem<TrainCrew>(crew.Id, crew, HasMembers(crew) ? "Active" : "Not Active", text);
		}).ToList();
		CollectionPool<List<BaseLocomotive>, BaseLocomotive>.Release(aeActiveLocomotives);
		aeActiveLocomotives = null;
		builder.AddListDetail(items, selectedTrainCrewId, delegate(UIPanelBuilder uIPanelBuilder, TrainCrew trainCrew)
		{
			if (trainCrew == null)
			{
				uIPanelBuilder.AddExpandingVerticalSpacer();
				uIPanelBuilder.AddLabelEmptyState(items.Any() ? "No crew selected" : "No crews configured");
			}
			else
			{
				uIPanelBuilder.Spacing = 8f;
				uIPanelBuilder.AddLabel("<style=H1>" + trainCrew.Name + "</style>");
				uIPanelBuilder.AddLabelMarkup(trainCrew.Description).Width(500f);
				if (CanEditTrainCrews)
				{
					uIPanelBuilder.ButtonStrip(delegate(UIPanelBuilder uIPanelBuilder2)
					{
						uIPanelBuilder2.Spacer();
						uIPanelBuilder2.AddButton("Edit", delegate
						{
							ActionRenameTrainCrew(trainCrew);
						});
						uIPanelBuilder2.AddButton("Delete", delegate
						{
							ActionDeleteTrainCrew(trainCrew);
						});
					});
				}
				Model.Ops.Timetable.Timetable timetable = timetableController.Current;
				if (timetable != null)
				{
					uIPanelBuilder.AddSection("Timetable", delegate(UIPanelBuilder builder2)
					{
						BuildTimetableSymbolField(builder2, timetable, trainCrew);
					});
				}
				uIPanelBuilder.AddSection("Crew", delegate(UIPanelBuilder uIPanelBuilder2)
				{
					foreach (PlayerId memberPlayerId in trainCrew.MemberPlayerIds)
					{
						IPlayer player = playersManager.PlayerForId(memberPlayerId);
						if (player != null)
						{
							uIPanelBuilder2.AddLabel(Hyperlink.To(player));
						}
					}
					uIPanelBuilder2.Spacer(8f);
					uIPanelBuilder2.ButtonStrip(delegate(UIPanelBuilder uIPanelBuilder3)
					{
						if (StateManager.CheckAuthorizedToSendMessage(new RequestSetTrainCrewMembership(myPlayerId.String, null, join: true)))
						{
							if (trainCrew.MemberPlayerIds.Contains(myPlayerId))
							{
								uIPanelBuilder3.AddButton("Leave", delegate
								{
									RequestSetMembership(trainCrew, join: false);
								});
							}
							else
							{
								uIPanelBuilder3.AddButton("Join", delegate
								{
									RequestSetMembership(trainCrew, join: true);
								});
							}
						}
						else
						{
							uIPanelBuilder3.AddLabel("<i>Trainmaster and above manage crew assignments.</i>");
						}
					});
				});
				uIPanelBuilder.AddSection("Equipment", delegate(UIPanelBuilder uIPanelBuilder2)
				{
					List<Car> list = (from car in trainController.Cars
						where car.trainCrewId == trainCrew.Id
						orderby car.SortName
						select car).ToList();
					if (list.Count > 0)
					{
						foreach (Car item in list)
						{
							uIPanelBuilder2.AddLabel(Hyperlink.To(item));
						}
						return;
					}
					uIPanelBuilder2.AddLabel("<alpha=#66><i>Assign equipment to a crew using the Operations tab in the car's Inspector.</i>");
				});
			}
			uIPanelBuilder.AddExpandingVerticalSpacer();
			if (CanEditTrainCrews)
			{
				uIPanelBuilder.AddButton("Create", ActionCreateTrainCrew);
			}
		});
		bool HasMembers(TrainCrew tc)
		{
			foreach (PlayerId memberPlayerId2 in tc.MemberPlayerIds)
			{
				if (playersManager.IsOnline(memberPlayerId2))
				{
					return true;
				}
			}
			foreach (BaseLocomotive item2 in aeActiveLocomotives)
			{
				if (item2.trainCrewId == tc.Id)
				{
					return true;
				}
			}
			return false;
		}
	}

	private static string DropdownLabelForTimetableTrain(Model.Ops.Timetable.Timetable.Train train)
	{
		GameDateTime now = TimeWeather.Now;
		GameDateTime? gameDateTime = null;
		GameDateTime? gameDateTime2 = null;
		string text = null;
		string text2 = null;
		for (int num = 0; num < train.Entries.Count; num++)
		{
			GameDateTime gameDateTime3 = train.GetGameDateTime(TimetableTimeType.Arrival, num, now);
			GameDateTime gameDateTime4 = train.GetGameDateTime(TimetableTimeType.Departure, num, now);
			if (gameDateTime2.HasValue)
			{
				GameDateTime valueOrDefault = gameDateTime2.GetValueOrDefault();
				if (!(gameDateTime4 < valueOrDefault))
				{
					goto IL_006f;
				}
			}
			gameDateTime2 = gameDateTime4;
			_ = train.Entries[num];
			goto IL_006f;
			IL_006f:
			if (gameDateTime.HasValue)
			{
				GameDateTime valueOrDefault2 = gameDateTime.GetValueOrDefault();
				if (!(gameDateTime3 > valueOrDefault2))
				{
					goto IL_00a8;
				}
			}
			gameDateTime = gameDateTime3;
			text = train.Entries[num].Station;
			goto IL_00a8;
			IL_00a8:
			if (gameDateTime4 > now)
			{
				text2 = "- Dep. " + train.Entries[num].Station + " " + gameDateTime4.TimeString();
				break;
			}
		}
		if (text2 == null)
		{
			text2 = ((!gameDateTime.HasValue) ? "- No Departures" : ("<alpha=#60>- Arr. " + text + " " + gameDateTime.GetValueOrDefault().TimeString()));
		}
		return train.DisplayStringLong + " " + text2;
	}

	public static void BuildTimetableSymbolField(UIPanelBuilder builder, Model.Ops.Timetable.Timetable timetable, TrainCrew trainCrew)
	{
		IConfigurableElement configurableElement;
		if (CanEditTrainCrews)
		{
			List<Model.Ops.Timetable.Timetable.Train> source = timetable.Trains.Values.OrderBy((Model.Ops.Timetable.Timetable.Train t) => t.SortName).ToList();
			List<string> list = source.Select(DropdownLabelForTimetableTrain).ToList();
			List<string> availableSymbolValues = source.Select((Model.Ops.Timetable.Timetable.Train t) => t.Name).ToList();
			list.Insert(0, "None");
			availableSymbolValues.Insert(0, null);
			int currentSelectedIndex = availableSymbolValues.IndexOf(trainCrew.TimetableSymbol);
			configurableElement = builder.AddField("Train Symbol", builder.AddDropdown(list, currentSelectedIndex, delegate(int selectedIndex)
			{
				string value = availableSymbolValues[selectedIndex];
				SetTimetableSymbol(trainCrew, value);
			}));
		}
		else
		{
			Model.Ops.Timetable.Timetable.Train train = timetable.Trains.Values.FirstOrDefault((Model.Ops.Timetable.Timetable.Train t) => t.Name == trainCrew.TimetableSymbol);
			configurableElement = builder.AddField("Train Symbol", string.IsNullOrEmpty(trainCrew.TimetableSymbol) ? "<i>None</i>" : ((train == null) ? trainCrew.TimetableSymbol : DropdownLabelForTimetableTrain(train)));
		}
		configurableElement.Tooltip("Train Symbol", "Assign a timetable train symbol in order to associate the crew with a timetable schedule. Passenger Auto Engineers will hold at stations until departure time.");
	}

	private static void SetTimetableSymbol(TrainCrew trainCrew, string value)
	{
		StateManager.ApplyLocal(new RequestSetTrainCrewTimetableSymbol(trainCrew.Id, value));
	}

	private static void ActionDeleteTrainCrew(TrainCrew trainCrew)
	{
		ModalAlertController.Present("Delete train crew?", "Train crew " + trainCrew.Name + " will be permanently deleted.", new(bool, string)[2]
		{
			(false, "Cancel"),
			(true, "Delete")
		}, delegate(bool delete)
		{
			if (delete)
			{
				StateManager.ApplyLocal(new RequestDeleteTrainCrew(trainCrew.Id));
			}
		});
	}

	private static void ActionRenameTrainCrew(TrainCrew trainCrew)
	{
		PresentTrainInfoAlert("Edit Crew", "Save", trainCrew.Name, trainCrew.Description, delegate(string trainName, string trainDesc)
		{
			StateManager.ApplyLocal(new RequestEditTrainCrew(trainCrew.Id, trainName, trainDesc));
		});
	}

	private static void PresentTrainInfoAlert(string title, string submitText, string editName, string editDesc, Action<string, string> onSubmit)
	{
		ModalAlertController.Present(delegate(UIPanelBuilder builder, Action dismiss)
		{
			builder.AddLabel(title, delegate(TMP_Text text)
			{
				text.fontSize = 22f;
				text.horizontalAlignment = HorizontalAlignmentOptions.Center;
			});
			builder.AddSection("Name", delegate(UIPanelBuilder uIPanelBuilder)
			{
				uIPanelBuilder.AddInputField(editName, delegate(string newName)
				{
					editName = newName;
				}, "Name", 20);
			});
			builder.AddSection("Description", delegate(UIPanelBuilder uIPanelBuilder)
			{
				uIPanelBuilder.AddMultilineTextEditor(editDesc, "Description", delegate(string newDesc)
				{
					editDesc = newDesc;
				}, delegate
				{
				}).Height(200f);
			});
			builder.Spacer(16f);
			builder.AlertButtons(delegate(UIPanelBuilder uIPanelBuilder)
			{
				uIPanelBuilder.AddButtonMedium("Cancel", dismiss);
				uIPanelBuilder.AddButtonMedium(submitText, delegate
				{
					onSubmit(editName.Trim(), editDesc.Trim());
					dismiss();
				});
			});
		}, 500);
	}

	private static void ActionCreateTrainCrew()
	{
		PresentTrainInfoAlert("Create Train Crew", "Create", "", "", delegate(string trainName, string trainDesc)
		{
			StateManager.ApplyLocal(new RequestCreateTrainCrew(new Snapshot.TrainCrew("", trainName, new HashSet<string> { PlayersManager.PlayerId.String }, trainDesc, null)));
		});
	}

	private static void RequestSetMembership(TrainCrew trainCrew, bool join)
	{
		StateManager.ApplyLocal(new RequestSetTrainCrewMembership(PlayersManager.PlayerId.String, trainCrew.Id, join));
	}
}
