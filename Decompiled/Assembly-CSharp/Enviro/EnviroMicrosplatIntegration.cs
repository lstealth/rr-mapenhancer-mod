using UnityEngine;

namespace Enviro;

[ExecuteInEditMode]
[AddComponentMenu("Enviro 3/Integrations/MicroSplat Integration")]
public class EnviroMicrosplatIntegration : MonoBehaviour
{
	[Header("Wetness")]
	public bool UpdateWetness = true;

	[Range(0f, 1f)]
	public float minWetness;

	[Range(0f, 1f)]
	public float maxWetness = 1f;

	[Header("Rain Ripples")]
	public bool UpdateRainRipples = true;

	[Header("Puddle Settings")]
	public bool UpdatePuddles = true;

	[Header("Stream Settings")]
	public bool UpdateStreams = true;

	[Header("Snow Settings")]
	public bool UpdateSnow = true;

	private void Update()
	{
		if (!(EnviroManager.instance == null) && !(EnviroManager.instance.Environment == null))
		{
			if (UpdateSnow)
			{
				Shader.SetGlobalFloat("_Global_SnowLevel", EnviroManager.instance.Environment.Settings.snow);
			}
			if (UpdateWetness)
			{
				float x = Mathf.Clamp(EnviroManager.instance.Environment.Settings.wetness, minWetness, maxWetness);
				Shader.SetGlobalVector("_Global_WetnessParams", new Vector2(x, maxWetness));
			}
			if (UpdatePuddles)
			{
				Shader.SetGlobalFloat("_Global_PuddleParams", EnviroManager.instance.Environment.Settings.wetness);
			}
			if (UpdateRainRipples && EnviroManager.instance.Environment != null)
			{
				float value = Mathf.Clamp(EnviroManager.instance.Environment.Settings.wetness, 0f, 1f);
				Shader.SetGlobalFloat("_Global_RainIntensity", value);
			}
			if (UpdateStreams)
			{
				Shader.SetGlobalFloat("_Global_StreamMax", EnviroManager.instance.Environment.Settings.wetness);
			}
		}
	}
}
