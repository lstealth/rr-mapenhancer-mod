using System;
using AssetPack.Common;
using Game.Messages;
using KeyValue.Runtime;
using Model.Definition.Data;
using RollingStock;
using Serilog;
using UnityEngine;

namespace Model;

public readonly struct ComponentBuilderContext : IDefinitionReferenceResolver, IPrefabInstantiator
{
	private readonly AnimationMap _animationMap;

	private readonly MaterialMap _materialMap;

	private readonly IPrefabInstantiator _prefabInstantiator;

	private readonly Action<string, Action<Value>> _observeProperty;

	public GameObject GameObject { get; }

	public GameObject AnimatorGameObject { get; }

	public CarColorController CarColorController { get; }

	public string ObjectName { get; }

	public ComponentBuilderContext(string objectName, GameObject gameObject, AnimationMap animationMap, MaterialMap materialMap, CarColorController carColorController, IPrefabInstantiator prefabInstantiator, Action<string, Action<Value>> observeProperty)
	{
		ObjectName = objectName;
		GameObject = gameObject;
		_animationMap = animationMap;
		_materialMap = materialMap;
		CarColorController = carColorController;
		_prefabInstantiator = prefabInstantiator;
		AnimatorGameObject = ((_animationMap == null) ? null : _animationMap.gameObject);
		_observeProperty = observeProperty;
	}

	public T InstantiatePrefab<T>(string name, Transform parent) where T : Component
	{
		return _prefabInstantiator.InstantiatePrefab<T>(name, parent);
	}

	public Transform Resolve(TransformReference transformReference)
	{
		return GameObject.transform.parent.ResolveTransform(transformReference);
	}

	public AnimationClip Resolve(AnimationReference animationReference)
	{
		return _animationMap.Resolve(animationReference);
	}

	public bool TryResolve(AnimationReference animationReference, out AnimationClip animationClip)
	{
		animationClip = Resolve(animationReference);
		return animationClip != null;
	}

	public Material Resolve(MaterialReference materialReference)
	{
		if (_materialMap == null)
		{
			Log.Error("{object}: Can't resolve materialReference because _materialMap is null.", ObjectName);
			return null;
		}
		return _materialMap.Resolve(materialReference);
	}

	public void ObserveProperty(string key, Action<Value> observer)
	{
		_observeProperty(key, observer);
	}

	public void ObserveProperty(PropertyChange.Control control, Action<Value> observer)
	{
		string arg = PropertyChange.KeyForControl(control);
		_observeProperty(arg, observer);
	}
}
