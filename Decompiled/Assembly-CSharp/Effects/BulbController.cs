using System;
using KeyValue.Runtime;
using UnityEngine;

namespace Effects;

public class BulbController : MonoBehaviour
{
	public string onOffKey = "bulb";

	public Renderer bulbRenderer;

	public Light light;

	public Color baseColor = Color.white;

	public float materialIntensity = 1f;

	public float lightIntensityMultiplier = 2f;

	private float _targetParameter;

	private float _parameter;

	private IDisposable _observer;

	private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

	private static readonly int ColorColor = Shader.PropertyToID("_Color");

	private MaterialPropertyBlock _materialPropertyBlock;

	private void Start()
	{
		_observer = GetComponentInParent<KeyValueObject>().Observe(onOffKey, delegate(Value value)
		{
			_targetParameter = (value.BoolValue ? 1 : 0);
		});
		_materialPropertyBlock = new MaterialPropertyBlock();
	}

	private void OnDisable()
	{
		_observer?.Dispose();
	}

	private void Update()
	{
		_parameter = Mathf.Lerp(_parameter, _targetParameter, Time.deltaTime * 5f);
		light.enabled = _parameter > 0.001f;
		UpdateEmission(_parameter);
	}

	private void UpdateEmission(float value)
	{
		Color color = baseColor * _parameter;
		_materialPropertyBlock.SetColor(EmissionColor, color * (1f + materialIntensity * value));
		_materialPropertyBlock.SetColor(ColorColor, baseColor);
		bulbRenderer.SetPropertyBlock(_materialPropertyBlock);
		light.intensity = lightIntensityMultiplier * value;
	}
}
