using System;
using Helpers;
using KeyValue.Runtime;
using UnityEngine;

namespace Effects;

public class LanternController : MonoBehaviour
{
	public Renderer filamentRenderer;

	public Light light;

	public Color baseColor = Color.white;

	public float materialIntensity = 1f;

	public float lightIntensityMultiplier = 2f;

	public AnimationCurve parameterCurve = AnimationCurve.Constant(0f, 1f, 1f);

	public AnimationCurve scaleCurve = AnimationCurve.Constant(0f, 1f, 1f);

	public string onOffKey = "lantern.0";

	private Camera _camera;

	private float _parameter;

	private float _targetParameter;

	private float _xScale0;

	private Material _filamentMaterial;

	private IDisposable _observer;

	private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

	private void Awake()
	{
		_filamentMaterial = filamentRenderer.CreateUniqueMaterial();
	}

	private void Start()
	{
		KeyValueObject componentInParent = GetComponentInParent<KeyValueObject>();
		if (componentInParent == null || string.IsNullOrEmpty(onOffKey))
		{
			_targetParameter = 1f;
		}
		else
		{
			_observer = componentInParent.Observe(onOffKey, delegate(Value value)
			{
				_targetParameter = (value.BoolValue ? 1 : 0);
			});
		}
		_xScale0 = base.transform.localScale.x;
	}

	private void OnDestroy()
	{
		_observer?.Dispose();
		_observer = null;
		UnityEngine.Object.Destroy(_filamentMaterial);
		_filamentMaterial = null;
	}

	private void Update()
	{
		if (MainCameraHelper.TryGetIfNeeded(ref _camera))
		{
			_parameter = Mathf.Lerp(_parameter, _targetParameter, Time.deltaTime * 5f);
			light.enabled = _parameter > 0.001f;
			if (parameterCurve != null && parameterCurve.length >= 1)
			{
				float value = _parameter * parameterCurve.Evaluate(Mathf.Repeat(Time.time, parameterCurve[parameterCurve.length - 1].time));
				filamentRenderer.transform.LookAt(_camera.transform, Vector3.up);
				filamentRenderer.transform.localScale = Vector3.one * (_xScale0 + scaleCurve.Evaluate(Vector3.Distance(_camera.transform.position, base.transform.position)));
				UpdateEmission(value);
			}
		}
	}

	private void UpdateEmission(float value)
	{
		Color color = baseColor * _parameter;
		_filamentMaterial.SetColor(EmissionColor, color * (1f + materialIntensity * value));
		_filamentMaterial.color = baseColor;
		light.intensity = lightIntensityMultiplier * value;
	}
}
