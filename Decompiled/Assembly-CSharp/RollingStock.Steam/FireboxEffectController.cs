using Helpers;
using Model;
using UnityEngine;

namespace RollingStock.Steam;

public class FireboxEffectController : MonoBehaviour
{
	[SerializeField]
	private MeshRenderer meshRenderer;

	[SerializeField]
	private Light light;

	private Color _color;

	private float _lightIntensity;

	private Vector4 _speed;

	private static readonly int SpeedPropertyId = Shader.PropertyToID("_Speed");

	private BaseLocomotive _locomotive;

	private Material _material;

	private void Awake()
	{
		_material = meshRenderer.CreateUniqueMaterial();
		_color = _material.color;
		_speed = _material.GetVector(SpeedPropertyId);
		_lightIntensity = light.intensity;
	}

	private void OnEnable()
	{
		_locomotive = GetComponentInParent<BaseLocomotive>();
		_locomotive.OnHasFuelDidChange += HasFuelDidChange;
	}

	private void OnDisable()
	{
		_locomotive.OnHasFuelDidChange -= HasFuelDidChange;
	}

	private void OnDestroy()
	{
		Object.Destroy(_material);
		_material = null;
	}

	private void HasFuelDidChange()
	{
		bool hasFuel = _locomotive.HasFuel;
		Material material = _material;
		material.color = (hasFuel ? _color : Color.black);
		material.SetVector(SpeedPropertyId, hasFuel ? _speed : (_speed * 0.1f));
		light.intensity = (hasFuel ? _lightIntensity : 0f);
	}
}
