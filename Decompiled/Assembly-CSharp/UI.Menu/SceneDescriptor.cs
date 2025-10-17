using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI.Menu;

public readonly struct SceneDescriptor
{
	private readonly string _path;

	private readonly int _buildIndex;

	public static SceneDescriptor MainMenu = new SceneDescriptor("Assets/Scenes/MainMenu.unity");

	public static SceneDescriptor GameUI = new SceneDescriptor("Assets/Scenes/GameUI.unity");

	public static SceneDescriptor PersistentLoader = new SceneDescriptor("Assets/Scenes/Persistent.unity");

	public static SceneDescriptor BushnellWhittier = new SceneDescriptor("Assets/Scenes/BushnellWhittier-MapManager.unity");

	public static SceneDescriptor EnvironmentEnviro = new SceneDescriptor("Assets/Scenes/Environment-Enviro.unity");

	public static SceneDescriptor Editor = new SceneDescriptor("Assets/Scenes/Bryson-Editor.unity");

	public static SceneDescriptor Tests = new SceneDescriptor("Assets/Scenes/Tests.unity");

	public bool IsLoaded => Scene.isLoaded;

	public Scene Scene
	{
		get
		{
			if (!UsePath)
			{
				return SceneManager.GetSceneByBuildIndex(_buildIndex);
			}
			return SceneManager.GetSceneByPath(_path);
		}
	}

	private bool UsePath => !string.IsNullOrEmpty(_path);

	public SceneDescriptor(string path)
	{
		_path = path;
		_buildIndex = -1;
	}

	public SceneDescriptor(int buildIndex)
	{
		_path = null;
		_buildIndex = buildIndex;
	}

	public void LoadSync(LoadSceneMode loadSceneMode)
	{
		if (UsePath)
		{
			SceneManager.LoadScene(_path, loadSceneMode);
		}
		else
		{
			SceneManager.LoadScene(_buildIndex, loadSceneMode);
		}
	}

	public AsyncOperation LoadAsync(LoadSceneMode loadSceneMode)
	{
		if (!UsePath)
		{
			return SceneManager.LoadSceneAsync(_buildIndex, loadSceneMode);
		}
		return SceneManager.LoadSceneAsync(_path, loadSceneMode);
	}

	public AsyncOperation UnloadAsync()
	{
		if (!UsePath)
		{
			return SceneManager.UnloadSceneAsync(_buildIndex);
		}
		return SceneManager.UnloadSceneAsync(_path);
	}

	public override string ToString()
	{
		if (!UsePath)
		{
			int buildIndex = _buildIndex;
			return buildIndex.ToString();
		}
		return _path;
	}
}
