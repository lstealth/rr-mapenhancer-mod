using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WorldStreamer2;

public class StreamerLoadingManager
{
	private enum LoadingState
	{
		Loading,
		Unloading
	}

	public Streamer streamer;

	private List<Scene> scenesToUnload = new List<Scene>();

	private List<SceneSplit> scenesToLoad = new List<SceneSplit>();

	private List<AsyncOperation> asyncOperations = new List<AsyncOperation>();

	private LoadingState loadingState;

	private bool operationStarted;

	public int ScenesToUnloadCount => scenesToUnload.Count;

	public int ScenesToLoadCount => scenesToLoad.Count;

	public int AsyncOperationsCount => asyncOperations.Count;

	public void Update()
	{
		if (operationStarted || asyncOperations.Count > 0)
		{
			return;
		}
		if (loadingState == LoadingState.Unloading)
		{
			if (scenesToLoad.Count > 0)
			{
				loadingState = LoadingState.Loading;
			}
			else if (scenesToUnload.Count > 0)
			{
				operationStarted = true;
				streamer.StartCoroutine(UnloadAsync());
			}
		}
		if (loadingState == LoadingState.Loading)
		{
			if (scenesToLoad.Count > 0)
			{
				operationStarted = true;
				streamer.StartCoroutine(LoadAsync());
			}
			if (scenesToLoad.Count == 0)
			{
				loadingState = LoadingState.Unloading;
			}
		}
	}

	private void Load()
	{
		_ = new int[scenesToLoad.Count];
		int sceneID = SceneManager.sceneCount;
		SceneSplit split = scenesToLoad[0];
		AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(split.sceneName, LoadSceneMode.Additive);
		asyncOperation.completed += delegate(AsyncOperation operation)
		{
			SceneLoadComplete(sceneID, split);
			OnOperationDone(operation);
		};
		asyncOperations.Add(asyncOperation);
		scenesToLoad.RemoveAt(0);
		operationStarted = false;
	}

	private IEnumerator LoadAsync()
	{
		_ = new int[scenesToLoad.Count];
		for (int i = 0; i < scenesToLoad.Count; i++)
		{
			int sceneID = SceneManager.sceneCount;
			SceneSplit split = scenesToLoad[i];
			AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(split.sceneName, LoadSceneMode.Additive);
			asyncOperation.completed += delegate(AsyncOperation operation)
			{
				SceneLoadComplete(sceneID, split);
				OnOperationDone(operation);
			};
			asyncOperations.Add(asyncOperation);
			yield return null;
		}
		scenesToLoad.Clear();
		operationStarted = false;
	}

	private void SceneLoadComplete(int sceneID, SceneSplit split)
	{
		streamer.StartCoroutine(SceneLoadCompleteAsync(sceneID, split));
	}

	private IEnumerator SceneLoadCompleteAsync(int sceneID, SceneSplit split)
	{
		yield return null;
		streamer.OnSceneLoaded(SceneManager.GetSceneAt(sceneID), split);
	}

	private void OnOperationDone(AsyncOperation asyncOperation)
	{
		streamer.StartCoroutine(RemoveAsyncOperation(asyncOperation));
	}

	private IEnumerator RemoveAsyncOperation(AsyncOperation asyncOperation)
	{
		yield return null;
		yield return null;
		asyncOperations.Remove(asyncOperation);
	}

	private void Unload()
	{
		AsyncOperation asyncOperation = SceneManager.UnloadSceneAsync(scenesToUnload[0]);
		asyncOperation.completed += OnOperationDone;
		asyncOperations.Add(asyncOperation);
		scenesToUnload.RemoveAt(0);
		operationStarted = false;
	}

	private IEnumerator UnloadAsync()
	{
		yield return null;
		for (int i = 0; i < scenesToUnload.Count; i++)
		{
			AsyncOperation asyncOperation = SceneManager.UnloadSceneAsync(scenesToUnload[i]);
			asyncOperation.completed += OnOperationDone;
			asyncOperations.Add(asyncOperation);
			yield return null;
		}
		scenesToUnload.Clear();
		operationStarted = false;
	}

	public void UnloadSceneAsync(Scene scene)
	{
		scenesToUnload.Add(scene);
	}

	public void LoadSceneAsync(SceneSplit split)
	{
		scenesToLoad.Add(split);
	}
}
