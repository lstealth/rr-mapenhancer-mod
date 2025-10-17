using Helpers;
using UnityEngine;

namespace Effects;

public class FuseeRenderer : MonoBehaviour
{
	public EmissiveLightProfile emissiveLightProfile;

	public Renderer filamentRenderer;

	public Light light;

	public float lightIntensityMultiplier = 2f;

	private Camera _camera;

	private float _parameter;

	private float _zPosition0;

	private float _xScale0;

	private Material _filamentMaterial;

	private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

	private void Awake()
	{
		_filamentMaterial = filamentRenderer.CreateUniqueMaterial();
		_zPosition0 = base.transform.localPosition.z;
		_xScale0 = base.transform.localScale.x;
	}

	private void OnDestroy()
	{
		Object.Destroy(_filamentMaterial);
		_filamentMaterial = null;
	}

	private void Start()
	{
		_camera = Camera.main;
		_parameter = 1f;
	}

	private void Update()
	{
		_parameter = Random.Range(0.7f, 1f);
		filamentRenderer.transform.LookAt(Camera.main.transform, Vector3.up);
		UpdateEmission();
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
		Vector3 vector = _camera.transform.position - base.transform.position;
		Vector3 normalized = vector.normalized;
		Vector3 vector2 = base.transform.InverseTransformDirection(normalized);
		Mathf.Atan2(vector2.x, vector2.z);
		Mathf.Atan2(vector2.y, vector2.z);
		float num = 1f;
		num *= _parameter;
		float magnitude = vector.magnitude;
		num *= emissiveLightProfile.intensityCurve.Evaluate(magnitude);
		float num2 = emissiveLightProfile.scaleCurve.Evaluate(magnitude);
		Color color = emissiveLightProfile.emissionColor * _parameter;
		_filamentMaterial.SetColor(EmissionColor, color * (1f + emissiveLightProfile.intensity * num));
		_filamentMaterial.color = emissiveLightProfile.emissionColor;
		Vector3 localPosition = base.transform.localPosition;
		base.transform.localPosition = localPosition;
		base.transform.localScale = Vector3.one * (_xScale0 + num2);
		light.intensity = lightIntensityMultiplier * num;
	}
}
