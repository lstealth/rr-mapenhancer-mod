using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AssetPack.Common;
using AssetPack.Runtime;
using Model.Database;
using Model.Definition;
using Model.Definition.Components;
using Model.Definition.Components.MapMasks;
using Model.Definition.Data;
using TMPro;
using UI.CarEditor.ComponentEditors;
using UI.CarEditor.DefinitionEditors;
using UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace UI.CarEditor;

[RequireComponent(typeof(Window))]
public class CarEditorWindow : MonoBehaviour
{
	[SerializeField]
	private ObjectEditorPrimaryPanel primary;

	[SerializeField]
	private ObjectEditorSecondaryPanel secondary;

	[SerializeField]
	private Button applyButton;

	[SerializeField]
	private Button removeComponentButton;

	[SerializeField]
	private Button duplicateComponentButton;

	[SerializeField]
	private TMP_Dropdown addComponentDropdown;

	private Window _window;

	private AssetPackRuntimeStore _store;

	private ContainerItem _item;

	private string _carId;

	private Coroutine _refreshCoroutine;

	private GameObject _componentGameObject;

	private ComponentEditor _componentEditor;

	private DefinitionEditor _definitionEditor;

	private readonly List<Type> _addComponentOptions = new List<Type>();

	private Model.Definition.Component _selectedComponent;

	private object _selectedObject;

	private Func<TransformReference, (Vector3, Quaternion)> _getParentPositionRotation;

	private Action _onChanged;

	private static CarEditorWindow Instance => WindowManager.Shared.GetWindow<CarEditorWindow>();

	public static bool IsShown => Instance._window.IsShown;

	public static void Show(AssetPackRuntimeStore store, string identifier, Func<TransformReference, (Vector3, Quaternion)> getParentPositionRotation, Action onChanged)
	{
		Instance._Show(store, identifier, getParentPositionRotation, onChanged);
	}

	private void _Show(AssetPackRuntimeStore store, string identifier, Func<TransformReference, (Vector3, Quaternion)> getParentPositionRotation, Action onChanged)
	{
		_window.Title = "Definition Editor";
		_window.ShowWindow();
		_getParentPositionRotation = getParentPositionRotation;
		_onChanged = onChanged;
		Configure(store, identifier);
	}

	public static void Hide()
	{
		Instance._window.CloseWindow();
	}

	private void Awake()
	{
		_window = GetComponent<Window>();
		_window.OnShownDidChange += delegate(bool shown)
		{
			if (!shown)
			{
				CleanupComponentEditor();
			}
		};
		applyButton.onClick.AddListener(ApplyChanges);
		removeComponentButton.onClick.AddListener(RemoveSelectedComponent);
		duplicateComponentButton.onClick.AddListener(DuplicateSelectedComponent);
		addComponentDropdown.onValueChanged.AddListener(AddComponentDropdownChanged);
		_componentGameObject = new GameObject("Component GameObject");
	}

	private void OnDisable()
	{
		if (_componentGameObject != null)
		{
			UnityEngine.Object.Destroy(_componentGameObject);
		}
		_componentGameObject = null;
		CleanupComponentEditor();
	}

	private void Configure(AssetPackRuntimeStore store, string identifier)
	{
		_store = store;
		_item = _store.ContainerItemForObjectIdentifier(identifier);
		_window.Title = "Definition Editor - " + identifier;
		ObjectDefinition definition = _item.Definition;
		if (definition.Components == null)
		{
			definition.Components = new List<Model.Definition.Component>();
		}
		ConfigureModelAnimationNamesAsync();
		ConfigurePanes(null);
		ConfigureAddComponentDropdown();
		UpdateForSelectedComponent();
	}

