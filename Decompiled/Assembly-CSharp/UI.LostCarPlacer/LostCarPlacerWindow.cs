using System.Collections.Generic;
using System.Linq;
using Core;
using Game.State;
using TMPro;
using UI.Builder;
using UI.Common;
using UnityEngine;

namespace UI.LostCarPlacer;

public class LostCarPlacerWindow : MonoBehaviour, IBuilderWindow
{
	private Window _window;

	private UIPanel _panel;

	private bool _pendingPlace;

	public UIBuilderAssets BuilderAssets { get; set; }

	private static LostCarPlacerWindow Instance => WindowManager.Shared.GetWindow<LostCarPlacerWindow>();

	public static void ShowIfNeeded()
	{
		if (TrainController.Shared.LostCarCuts.Any())
		{
			StateManager.AssertIsHost();
			LostCarPlacerWindow instance = Instance;
			if (!instance._pendingPlace)
			{
				instance.Populate();
				instance._window.ShowWindow();
			}
		}
	}

	private void Awake()
	{
		_window = GetComponent<Window>();
		_window.DelegateRequestClose = HandleWindowRequestClose;
	}

	private void OnDisable()
	{
		_panel?.Dispose();
		_panel = null;
	}

	private void Populate()
	{
		_panel?.Dispose();
		_window.Title = "Lost & Found";
		_panel = UIPanel.Create(_window.contentRectTransform, BuilderAssets, BuildWindowContent);
	}

	private void BuildWindowContent(UIPanelBuilder builder)
	{
		List<List<TrainController.LostCar>> cuts = TrainController.Shared.LostCarCuts;
		builder.AddLabel("Railroader was unable to restore the locations of " + cuts.Count.Pluralize("cut") + " of cars while loading this game, likely due to a track change.\n\n<i>Use the Place button below to place each cut of cars.</i>");
		builder.Spacer(8f);
		builder.VScrollView(delegate(UIPanelBuilder uIPanelBuilder)
		{
			uIPanelBuilder.Spacing = 4f;
			for (int i = 0; i < cuts.Count; i++)
			{
				int cutIndex = i;
				List<TrainController.LostCar> cut = cuts[cutIndex];
				uIPanelBuilder.HStack(delegate(UIPanelBuilder uIPanelBuilder2)
				{
					uIPanelBuilder2.VStack(delegate(UIPanelBuilder uIPanelBuilder3)
					{
						List<TrainController.LostCar> source = cut.OrderByDescending((TrainController.LostCar c) => c.Descriptor.DefinitionInfo.Definition.WeightEmpty).ToList();
						uIPanelBuilder3.AddLabel(string.Join(", ", source.Select((TrainController.LostCar c) => c.Descriptor.Ident))).TextWrap(TextOverflowModes.Ellipsis, TextWrappingModes.NoWrap);
						uIPanelBuilder3.AddLabel(string.Join(", ", source.Select((TrainController.LostCar c) => c.Descriptor.DefinitionInfo.Metadata.Name).Distinct())).TextWrap(TextOverflowModes.Ellipsis, TextWrappingModes.NoWrap);
					});
					uIPanelBuilder2.AddButton("Place\n" + cut.Count.Pluralize("car"), delegate
					{
						Place(cutIndex, cut);
					}).Width(80f);
				}).Height(54f);
			}
		});
	}

	private void Place(int cutIndex, List<TrainController.LostCar> cut)
	{
		_window.CloseWindow();
		Vector3? vector = null;
		foreach (TrainController.LostCar item in cut)
		{
			if (item.Position.HasValue)
			{
				vector = item.Position.Value;
				break;
			}
		}
		if (vector.HasValue)
		{
			CameraSelector.shared.JumpToPoint(vector.Value, Quaternion.identity, CameraSelector.CameraIdentifier.Strategy);
		}
		_pendingPlace = true;
		ConsistPlacer.Instance().Present(cut.Select((TrainController.LostCar lostCar) => lostCar.Descriptor), cut.Select((TrainController.LostCar lostCar) => lostCar.Id).ToList(), delegate(bool placed)
		{
			_pendingPlace = false;
			if (placed)
			{
				TrainController.Shared.LostCarCuts.RemoveAt(cutIndex);
			}
			ShowIfNeeded();
		});
	}

	private void HandleWindowRequestClose()
	{
		int count = TrainController.Shared.LostCarCuts.Count;
		if (count == 0)
		{
			_window.CloseWindow();
			return;
		}
		ModalAlertController.Present("Close without placing all Lost & Found cuts?", count.Pluralize("cut") + " have not been placed.", new List<(int, string)>
		{
			(0, "Cancel"),
			(1, "Close")
		}, delegate(int choice)
		{
			if (choice != 0 && choice == 1)
			{
				_window.CloseWindow();
			}
		});
	}
}
