using System.Collections.Generic;
using System.Linq;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Game.Messages;
using Game.State;
using Helpers;
using KeyValue.Runtime;
using Model;
using Model.Definition;
using Model.Definition.Components;
using Model.Definition.Data;
using RollingStock;
using RollingStock.Steam;
using UI.Builder;
using UI.Common;
using UnityEngine;

namespace UI.CarCustomizeWindow;

public class CarCustomizeWindow : MonoBehaviour, IBuilderWindow
{
	private Window _window;

	private UIPanel _panel;

	private readonly UIState<int> _selectedTabState = new UIState<int>(0);

	private Car _car;

	public const string LetteringBasicKey = "lettering.basic";

	public UIBuilderAssets BuilderAssets { get; set; }

	private static CarCustomizeWindow Instance => WindowManager.Shared.GetWindow<CarCustomizeWindow>();

	private static bool IsSandbox => StateManager.IsSandbox;

	private bool CanChangeReportingMark
	{
		get
		{
			if (CanCustomize(_car, out var _))
			{
				return CanRenumber;
			}
			return false;
		}
	}

	private bool CanRenumber => _car.Archetype != CarArchetype.Tender;

	public static void Show(Car car)
	{
		CarCustomizeWindow instance = Instance;
		instance._car = car;
		instance.Populate();
		instance._window.ShowWindow();
	}

	private void Awake()
	{
		_window = GetComponent<Window>();
	}

	private void OnEnable()
	{
		Messenger.Default.Register<CarIdentChanged>(this, delegate
		{
			Populate();
		});
	}

	private void OnDisable()
	{
		_panel?.Dispose();
		_panel = null;
		Messenger.Default.Unregister(this);
	}

	private void Populate()
	{
		_panel?.Dispose();
		if (_car == null)
		{
			return;
		}
		_window.Title = "Customize " + _car.DisplayName;
		_panel = UIPanel.Create(_window.contentRectTransform, BuilderAssets, delegate(UIPanelBuilder builder)
		{
			builder.VScrollView(delegate(UIPanelBuilder builder2)
			{
				BuildBasicsTab(builder2);
				bool num = BuildColorTab(builder2);
				bool flag = BuildLetteringTab(builder2);
				if (num || flag)
				{
					BuildCopyStyle(builder2);
				}
				if (_car.IsLocomotive)
				{
					BuildSoundTab(builder2);
				}
				builder2.AddExpandingVerticalSpacer();
			});
		});
	}

	private void BuildBasicsTab(UIPanelBuilder builder)
	{
		builder.AddSection("Identity", delegate(UIPanelBuilder uIPanelBuilder)
		{
			uIPanelBuilder.AddField("Reporting Mark", uIPanelBuilder.HStack(delegate(UIPanelBuilder field)
			{
				if (CanChangeReportingMark)
				{
					field.AddInputFieldReportingMark(_car.Ident.ReportingMark, ChangeReportingMark).FlexibleWidth();
				}
				else
				{
					field.AddLabel(_car.Ident.ReportingMark);
				}
			}));
			uIPanelBuilder.AddField("Road Number", uIPanelBuilder.HStack(delegate(UIPanelBuilder field)
			{
				if (CanRenumber)
				{
					field.AddInputField(_car.Ident.RoadNumber, ChangeRoadNumber, null, 6).FlexibleWidth();
				}
				else
				{
					field.AddLabel(_car.Ident.RoadNumber);
				}
			}));
		});
	}

	private void ChangeRoadNumber(string newRoadNumber)
	{
		RebuildUponRequestReject();
		StateManager.ApplyLocal(new RequestCarSetIdent(_car.id, _car.Ident.ReportingMark, newRoadNumber));
	}

	private void ChangeReportingMark(string newReportingMark)
	{
		RebuildUponRequestReject();
		StateManager.ApplyLocal(new RequestCarSetIdent(_car.id, newReportingMark, _car.Ident.RoadNumber));
	}

	private void RebuildUponRequestReject()
	{
		Messenger.Default.Register<RequestRejected>(this, delegate
		{
			_panel.Rebuild();
			Unregister();
		});
		LeanTween.delayedCall(1f, Unregister);
		void Unregister()
		{
			Messenger.Default.Unregister<RequestRejected>(this);
		}
	}

	private void BuildCopyStyle(UIPanelBuilder builder)
	{
		builder.AddSection("Tools", delegate(UIPanelBuilder uIPanelBuilder)
		{
			uIPanelBuilder.AddField("", uIPanelBuilder.AddButton("<sprite name=Copy><sprite name=Coupled>", CopyToCoupled).RectTransform);
		});
	}

	private CarColorScheme CurrentScheme()
	{
		return CarColorScheme.From(_car.KeyValueObject["_colorScheme"]);
	}

