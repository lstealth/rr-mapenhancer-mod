using System;
using System.Collections.Generic;
using System.Linq;
using AssetPack.Common;
using UnityEngine;
using UnityEngine.Serialization;

namespace Helpers;

[ExecuteInEditMode]
[RequireComponent(typeof(SceneryAssetInstance))]
public class SceneryAssetMaterialCustomizer : MonoBehaviour
{
	[Serializable]
	public struct MaterialCustomization
	{
		public string materialName;

		[FormerlySerializedAs("color")]
		public Color baseColor;
	}

	public List<MaterialCustomization> customizations = new List<MaterialCustomization>();

	private SceneryAssetInstance _instance;

	private Transform _modelTransform;

	private readonly List<(MeshRenderer, int)> _materialBlockedRenderers = new List<(MeshRenderer, int)>();

	private static readonly int PropIdBaseColor = Shader.PropertyToID("_BaseColor");

	private void Awake()
	{
		_instance = GetComponent<SceneryAssetInstance>();
	}

	private void OnEnable()
	{
		_instance.OnDidLoadModels += InstanceDidLoadModels;
	}

	private void OnDisable()
	{
		RemoveMaterialBlockProperties();
		_instance.OnDidLoadModels -= InstanceDidLoadModels;
	}

	private void InstanceDidLoadModels(Transform modelTransform)
	{
		_modelTransform = modelTransform;
		ApplyMaterialCustomizations();
	}

	private void ApplyMaterialCustomizations()
	{
		if (_modelTransform == null)
		{
			return;
		}
		MaterialMap componentInChildren = _modelTransform.GetComponentInChildren<MaterialMap>();
		if (componentInChildren != null)
		{
			ApplyUsingMaterialMap(componentInChildren.entries.ToList(), fromMaterialMap: true);
			return;
		}
		List<MaterialMap.MapEntry> entries = (from m in (from m in _modelTransform.GetComponentsInChildren<MeshRenderer>().SelectMany((MeshRenderer mr) => mr.sharedMaterials)
				where m.HasColor(PropIdBaseColor)
				select m).Distinct()
			select new MaterialMap.MapEntry
			{
				material = m,
				name = m.name
			}).ToList();
		ApplyUsingMaterialMap(entries, fromMaterialMap: false);
	}

	private void ApplyUsingMaterialMap(List<MaterialMap.MapEntry> entries, bool fromMaterialMap)
	{
		MeshRenderer[] componentsInChildren = _modelTransform.GetComponentsInChildren<MeshRenderer>();
		RemoveMaterialBlockProperties();
		foreach (MaterialCustomization customization in customizations)
		{
			MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
			materialPropertyBlock.SetColor(PropIdBaseColor, customization.baseColor);
			int num = 0;
			foreach (MaterialMap.MapEntry entry in entries)
			{
				if (entry.name != customization.materialName || entry.material == null)
				{
					continue;
				}
				num++;
				int num2 = 0;
				MeshRenderer[] array = componentsInChildren;
				foreach (MeshRenderer meshRenderer in array)
				{
					for (int j = 0; j < meshRenderer.sharedMaterials.Length; j++)
					{
						if (!(meshRenderer.sharedMaterials[j] != entry.material))
						{
							meshRenderer.SetPropertyBlock(materialPropertyBlock, j);
							num2++;
							_materialBlockedRenderers.Add((meshRenderer, j));
						}
					}
				}
				if (num2 == 0)
				{
					Debug.LogWarning($"SceneryAssetMaterialCustomizer: Couldn't find material {entry.material} in model {base.name}", this);
				}
			}
			if (num == 0)
			{
				Debug.LogWarning("SceneryAssetMaterialCustomizer: Couldn't find customization entry " + customization.materialName + " in model " + base.name, this);
			}
		}
	}

	private void RemoveMaterialBlockProperties()
	{
		foreach (var (meshRenderer, materialIndex) in _materialBlockedRenderers)
		{
			if (meshRenderer != null)
			{
				meshRenderer.SetPropertyBlock(null, materialIndex);
			}
		}
		_materialBlockedRenderers.Clear();
	}
}
