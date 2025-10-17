using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using Effects;
using Helpers;
using KeyValue.Runtime;
using Model;
using UnityEngine;

namespace Track.Signals;

[SelectionBase]
public class CTCPanelLamp : MonoBehaviour, IPickable
{
	public enum Mode
	{
		BlockDirection,
		Switch,
		BlockOccupancy,
		InterlockingDirection
	}

	public enum Color
	{
		Red,
		Green,
		Yellow,
		White
	}

	public MeshRenderer lampRenderer;

	public Transform modelTransform;

	public LensPalette palette;

	public Color color;

	public Mode mode;

	public string targetId;

	public int filterValue;

	private readonly HashSet<IDisposable> _observers = new HashSet<IDisposable>();

	private bool _lit;

	private Material _lampMaterial;

	private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

	private TooltipInfo _cachedTooltipInfo;

	private float _cachedTooltipExpires;

	public float MaxPickDistance => 3f;

	public int Priority => 1;

	public PickableActivationFilter ActivationFilter => PickableActivationFilter.PrimaryOnly;

	public TooltipInfo TooltipInfo
	{
		get
		{
			if (_cachedTooltipExpires < Time.unscaledTime)
			{
				_cachedTooltipInfo = BuildTooltip();
				_cachedTooltipExpires = Time.unscaledTime + 1f;
			}
			return _cachedTooltipInfo;
		}
	}

	private void Awake()
	{
		_lampMaterial = lampRenderer.CreateUniqueMaterial();
	}

	private void OnDestroy()
	{
		UnityEngine.Object.Destroy(_lampMaterial);
		_lampMaterial = null;
	}

	private void OnEnable()
	{
		modelTransform.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-90, 90)) * modelTransform.localRotation;
		KeyValueObject componentInParent = GetComponentInParent<KeyValueObject>();
		if (!string.IsNullOrEmpty(targetId))
		{
			string key = mode switch
			{
				Mode.BlockDirection => CTCKeys.BlockTrafficFilter(targetId), 
				Mode.Switch => CTCKeys.SwitchPosition(targetId), 
				Mode.BlockOccupancy => CTCKeys.BlockOccupancy(targetId), 
				Mode.InterlockingDirection => CTCKeys.InterlockingDirection(targetId), 
				_ => throw new ArgumentOutOfRangeException(), 
			};
			_observers.Add(componentInParent.Observe(key, OnPropertyChange));
		}
	}

	private void OnDisable()
	{
		foreach (IDisposable observer in _observers)
		{
			observer.Dispose();
		}
		_observers.Clear();
	}

	private void OnPropertyChange(Value value)
	{
		SetLit(value.IntValue == filterValue);
	}

	private void SetLit(bool lit)
	{
		_lit = lit;
		Material lampMaterial = _lampMaterial;
		LensPalette.LensColorItem lensColorItem = ItemForLensColor();
		lampMaterial.color = lensColorItem.unlit;
		lampMaterial.SetColor(value: lit ? (lensColorItem.lit * lensColorItem.emissiveIntensity) : UnityEngine.Color.black, nameID: EmissionColor);
	}

	private LensPalette.LensColorItem ItemForLensColor()
	{
		return color switch
		{
			Color.Red => palette.red, 
			Color.Green => palette.green, 
			Color.Yellow => palette.yellow, 
			Color.White => palette.white, 
			_ => throw new ArgumentOutOfRangeException(), 
		};
	}

	public void Activate(PickableActivateEvent evt)
	{
	}

	public void Deactivate()
	{
	}

	private TooltipInfo BuildTooltip()
	{
		switch (mode)
		{
		case Mode.BlockDirection:
			return TooltipInfo.Empty;
		case Mode.Switch:
			return new TooltipInfo("Switch Position", null);
		case Mode.BlockOccupancy:
		{
			CTCBlock block = CTCPanelController.Shared.BlockForId(targetId);
			return BuildTooltipForBlockOccupancy(block);
		}
		case Mode.InterlockingDirection:
			return new TooltipInfo("Signal Direction", null);
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	private TooltipInfo BuildTooltipForBlockOccupancy(CTCBlock block)
	{
		string text2;
		string title;
		if (_lit)
		{
			HashSet<Car> hashSet = block.CarsInBlock();
			List<Car> list = (from car in hashSet
				where car.IsLocomotive
				orderby car.DisplayName
				select car).ToList();
			string text = hashSet.Count.Pluralize("car");
			text2 = ((list.Count <= 0) ? text : (string.Join(", ", list.Select((Car l) => l.DisplayName)) + " (" + text + ")"));
			title = "Occupied Block";
		}
		else
		{
			title = null;
			text2 = null;
		}
		return new TooltipInfo(title, text2);
	}
}