	private void CopyToCoupled()
	{
		CarColorScheme colorScheme = CurrentScheme();
		string lettering = GetLettering(_car);
		foreach (Car item in _car.EnumerateCoupled())
		{
			if (item.IsOwnedByPlayer)
			{
				if (HasColorComponents(item))
				{
					SetColorScheme(item, colorScheme);
				}
				if (GetDecalComponents(item).Any())
				{
					SetLettering(item, lettering);
				}
			}
		}
	}

	private bool BuildColorTab(UIPanelBuilder builder)
	{
		if (!HasColorComponents(_car))
		{
			return false;
		}
		builder.AddSection("Color", delegate(UIPanelBuilder uIPanelBuilder)
		{
			CarColorScheme carColorScheme = CurrentScheme();
			uIPanelBuilder.AddField("Base Color", uIPanelBuilder.AddColorDropdown(carColorScheme.BaseHex, delegate(string newValue)
			{
				SetColorScheme(colorScheme: new CarColorScheme(newValue, CurrentScheme().DecalHex), car: _car);
			}));
			uIPanelBuilder.AddField("Lettering Color", uIPanelBuilder.AddColorDropdown(carColorScheme.DecalHex, delegate(string newValue)
			{
				SetColorScheme(colorScheme: new CarColorScheme(CurrentScheme().BaseHex, newValue), car: _car);
			}));
		});
		return true;
	}

	private bool BuildLetteringTab(UIPanelBuilder builder)
	{
		List<DecalComponent> decalComponents = GetDecalComponents(_car);
		if (decalComponents.Count <= 0)
		{
			return false;
		}
		builder.AddSection("Lettering", delegate(UIPanelBuilder builder2)
		{
			BuildLetteringTabDecal(builder2, decalComponents);
		});
		return true;
	}

	private void BuildLetteringTabDecal(UIPanelBuilder builder, List<DecalComponent> decalComponents)
	{
		string lettering = GetLettering(_car);
		builder.AddField("Text", builder.AddInputField(lettering, delegate(string newValue)
		{
			SetLettering(_car, newValue);
		}));
	}

	private void BuildSoundTab(UIPanelBuilder builder)
	{
		WhistleComponent whistleComponent = _car.Definition.Components.OfType<WhistleComponent>().FirstOrDefault();
		if (whistleComponent != null)
		{
			builder.AddSection("Whistle", delegate(UIPanelBuilder builder2)
			{
				BuildSoundTabWhistle(builder2, whistleComponent);
			});
		}
	}

	private void BuildSoundTabWhistle(UIPanelBuilder builder, WhistleComponent whistleComponent)
	{
		Value value = _car.KeyValueObject["whistle.custom"];
		WhistleCustomizationSettings settings = WhistleCustomizationSettings.FromPropertyValue(value) ?? new WhistleCustomizationSettings(whistleComponent.DefaultWhistleIdentifier);
		List<TypedContainerItem<WhistleDefinition>> source = TrainController.Shared.PrefabStore.AllDefinitionInfosOfType<WhistleDefinition>().ToList();
		List<string> values = source.Select((TypedContainerItem<WhistleDefinition> di) => di.Metadata.Name).ToList();
		List<string> whistleIds = source.Select((TypedContainerItem<WhistleDefinition> di) => di.Identifier).ToList();
		int currentSelectedIndex = whistleIds.FindIndex((string id) => settings.WhistleIdentifier == id);
		builder.AddField("Whistle", builder.AddDropdown(values, currentSelectedIndex, delegate(int index)
		{
			WhistleCustomizationSettings whistleCustomizationSettings = new WhistleCustomizationSettings(whistleIds[index]);
			_car.KeyValueObject["whistle.custom"] = whistleCustomizationSettings.PropertyValue;
		}));
	}

	public static bool CanCustomize(Car car, out string reason)
	{
		reason = null;
		if (!StateManager.HasTrainmasterAccess)
		{
			reason = "Must be Trainmaster or higher";
			return false;
		}
		if (IsSandbox)
		{
			return true;
		}
		if (!car.IsOwnedByPlayer)
		{
			reason = "Must be owned by your railroad";
			return false;
		}
		return true;
	}

	private static void SetColorScheme(Car car, CarColorScheme colorScheme)
	{
		car.KeyValueObject["_colorScheme"] = colorScheme.ToValue();
	}

	private static List<DecalComponent> GetDecalComponents(Car car)
	{
		return (from dc in car.Definition.Components.OfType<DecalComponent>()
			where dc.Content == DecalContent.Lettering
			select dc).ToList();
	}

	private static void SetLettering(Car car, string newValue)
	{
		car.KeyValueObject["lettering.basic"] = (string.IsNullOrEmpty(newValue) ? Value.Null() : Value.String(newValue.Truncate(100)));
	}

	private static string GetLettering(Car car)
	{
		return car.KeyValueObject["lettering.basic"].StringValue;
	}

	private static bool HasColorComponents(Car car)
	{
		return car.Definition.Components.OfType<ColorizerComponent>().Any();
	}
}
