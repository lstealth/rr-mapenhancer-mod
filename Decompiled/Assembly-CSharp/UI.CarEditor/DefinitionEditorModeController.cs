using System;
using System.Collections.Generic;
using System.Linq;
using AssetPack.Common;
using AssetPack.Runtime;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Game.Messages;
using Game.State;
using Helpers;
using Model;
using Model.Database;
using Model.Definition;
using Model.Definition.Data;
using RLD;
using Serilog;
using Track;
using UnityEngine;

namespace UI.CarEditor;

public class DefinitionEditorModeController : MonoBehaviour
{
	[SerializeField]
	private RLDApp rldApp;

	[SerializeField]
	private RTFocusCamera rtFocusCamera;

	private AssetPackRuntimeStore _store;

	private ContainerItem _selectedItem;

	private string _filterText = "";

	private string _newIdentifier;

	private string _lastCarDiagnostics = "";

	private static Texture2D _guiBackgroundTexture;

	public static bool IsEditing { get; private set; }

	private TrainController TrainController => TrainController.Shared;

	private void OnEnable()
	{
		Messenger.Default.Register<MapDidLoadEvent>(this, MapDidLoad);
		IsEditing = true;
	}

	private void OnDisable()
	{
		Messenger.Default.Unregister(this);
		IsEditing = false;
	}

	private void OnGUI()
	{
		if (TrainController == null)
		{
			return;
		}
		if ((object)_guiBackgroundTexture == null)
		{
			_guiBackgroundTexture = MakeTex(2, 2, Color.black);
		}
		using (new GUILayout.VerticalScope(new GUIStyle(GUI.skin.box)
		{
			normal = 
			{
				background = _guiBackgroundTexture
			}
		}))
		{
			if (_store == null)
			{
				DrawGUISelectStore();
			}
			else if (_selectedItem == null)
			{
				DrawGUIStore();
			}
			else if (GUILayout.Button("Close Object"))
			{
				RemoveAllCars();
				_selectedItem = null;
				CarEditorWindow.Hide();
			}
		}
	}