	private async void ConfigureModelAnimationNamesAsync()
	{
		ModelMapNames output = default(ModelMapNames);
		try
		{
			secondary.SetModelMapNames(output);
			secondary.SetModelHierarchyFromTransform(null);
			output = await GetModelMapNames();
		}
		catch (Exception exception)
		{
			Debug.LogError("Error getting animation names:");
			Debug.LogException(exception);
		}
		secondary.SetModelMapNames(output);
		async Task<ModelMapNames> GetModelMapNames()
		{
			string modelIdentifier = GetModelIdentifier();
			if (string.IsNullOrEmpty(modelIdentifier))
			{
				return default(ModelMapNames);
			}
			LoadedAssetReference<GameObject> loadedAssetReference = await _store.LoadAsset<GameObject>(modelIdentifier, CancellationToken.None);
			using (loadedAssetReference)
			{
				if (loadedAssetReference.Asset == null)
				{
					return default(ModelMapNames);
				}
				secondary.SetModelHierarchyFromTransform(loadedAssetReference.Asset.transform);
				ModelMapNames result = default(ModelMapNames);
				AnimationMap componentInChildren = loadedAssetReference.Asset.GetComponentInChildren<AnimationMap>();
				if (componentInChildren != null)
				{
					result.Animation = componentInChildren.animationClips.Select((AnimationMap.MapEntry ac) => ac.name).ToList();
				}
				MaterialMap componentInChildren2 = loadedAssetReference.Asset.GetComponentInChildren<MaterialMap>();
				if (componentInChildren2 != null)
				{
					result.Material = componentInChildren2.entries.Select((MaterialMap.MapEntry ac) => ac.name).ToList();
				}
				return result;
			}
		}
	}

	private void ConfigurePanes(object selectedObject)
	{
		_selectedObject = selectedObject;
		secondary.Configure(selectedObject, RequestRefresh);
		primary.Configure(_item, selectedObject, DidSelectObject);
	}

	private void DidSelectObject(object obj)
	{
		secondary.Configure(obj, RequestRefresh);
		CleanupComponentEditor();
		if (obj is ObjectDefinition objectDefinition)
		{
			_selectedComponent = null;
			if (objectDefinition is SteamLocomotiveDefinition definition)
			{
				SteamLocomotiveDefinitionEditor steamLocomotiveDefinitionEditor = _componentGameObject.AddComponent<SteamLocomotiveDefinitionEditor>();
				steamLocomotiveDefinitionEditor.Configure(definition, _getParentPositionRotation);
				_definitionEditor = steamLocomotiveDefinitionEditor;
			}
		}
		else if (obj is Model.Definition.Component component)
		{
			_selectedComponent = component;
			ComponentEditor componentEditor = ((component is DecalComponent component2) ? CreateComponentEditor<DecalComponentEditor>(component2) : ((component is LadderComponent component3) ? CreateComponentEditor<LadderComponentEditor>(component3) : ((component is LoadTargetComponent component4) ? CreateComponentEditor<LoadTargetComponentEditor>(component4) : ((component is LegacyMapMaskComponent component5) ? CreateComponentEditor<LegacyMapMaskComponentEditor>(component5) : ((component is CircleMapMaskComponent component6) ? CreateComponentEditor<CircleMapMaskComponentEditor>(component6) : ((component is RectangleMapMaskComponent component7) ? CreateComponentEditor<RectangleMapMaskComponentEditor>(component7) : ((component is RadialControlComponent component8) ? CreateComponentEditor<RadialControlComponentEditor>(component8) : ((!(component is SeatComponent component9)) ? CreateComponentEditor<ComponentEditor>(component) : CreateComponentEditor<SeatComponentEditor>(component9)))))))));
			_componentEditor = componentEditor;
			ComponentEditor componentEditor2 = _componentEditor;
			componentEditor2.OnValueChanged = (Action)Delegate.Combine(componentEditor2.OnValueChanged, (Action)delegate
			{
				secondary.Rebuild();
				RequestRefresh();
			});
		}
		else
		{
			_selectedComponent = null;
		}
		UpdateForSelectedComponent();
	}

	private T CreateComponentEditor<T>(Model.Definition.Component component) where T : ComponentEditor
	{
		T val = _componentGameObject.AddComponent<T>();
		val.Configure(component, _getParentPositionRotation);
		return val;
	}

	private void CleanupComponentEditor()
	{
		if (_componentEditor != null)
		{
			UnityEngine.Object.Destroy(_componentEditor);
			_componentEditor = null;
		}
		if (_definitionEditor != null)
		{
			UnityEngine.Object.Destroy(_definitionEditor);
			_definitionEditor = null;
		}
	}

	private void UpdateForSelectedComponent()
	{
		bool interactable = _selectedComponent != null;
		removeComponentButton.interactable = interactable;
		duplicateComponentButton.interactable = interactable;
	}

