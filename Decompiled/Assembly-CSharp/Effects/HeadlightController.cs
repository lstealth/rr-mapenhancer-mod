using Game;
using Helpers;
using UnityEngine;

namespace Effects;

public class HeadlightController : MonoBehaviour
{
	public enum State
	{
		Off,
		Dim,
		On
	}

	public enum HeadlightDirection
	{
		Forward,
		Reverse
	}

	[SerializeField]
	public State state;

	public float speedOn = 5f;

	public float speedOff = 2f;

	[Range(0f, 1f)]
	public float dimValue = 0.6f;

	public Renderer filamentRenderer;

	public Transform reflector;

	public Light light;

	public EmissiveLightProfile emissiveLightProfile;

	private Camera _camera;

	private float _parameter;

	private float _lightParameter;

	private float _zPosition0;

	private float _xScale0;

	private Material _filamentMaterial;

	private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

	public HeadlightDirection Direction { get; set; }

	public bool LightEnabled { get; set; } = true;

	private static float SunLevel => TimeWeather.SunLevel;

	private void Awake()
	{
		_filamentMaterial = filamentRenderer.CreateUniqueMaterial();
		_zPosition0 = reflector.localPosition.z;
		_xScale0 = reflector.localScale.x;
	}

	private void OnDestroy()
	{
		Object.Destroy(_filamentMaterial);
		_filamentMaterial = null;
	}

	private void Start()
	{
		_camera = Camera.main;
		UpdateForParameter();
	}

	private void Update()
	{
		UpdateForParameter();
		UpdateEmission();
	}

	private void UpdateForParameter()
	{
		float num = ValueForState(state);
		float b = ValueForLight(state);
		float num2 = ((num > _parameter) ? speedOn : speedOff);
		float t = Time.deltaTime * num2;
		_parameter = Mathf.Lerp(_parameter, num, t);
		_lightParameter = Mathf.Lerp(_lightParameter, b, t);
		float num3 = Mathf.Lerp(emissiveLightProfile.lightIntensityNight, emissiveLightProfile.lightIntensityDay, SunLevel);
		light.intensity = _lightParameter * num3;
		light.enabled = LightEnabled && _lightParameter > 0.01f;
	}

	private float ValueForState(State theState)
	{
		return theState switch
		{
			State.Off => 0f, 
			State.Dim => dimValue, 
			State.On => 1f, 
			_ => 0f, 
		};
	}

	private float ValueForLight(State theState)
	{
		return theState switch
		{
			State.Off => 0f, 
			State.Dim => emissiveLightProfile.lightIntensityDimMultiplier, 
			State.On => 1f, 
			_ => 0f, 
		};
	}

	private void UpdateEmission()
	{
		EmissiveLightProfile emissiveLightProfile = this.emissiveLightProfile;
		if (emissiveLightProfile == null)
		{
			return;
		}
		if (_camera == null)
		{
			_camera = Camera.main;
			if (_camera == null)
			{
				return;
			}
		}
		bool num = state != State.Off;
		Vector3 vector = _camera.transform.position - base.transform.position;
		Vector3 normalized = vector.normalized;
		Vector3 vector2 = base.transform.InverseTransformDirection(normalized);
		float f = Mathf.Atan2(vector2.x, vector2.z) * 57.29578f;
		float num2 = Mathf.InverseLerp(value: Mathf.Max(b: Mathf.Abs(Mathf.Atan2(vector2.y, vector2.z) * 57.29578f), a: Mathf.Abs(f)), a: emissiveLightProfile.minimumBeamAngle, b: emissiveLightProfile.fullBeamAngle);
		float num3 = Mathf.SmoothStep(emissiveLightProfile.minimumMultiplier, 1f, num2);
		num3 *= _parameter;
		float magnitude = vector.magnitude;
		magnitude *= Mathf.InverseLerp(10f, 30f, _camera.fieldOfView);
		num3 *= emissiveLightProfile.intensityCurve.Evaluate(magnitude);
		if (state == State.Dim)
		{
			num3 = Mathf.Min(emissiveLightProfile.dimValueMaximum, num3);
		}
		float num4 = (num ? (emissiveLightProfile.scaleCurve.Evaluate(magnitude) * emissiveLightProfile.angleScaleCurve.Evaluate(num2)) : 0f);
		Color color = emissiveLightProfile.emissionColor * _parameter;
		float num5 = emissiveLightProfile.intensity * num3;
		_filamentMaterial.SetColor(EmissionColor, color * (1f + num5));
		Vector3 localPosition = reflector.localPosition;
		float num6 = (num ? (num2 * Mathf.InverseLerp(10f, 50f, magnitude) * 0.5f) : 0f);
		localPosition.z = _zPosition0 + num6;
		reflector.localPosition = localPosition;
		reflector.localScale = Vector3.one * (_xScale0 + num4);
	}
}
