using System;
using UnityEngine;

namespace UI.Map;

[RequireComponent(typeof(MeshRenderer))]
public class MaterialReturnOnDestroy : MonoBehaviour
{
	public Action<Material> ReturnMaterial;

	private MeshRenderer _meshRenderer;

	private void Awake()
	{
		_meshRenderer = GetComponent<MeshRenderer>();
	}

	private void OnDestroy()
	{
		if (!(_meshRenderer == null))
		{
			Material sharedMaterial = _meshRenderer.sharedMaterial;
			if (!(sharedMaterial == null))
			{
				ReturnMaterial?.Invoke(sharedMaterial);
			}
		}
	}
}
