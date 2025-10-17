using System;
using Helpers;
using UnityEngine;

namespace Effects;

public class ClassLight : MonoBehaviour
{
	public enum LensColor
	{
		Green,
		White
	}

	public bool lit;

	public LensColor color;

	private bool _wasLit;

	private LensColor _lastColor;

	public MeshRenderer lampRenderer;

	public LensPalette palette;

	private MaterialInstances _materials;

	private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

	private void Awake()
	{
		_materials = new MaterialInstances(lampRenderer);
	}

	private void OnDestroy()
	{
		_materials.Dispose();
		_materials = null;
	}

	private void Start()
	{
		for (int i = 0; i < _materials.Count - 1; i++)
		{
			_materials[i + 1].CopyPropertiesFromMaterial(palette.emissiveMaterial);
		}
		ApplyLensColors();
	}

	private void Update()
	{
		if (lit != _wasLit || color != _lastColor)
		{
			ApplyLensColors();
			_wasLit = lit;
			_lastColor = color;
		}
	}

	private void ApplyLensColors()
	{
		for (int i = 0; i < _materials.Count - 1; i++)
		{
			Material material = _materials[i + 1];
			UpdateMaterial(material);
		}
	}

	private void UpdateMaterial(Material material)
	{
		if (!(palette == null))
		{
			LensPalette.LensColorItem lensColorItem = ItemForLensColor(color);
			material.color = lensColorItem.unlit;
			Color value = (lit ? (lensColorItem.lit * lensColorItem.emissiveIntensity) : Color.black);
			material.SetColor(EmissionColor, value);
		}
	}

	private LensPalette.LensColorItem ItemForLensColor(LensColor lensColor)
	{
		return lensColor switch
		{
			LensColor.Green => palette.green, 
			LensColor.White => palette.white, 
			_ => palette.white, 
		};
	}

	public (bool, LensColor) NextState()
	{
		if (lit)
		{
			return color switch
			{
				LensColor.White => (true, LensColor.Green), 
				LensColor.Green => (false, LensColor.Green), 
				_ => throw new Exception("Unexpected color " + color), 
			};
		}
		return (true, LensColor.White);
	}
}
