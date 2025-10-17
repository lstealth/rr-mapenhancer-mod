using System;
using System.Threading;
using System.Threading.Tasks;
using Helpers;
using Model;
using RollingStock;
using Serilog;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Effects.Decals;

[RequireComponent(typeof(DecalProjector))]
public class DecalProjectorHelper : MonoBehaviour
{
	public Color color = new Color(1f, 0.8f, 0.55f);

	public string templateName;

	public string text;

	public CanvasDecalRenderer decalRenderer;

	private DecalProjector _decalProjector;

	private Material _material;

	private CanvasDecal _decal;

	private Car _car;

	private bool _forceColor;

	private CancellationTokenSource _renderCancellationTokenSource;

	private bool _rendered;

	private bool _carVisible;

	private bool _decalVisible;

	private bool _decalRegistered;

	public const int MaxLetteringLength = 100;

	private void Awake()
	{
		_decalProjector = GetComponent<DecalProjector>();
	}

	private void OnEnable()
	{
		_car = GetComponentInParent<Car>();
		if (_car != null)
		{
			_car.OnVisibleDidChange += OnCarVisibilityDidChange;
		}
		OnCarVisibilityDidChange(_car.IsVisible);
		UpdateDecalProjectorEnabled();
		RenderDecal();
		CarColorController componentInParent = GetComponentInParent<CarColorController>();
		if (componentInParent != null)
		{
			componentInParent.ColorSchemeChanged += ColorSchemeChanged;
			ColorSchemeChanged(componentInParent.Scheme);
		}
	}

	private void OnDisable()
	{
		_car.OnVisibleDidChange -= OnCarVisibilityDidChange;
		CarColorController componentInParent = GetComponentInParent<CarColorController>();
		if (componentInParent != null)
		{
			componentInParent.ColorSchemeChanged -= ColorSchemeChanged;
		}
		SetDecalRegistered(registered: false);
	}

	private void OnDestroy()
	{
		_decal?.Dispose();
		if (_material != null)
		{
			UnityEngine.Object.Destroy(_material);
			_material = null;
		}
	}

	private void OnCarVisibilityDidChange(bool visible)
	{
		if (_carVisible != visible)
		{
			_carVisible = visible;
			SetDecalRegistered(visible);
			UpdateDecalProjectorEnabled();
		}
	}

	private void SetDecalRegistered(bool registered)
	{
		if (_decalRegistered != registered)
		{
			DecalCullingManager shared = DecalCullingManager.Shared;
			if (registered)
			{
				shared.RegisterDecal(_decalProjector, OnDecalVisibilityDidChange);
			}
			else
			{
				shared.UnregisterDecal(_decalProjector);
			}
			_decalVisible = false;
			_decalRegistered = registered;
		}
	}

	private void OnDecalVisibilityDidChange(bool visible)
	{
		_decalVisible = visible;
		UpdateDecalProjectorEnabled();
	}

	private void UpdateDecalProjectorEnabled()
	{
		if (_decalProjector != null)
		{
			_decalProjector.enabled = _rendered && _decalVisible && _carVisible;
		}
	}

	private void ColorSchemeChanged(CarColorScheme scheme)
	{
		if (!_forceColor && scheme.Decal.HasValue)
		{
			color = scheme.Decal.Value;
		}
		ApplyColor();
	}

	[ContextMenu("Render Decal")]
	public void RenderDecal()
	{
		if (!(_decalProjector == null))
		{
			_decal?.Dispose();
			RenderDecalAsync();
		}
	}

	private async void RenderDecalAsync()
	{
		_rendered = false;
		_renderCancellationTokenSource?.Cancel();
		_renderCancellationTokenSource = new CancellationTokenSource();
		if ((object)_material == null)
		{
			_material = new Material(decalRenderer.referenceMaterial);
		}
		try
		{
			_decal = await decalRenderer.Render(_decalProjector.size, templateName, text.Truncate(100), _renderCancellationTokenSource.Token);
			if (!(_material == null))
			{
				_material.SetTexture("_Texture", _decal.Texture);
				ApplyColor();
				_rendered = true;
				_decalProjector.material = _material;
				UpdateDecalProjectorEnabled();
			}
		}
		catch (TaskCanceledException)
		{
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Exception from CanvasDecalRenderer");
		}
	}

	private void ApplyColor()
	{
		_material.SetColor("_Color", color);
	}

	public void ForceColor(Color theColor)
	{
		color = theColor;
		_forceColor = true;
	}
}
