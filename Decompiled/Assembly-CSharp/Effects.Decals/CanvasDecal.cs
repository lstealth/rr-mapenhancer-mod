using System;
using UnityEngine;

namespace Effects.Decals;

public class CanvasDecal : IDisposable
{
	private CanvasDecalRenderer _parent;

	public string Key { get; }

	public Texture Texture { get; private set; }

	internal CanvasDecal(string key, Texture texture, CanvasDecalRenderer parent)
	{
		Key = key;
		Texture = texture;
		_parent = parent;
	}

	public void Dispose()
	{
		if (!(_parent == null))
		{
			_parent.Return(this);
			_parent = null;
			Texture = null;
		}
	}
}
