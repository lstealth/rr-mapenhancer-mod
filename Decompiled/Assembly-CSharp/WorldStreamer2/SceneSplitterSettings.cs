using System.Collections.Generic;
using UnityEngine;

namespace WorldStreamer2;

public class SceneSplitterSettings : MonoBehaviour
{
	public string scenesPath = "NatureManufacture Assets/WorldStreamer/SplitScenes";

	public List<SceneCollectionManager> sceneCollectionManagers = new List<SceneCollectionManager>();

	public string GetScenesPath()
	{
		string text = scenesPath;
		if (!text.StartsWith("Assets/"))
		{
			text = ((!text.StartsWith("/") && !text.StartsWith("\\")) ? ("Assets/" + scenesPath) : ("Assets" + scenesPath));
		}
		if (text[text.Length - 1] != '/' && text[text.Length - 1] != '\\')
		{
			text += "/";
		}
		return text;
	}
}
