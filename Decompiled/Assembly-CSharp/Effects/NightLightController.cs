using System;
using UnityEngine;

namespace Effects;

public class NightLightController : MonoBehaviour
{
	public Light light;

	[Range(0f, 1f)]
	public float threshold = 0.995f;

	public Renderer bulbRenderer;

	public Color baseColor = Color.white;

	public float materialIntensity = 1f;

	private float _lightIntensityMultiplier = 1f;

	private float _parameter;

	private IDisposable _scheduleHandle;

	private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

	private static readonly int ColorColor = Shader.PropertyToID("_Color");

	private MaterialPropertyBlock _materialPropertyBlock;

	private void Awake()
	{
		if (light != null)
		{
			_lightIntensityMultiplier = light.intensity;
			light.enabled = false;
		}
		else
		{
			Debug.LogWarning("Couldn't find light, setting default intensity.");
			_lightIntensityMultiplier = 50f;
		}
	}

	private void OnEnable()
	{
		_scheduleHandle = ClockDriver.Instance.Schedule(18f, 6f, SetOn);
	}

	private void OnDisable()
	{
		_scheduleHandle?.Dispose();
		_scheduleHandle = null;
	}

	private void SetOn(bool on)
	{
		float to = (on ? 1f : 0f);
		LeanTween.cancel(base.gameObject);
		LeanTween.value(base.gameObject, _parameter, to, 1f).setEaseInOutCubic().setOnUpdate(delegate(float value)
		{
			_parameter = value;
			UpdateEmission(_parameter, baseColor, materialIntensity, bulbRenderer, _lightIntensityMultiplier, light);
		});
	}

	private void UpdateEmission(float parameter, Color baseColor, float materialIntensity, Renderer bulbRenderer, float lightIntensityMultiplier, Light light)
	{
		if (bulbRenderer != null)
		{
			Color color = baseColor * parameter;
			if (_materialPropertyBlock == null)
			{
				_materialPropertyBlock = new MaterialPropertyBlock();
			}
			_materialPropertyBlock.SetColor(EmissionColor, color * (1f + materialIntensity * parameter));
			_materialPropertyBlock.SetColor(ColorColor, baseColor);
			bulbRenderer.SetPropertyBlock(_materialPropertyBlock);
		}
		if (light != null)
		{
			light.enabled = parameter > 0.001f;
			light.intensity = lightIntensityMultiplier * parameter;
		}
	}
}
