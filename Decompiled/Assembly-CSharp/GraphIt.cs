using UnityEngine;

public class GraphIt : MonoBehaviour
{
	public static GraphIt Instance => null;

	private void StepGraphInternal(GraphItData graph)
	{
	}

	private void LateUpdate()
	{
	}

	private void FixedUpdate()
	{
	}

	public static void GraphSetup(string graph, bool include_0, int sample_window)
	{
	}

	public static void GraphSetupInclude0(string graph, bool include_0)
	{
	}

	public static void GraphSetupHeight(string graph, float height)
	{
	}

	public static void GraphSetupHidden(string graph, bool hidden)
	{
	}

	public static void GraphSetupSampleWindowSize(string graph, int sample_window)
	{
	}

	public static void GraphSetupColour(string graph, Color color)
	{
	}

	public static void GraphSetupColour(string graph, string subgraph, Color color)
	{
	}

	public static void Log(string graph, float f)
	{
	}

	public static void Log(string graph, string subgraph, float f)
	{
	}

	public static void Log(string graph)
	{
	}

	public static void Log(string graph, string subgraph)
	{
	}

	public static void LogFixed(string graph, float f)
	{
	}

	public static void LogFixed(string graph, string subgraph, float f)
	{
	}

	public static void LogFixed(string graph)
	{
	}

	public static void LogFixed(string graph, string subgraph)
	{
	}

	public static void StepGraph(string graph)
	{
	}

	public static void PauseGraph(string graph)
	{
	}

	public static void UnpauseGraph(string graph)
	{
	}

	public static void ShareYAxis(string graph, bool shared_y_axis)
	{
	}
}