	private string GetModelIdentifier()
	{
		ObjectDefinition definition = _item.Definition;
		if (!(definition is CarDefinition { ModelIdentifier: var modelIdentifier }))
		{
			if (!(definition is TruckDefinition { ModelIdentifier: var modelIdentifier2 }))
			{
				return null;
			}
			return modelIdentifier2;
		}
		return modelIdentifier;
	}

	private void ConfigureAddComponentDropdown()
	{
		addComponentDropdown.ClearOptions();
		_addComponentOptions.Clear();
		List<string> names = new List<string>();
		Type[] types = typeof(ComponentAttribute).Assembly.GetTypes();
		foreach (Type type in types)
		{
			object[] customAttributes = type.GetCustomAttributes(typeof(ComponentAttribute), inherit: true);
			if (customAttributes.Length != 0 && (customAttributes[0] as ComponentAttribute).IsCompatibleWith(_item.Definition) && type.GetCustomAttributes(typeof(HideInEditorAttribute), inherit: true).Length == 0)
			{
				string str = DisplayNameForComponentType(type);
				AddOption(type, str);
			}
		}
		names.Insert(0, "Add Component");
		addComponentDropdown.AddOptions(names);
		void AddOption(Type item, string item2)
		{
			_addComponentOptions.Add(item);
			names.Add(item2);
		}
	}

	private static string DisplayNameForComponentType(Type type)
	{
		string text = type.ToString();
		int num = text.LastIndexOf('.');
		if (num >= 0)
		{
			string text2 = text;
			int num2 = num + 1;
			text = text2.Substring(num2, text2.Length - num2);
		}
		if (text.EndsWith("Component"))
		{
			string text2 = text;
			int num2 = "Component".Length;
			text = text2.Substring(0, text2.Length - num2);
		}
		return text.ToSentence();
	}

	private void AddComponentDropdownChanged(int index)
	{
		if (index != 0)
		{
			Model.Definition.Component component = (Model.Definition.Component)Activator.CreateInstance(_addComponentOptions[index - 1]);
			component.Name = NameForNewComponent(component);
			component.Transform = PositionRotationScale.Zero;
			_item.Definition.Components.Add(component);
			addComponentDropdown.value = 0;
			ConfigurePanes(component);
			DidSelectObject(component);
			RequestRefresh();
		}
	}

	private void ApplyChanges()
	{
		_store.SaveContainer();
		_onChanged();
		DefinitionChecker definitionChecker = new DefinitionChecker(_item.Identifier, _store.Identifier, _store);
		definitionChecker.Check(_item.Definition);
		definitionChecker.PrintToConsole();
	}

	private void RemoveSelectedComponent()
	{
		_item.Definition.Components.Remove(_selectedComponent);
		_selectedComponent = null;
		ConfigurePanes(null);
		RequestRefresh();
	}

	private void DuplicateSelectedComponent()
	{
		Model.Definition.Component component = ContainerSerialization.CloneViaSerialization(_selectedComponent);
		component.Name = NameForNewComponent(component);
		_item.Definition.Components.Add(component);
		ConfigurePanes(component);
		DidSelectObject(component);
		RequestRefresh();
	}

	private string NameForNewComponent(Model.Definition.Component component)
	{
		string text = (string.IsNullOrEmpty(component.Name) ? component.Kind : component.Name);
		Match match = Regex.Match(text, "\\s*\\d+$", RegexOptions.RightToLeft);
		if (match.Success && int.TryParse(match.Value.TrimStart(), out var result))
		{
			int length = match.Value.Length;
			string arg = text.Remove(text.Length - length, length);
			string proposed;
			while (true)
			{
				proposed = $"{arg} {result + 1}";
				if (_item.Definition.Components.All((Model.Definition.Component comp) => comp.Name != proposed))
				{
					break;
				}
				result++;
			}
			return proposed;
		}
		return text + " 1";
	}

	private void RequestRefresh()
	{
		if (_refreshCoroutine == null)
		{
			_refreshCoroutine = StartCoroutine(RefreshCoroutine());
		}
	}

	private IEnumerator RefreshCoroutine()
	{
		yield return new WaitForSecondsRealtime(0.2f);
		primary.Configure(_item, _selectedObject, DidSelectObject);
		_onChanged();
		_refreshCoroutine = null;
	}
}
