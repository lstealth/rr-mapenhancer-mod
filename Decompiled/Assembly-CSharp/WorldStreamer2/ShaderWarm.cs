using UnityEngine;

namespace WorldStreamer2;

public class ShaderWarm : MonoBehaviour
{
	private void Start()
	{
		Shader.WarmupAllShaders();
	}
}
