using System;
using Helpers;
using UnityEngine;

namespace Effects;

[SelectionBase]
public class MarkerLamp : MonoBehaviour
{
	public enum LensColor
	{
		Red,
		Green,
		Yellow
	}

	public int position;

	private bool _lit;

	[SerializeField]
	public LensColor[] lensColors = new LensColor[4];

	public Transform rotatingLamp;

	public MeshRenderer lampRenderer;

	public LensPalette palette;

	private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

	private float _lastRotation;

	private MaterialInstances _materials;

	public bool lit
	{
		get
		{
			return _lit;
		}
		set
		{
			_lit = value;
			ApplyLensColors();
		}
	}

	private void Awake()
	{
		_materials = new MaterialInstances(lampRenderer);
	}

	private void OnDestroy()
	{
		_materials?.Dispose();
		_materials = null;
	}

	private void Start()
	{
		for (int i = 0; i < 4; i++)
		{
			_materials[i + 1].CopyPropertiesFromMaterial(palette.emissiveMaterial);
		}
		ApplyLensColors();
	}

	private void OnValidate()
	{
	}

	private void Update()
	{
		float num = Mathf.LerpAngle(_lastRotation, position * 90, Time.deltaTime * 10f);
		rotatingLamp.localEulerAngles = new Vector3(0f, 0f, num);
		_lastRotation = num;
	}

	public void SetLensColors(LensColor[] aspects)
	{
		lensColors = aspects;
		ApplyLensColors();
	}

	private void ApplyLensColors()
	{
		for (int i = 0; i < 4; i++)
		{
			Material material = _materials[i + 1];
			UpdateMaterial(lensColors[i], material);
		}
	}

	private void UpdateMaterial(LensColor lensColor, Material material)
	{
		if (!(palette == null))
		{
			LensPalette.LensColorItem lensColorItem = ItemForLensColor(lensColor);
			material.color = lensColorItem.unlit;
			Color value = (_lit ? (lensColorItem.lit * lensColorItem.emissiveIntensity) : Color.black);
			material.SetColor(EmissionColor, value);
		}
	}

	private LensPalette.LensColorItem ItemForLensColor(LensColor lensColor)
	{
		return lensColor switch
		{
			LensColor.Red => palette.red, 
			LensColor.Green => palette.green, 
			LensColor.Yellow => palette.yellow, 
			_ => throw new ArgumentOutOfRangeException("lensColor", lensColor, null), 
		};
	}

	public (bool, int) NextState()
	{
		if (_lit)
		{
			if (position == 3)
			{
				return (false, position);
			}
			return (true, position + 1);
		}
		return (true, 0);
	}
}
