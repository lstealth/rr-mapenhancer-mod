using System;
using UnityEngine;

public class DebugGUIExamples : MonoBehaviour
{
	[DebugGUIGraph(0f, 1f, 0f, -1f, 1f, 0, true)]
	private float SinField;

	[DebugGUIPrint]
	[DebugGUIGraph(1f, 0.3f, 0.3f, 0f, 1f, 1, true)]
	private float mouseX;

	[DebugGUIPrint]
	[DebugGUIGraph(0f, 1f, 0f, 0f, 1f, 1, true)]
	private float mouseY;

	[DebugGUIGraph(0f, 1f, 1f, -1f, 1f, 0, true)]
	private float CosProperty => Mathf.Cos(Time.time * 6f);

	[DebugGUIGraph(1f, 0.3f, 1f, -1f, 1f, 0, true)]
	private float SinProperty => Mathf.Sin((Time.time + MathF.PI / 2f) * 6f);

	private void Awake()
	{
		DebugGUI.Log("Hello! I will disappear in five seconds!");
		DebugGUI.SetGraphProperties("smoothFrameRate", "SmoothFPS", 0f, 200f, 2, new Color(0f, 1f, 1f), autoScale: false);
		DebugGUI.SetGraphProperties("frameRate", "FPS", 0f, 200f, 2, new Color(1f, 0.5f, 1f), autoScale: false);
		DebugGUI.SetGraphProperties("fixedFrameRateSin", "FixedSin", -1f, 1f, 3, new Color(1f, 1f, 0f), autoScale: true);
	}

	private void Update()
	{
		SinField = Mathf.Sin(Time.time * 6f);
		mouseX = Input.mousePosition.x / (float)Screen.width;
		mouseY = Input.mousePosition.y / (float)Screen.height;
		if (Input.GetMouseButtonDown(0))
		{
			DebugGUI.Log(string.Format("Mouse clicked! ({0}, {1})", mouseX.ToString("F3"), mouseY.ToString("F3")));
		}
		DebugGUI.LogPersistent("smoothFrameRate", "SmoothFPS: " + (1f / Time.deltaTime).ToString("F3"));
		DebugGUI.LogPersistent("frameRate", "FPS: " + (1f / Time.smoothDeltaTime).ToString("F3"));
		if (Time.smoothDeltaTime != 0f)
		{
			DebugGUI.Graph("smoothFrameRate", 1f / Time.smoothDeltaTime);
		}
		if (Time.deltaTime != 0f)
		{
			DebugGUI.Graph("frameRate", 1f / Time.deltaTime);
		}
	}

	private void FixedUpdate()
	{
		DebugGUI.Graph("fixedFrameRateSin", Mathf.Sin(Time.time * 6f));
	}

	private void OnDestroy()
	{
		DebugGUI.RemoveGraph("frameRate");
		DebugGUI.RemoveGraph("fixedFrameRateSin");
		DebugGUI.RemovePersistent("frameRate");
	}
}
