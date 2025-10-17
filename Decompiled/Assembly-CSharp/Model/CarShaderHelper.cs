using UnityEngine;

namespace Model;

public class CarShaderHelper : MonoBehaviour
{
	public Shader shader;

	public Texture2D noiseDirtTexture;

	public Texture2D noiseNormalTexture;

	private static readonly int WearNoise = Shader.PropertyToID("_WearNoise");

	private static readonly int WearNormalNoise = Shader.PropertyToID("_WearNormalNoise");

	private const string SharedStandardCarShaderName = "Railroader/Standard Car Shader (Shared)";

	public const string BuiltinStandardCarShaderName = "Railroader/Standard Car Shader (Builtin)";

	public static CarShaderHelper Instance { get; private set; }

	private void Awake()
	{
		Instance = this;
	}

	public void ReplaceShaders(Object obj)
	{
		if (!(obj is GameObject gameObject))
		{
			if (obj is Component mb)
			{
				ReplaceShaders(mb);
			}
		}
		else
		{
			ReplaceShaders(gameObject.transform);
		}
	}

	private void ReplaceShaders(Component mb)
	{
		ReplaceShaders(mb, "Railroader/Standard Car Shader (Shared)", shader);
	}

	private void ReplaceShaders(Component mb, string searchShaderName, Shader replacementShader)
	{
		MeshRenderer[] componentsInChildren = mb.GetComponentsInChildren<MeshRenderer>();
		foreach (MeshRenderer meshRenderer in componentsInChildren)
		{
			for (int j = 0; j < meshRenderer.sharedMaterials.Length; j++)
			{
				Material material = meshRenderer.sharedMaterials[j];
				if (material == null)
				{
					Debug.LogWarning("Found null sharedMaterial on " + mb.name + " " + meshRenderer.name);
					continue;
				}
				Shader shader = material.shader;
				if (!(shader == replacementShader) && !(shader.name != searchShaderName))
				{
					material.shader = replacementShader;
					material.SetTexture(WearNoise, noiseDirtTexture);
					material.SetTexture(WearNormalNoise, noiseNormalTexture);
				}
			}
		}
	}
}
