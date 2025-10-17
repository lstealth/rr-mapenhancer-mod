using System.IO;
using UnityEngine;

namespace AssetPack.Runtime;

public class AssetPackTest : MonoBehaviour
{
	private void LoadBundle(string assetPackName)
	{
		string text = Path.Combine(Application.persistentDataPath, "AssetPacks", assetPackName, "Bundle");
		Debug.Log("AssetBundle.LoadFromFile: " + text);
		AssetBundle.LoadFromFile(text);
	}

	private void Start()
	{
		LoadBundle("cube");
		LoadBundle("sphere");
	}
}