	private static Texture2D MakeTex(int width, int height, Color col)
	{
		Color[] array = new Color[width * height];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = col;
		}
		Texture2D texture2D = new Texture2D(width, height);
		texture2D.hideFlags = HideFlags.DontSave;
		texture2D.SetPixels(array);
		texture2D.Apply();
		return texture2D;
	}

	private void DrawGUISelectStore()
	{
		using (new GUILayout.HorizontalScope())
		{
			GUILayout.Label("Filter:");
			_filterText = GUILayout.TextField(_filterText, GUILayout.Width(150f));
		}
		List<AssetPackRuntimeStore> list = ((PrefabStore)TrainController.PrefabStore).ExternalStores.ToList();
		list.Sort((AssetPackRuntimeStore a, AssetPackRuntimeStore b) => string.Compare(a.Identifier, b.Identifier, StringComparison.Ordinal));
		foreach (AssetPackRuntimeStore item in list)
		{
			if ((string.IsNullOrWhiteSpace(_filterText) || item.Identifier.Contains(_filterText)) && GUILayout.Button(item.Identifier))
			{
				_store = item;
			}
		}
	}

	private void DrawGUIStore()
	{
		if (GUILayout.Button("Close Pack"))
		{
			_store = null;
			return;
		}
		foreach (ContainerItem @object in _store.Container().Objects)
		{
			if (GUILayout.Button(@object.Identifier))
			{
				try
				{
					EditItem(@object);
				}
				catch (Exception exception)
				{
					Log.Error(exception, "Exception while opening item for editing {id}", @object.Identifier);
				}
			}
		}
		GUILayout.BeginHorizontal();
		DrawGUINewItem();
		GUILayout.EndHorizontal();
	}

	private void DrawGUINewItem()
	{
		GUILayout.Label("New:");
		_newIdentifier = Utilities.ValidateIdentifier(GUILayout.TextField(_newIdentifier, GUILayout.Width(150f)));
		if (!string.IsNullOrEmpty(_newIdentifier))
		{
			if (GUILayout.Button("Steam Locomotive"))
			{
				NewContainerItem(_newIdentifier, new SteamLocomotiveDefinition());
			}
			if (GUILayout.Button("Diesel"))
			{
				NewContainerItem(_newIdentifier, new DieselLocomotiveDefinition());
			}
			if (GUILayout.Button("Car"))
			{
				NewContainerItem(_newIdentifier, new CarDefinition());
			}
			if (GUILayout.Button("Truck"))
			{
				NewContainerItem(_newIdentifier, new TruckDefinition());
			}
			if (GUILayout.Button("Whistle"))
			{
				NewContainerItem(_newIdentifier, new WhistleDefinition());
			}
			if (GUILayout.Button("Scenery"))
			{
				NewContainerItem(_newIdentifier, new SceneryDefinition());
			}
			if (GUILayout.Button("Material"))
			{
				NewContainerItem(_newIdentifier, new MaterialDefinition());
			}
		}
	}

	private void MapDidLoad(MapDidLoadEvent mapDidLoadEvent)
	{
		rtFocusCamera.SetTargetCamera(Camera.main);
		rldApp.gameObject.SetActive(value: true);
		StateManager.ApplyLocal(new SetTimeOfDay(36000f));
	}

	private void NewContainerItem(string identifier, ObjectDefinition definition)
	{
		ContainerItem item = new ContainerItem
		{
			Identifier = identifier,
			Definition = definition,
			Metadata = new ObjectMetadata
			{
				Name = identifier,
				Description = "A new item",
				Credits = "",
				Tags = new List<string>()
			}
		};
		_store.AddItem(item);
		EditItem(item);
		_newIdentifier = null;
	}

	private void EditItem(ContainerItem item)
	{
		RemoveAllCars();
		_selectedItem = item;
		ObjectDefinition definition = item.Definition;
		if (!(definition is CarDefinition carDefinition))
		{
			if (definition is SceneryDefinition)
			{
				SceneryAssetInstance asset = SceneryAssetInstance.FindInstancesOfIdentifier(item.Identifier).FirstOrDefault();
				if (asset == null)
				{
					GameObject gameObject = GameObject.Find("SceneryEditorSentinel");
					GameObject gameObject2 = new GameObject("Editor Instance");
					gameObject2.transform.SetParent(gameObject.transform);
					gameObject2.hideFlags = HideFlags.DontSave;
					gameObject2.transform.localPosition = Vector3.zero;
					gameObject2.SetActive(value: false);
					asset = gameObject2.AddComponent<SceneryAssetInstance>();
					asset.identifier = item.Identifier;
					gameObject2.SetActive(value: true);
				}
				LeanTween.delayedCall(1f, (Action)delegate
				{
					CameraSelector.shared.ZoomToTransform(asset.transform);
				});
				CarEditorWindow.Show(_store, item.Identifier, (TransformReference tr) => GetParentPositionRotation(asset.transform, tr), delegate
				{
					asset.ReloadComponents();
				});
			}
			else
			{
				CarEditorWindow.Show(_store, item.Identifier, (TransformReference tr) => (Vector3.zero, Quaternion.identity), delegate
				{
				});
			}
		}
		else
		{
			EditItemCar(item, carDefinition, out var carId);
			CarEditorWindow.Show(_store, item.Identifier, (TransformReference tr) => GetParentPositionRotation(carId, tr), delegate
			{
				Messenger.Default.Send(new CarDefinitionDidChangeEvent(item.Identifier));
				ConfigureCheckForDiagnosticsCallback(carId);
			});
		}
	}

	private void EditItemCar(ContainerItem item, CarDefinition carDefinition, out string carId)
	{
		if (string.IsNullOrEmpty(carDefinition.ModelIdentifier))
		{
			carId = null;
			return;
		}
		TrackMarker trackMarker = TrainController.graph.MarkerForId("starter-editor");
		if (trackMarker == null)
		{
			throw new Exception("Editor span not found: starter-editor");
		}
		if (!trackMarker.Location.HasValue)
		{
			throw new Exception("Marker has no location: starter-editor");
		}
		Location value = trackMarker.Location.Value;
		CarIdent ident = new CarIdent("RR", carDefinition.BaseRoadNumber);
		List<CarDescriptor> list = new List<CarDescriptor>
		{
			new CarDescriptor(carDefinition.TypedContainerItem<CarDefinition>(item), ident)
		};
		if (carDefinition.TryGetTenderIdentifier(out var tenderIdentifier))
		{
			TypedContainerItem<CarDefinition> definitionInfo = TrainController.PrefabStore.CarDefinitionInfoForIdentifier(tenderIdentifier);
			CarIdent ident2 = new CarIdent(ident.ReportingMark, ident.RoadNumber + "T");
			list.Add(new CarDescriptor(definitionInfo, ident2));
		}
		TrainController.PlaceTrain(value, list);
		carId = TrainController.Cars.FirstOrDefault((Car car) => car.DefinitionInfo.Identifier == item.Identifier)?.id;
		ConfigureCheckForDiagnosticsCallback(carId);
	}

	private void ConfigureCheckForDiagnosticsCallback(string carId)
	{
		if (TrainController.TryGetCarForId(carId, out var car))
		{
			car.OnDidLoadModels += delegate
			{
				CheckForDiagnostics(carId);
			};
		}
	}

	private void CheckForDiagnostics(string carId)
	{
		if (TrainController.TryGetCarForId(carId, out var car))
		{
			string text = car.SetupDiagnostics.ToString();
			if (!(text == _lastCarDiagnostics))
			{
				global::Console.Log(string.IsNullOrEmpty(text) ? "Diagnostics clear.".ColorGreen() : ("Diagnostics".ColorRed() + ": " + text));
				_lastCarDiagnostics = text;
			}
		}
	}

	private static (Vector3, Quaternion) GetParentPositionRotation(string carId, TransformReference transformReference)
	{
		if (!TrainController.Shared.TryGetCarForId(carId, out var car))
		{
			Debug.LogWarning("Couldn't find car " + carId);
			return (Vector3.zero, Quaternion.identity);
		}
		Transform bodyTransform = car.BodyTransform;
		if (bodyTransform == null)
		{
			Car.MotionSnapshot motionSnapshot = car.GetMotionSnapshot();
			Debug.LogWarning($"Can't get transform reference position rotation - BodyTransform is null. Snapshot: {motionSnapshot.Position}, {motionSnapshot.Rotation}");
			return (motionSnapshot.Position, motionSnapshot.Rotation);
		}
		return GetParentPositionRotation(bodyTransform, transformReference);
	}

	private static (Vector3, Quaternion) GetParentPositionRotation(Transform bodyTransform, TransformReference transformReference)
	{
		Transform obj = bodyTransform.ResolveTransform(transformReference, defaultReturnsReceiver: true);
		Vector3 item = WorldTransformer.WorldToGame(obj.position);
		Quaternion rotation = obj.rotation;
		return (item, rotation);
	}

	private void RemoveAllCars()
	{
		StateManager.ApplyLocal(new RemoveCars(TrainController.Cars.Select((Car car) => car.id).ToList()));
	}
}
