using System;
using AssetPack.Common;
using KeyValue.Runtime;
using Model.Definition;
using RollingStock;
using UnityEngine;

namespace Model;

public static class ComponentSetup
{
	public struct Context
	{
		public AnimationMap AnimationMap;

		public MaterialMap MaterialMap;

		public CarColorController CarColorController;
	}

	public static void Setup(string objectName, Model.Definition.Component component, Context setupContext, Transform parent, Action<string, Action<Value>> observeProperty, IPrefabInstantiator prefabInstantiator)
	{
		GameObject gameObject = new GameObject(component.Name);
		gameObject.hideFlags = HideFlags.DontSave;
		gameObject.SetActive(value: false);
		Transform transform = gameObject.transform;
		transform.SetParent(parent, worldPositionStays: false);
		transform.localPosition = component.Transform.Position;
		transform.localRotation = component.Transform.Rotation;
		transform.localScale = component.Transform.Scale;
		ComponentBuilderContext ctx = new ComponentBuilderContext(objectName, gameObject, setupContext.AnimationMap, setupContext.MaterialMap, setupContext.CarColorController, prefabInstantiator, observeProperty);
		ComponentFactory.BuildComponent(component, ctx);
		gameObject.SetActive(value: true);
	}
}
