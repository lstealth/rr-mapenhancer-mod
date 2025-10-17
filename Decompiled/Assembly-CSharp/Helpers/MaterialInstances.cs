using System;
using UnityEngine;

namespace Helpers;

public class MaterialInstances : IDisposable
{
	private Material[] _instances;

	private bool _disposed;

	public Material this[int index]
	{
		get
		{
			if (_disposed)
			{
				throw new ObjectDisposedException("MaterialInstances");
			}
			return _instances[index];
		}
	}

	public int Count
	{
		get
		{
			Material[] instances = _instances;
			if (instances == null)
			{
				return 0;
			}
			return instances.Length;
		}
	}

	public MaterialInstances(Renderer renderer)
	{
		Material[] sharedMaterials = renderer.sharedMaterials;
		_instances = new Material[sharedMaterials.Length];
		for (int i = 0; i < sharedMaterials.Length; i++)
		{
			_instances[i] = new Material(sharedMaterials[i]);
		}
		renderer.materials = _instances;
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}
		if (_instances != null)
		{
			Material[] instances = _instances;
			foreach (Material material in instances)
			{
				if (material != null)
				{
					UnityEngine.Object.Destroy(material);
				}
			}
			_instances = null;
		}
		_disposed = true;
	}
}
