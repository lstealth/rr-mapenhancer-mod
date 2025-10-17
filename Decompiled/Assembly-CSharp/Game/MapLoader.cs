using System.Collections;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Game.State;
using Track;
using UI.Menu;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Game;

[RequireComponent(typeof(Graph))]
public class MapLoader : MonoBehaviour
{
	[SerializeField]
	private GameObject eventSystemPrefab;

	private Graph _graph;

	private void OnEnable()
	{
		_graph = GetComponent<Graph>();
		StartCoroutine(LoadUI());
	}

	private void OnDisable()
	{
		if (TrainController.Shared != null && TrainController.Shared.graph == _graph)
		{
			TrainController.Shared.graph = null;
		}
		SceneManager.UnloadSceneAsync(SceneDescriptor.GameUI.Scene);
	}

	private IEnumerator LoadUI()
	{
		if (Application.isEditor)
		{
			if (Object.FindObjectOfType<EventSystem>() == null)
			{
				Debug.Log("EventSystem not found; instantiating.");
				Object.Instantiate(eventSystemPrefab);
			}
			if (Object.FindObjectOfType<StateManager>() == null)
			{
				Debug.Log("StateManager not found; instantiating.");
				base.gameObject.AddComponent<StateManager>();
			}
			if (!SceneDescriptor.PersistentLoader.IsLoaded)
			{
				SceneDescriptor.GameUI.LoadSync(LoadSceneMode.Additive);
			}
		}
		yield return null;
		TrainController.Shared.graph = _graph;
		yield return new WaitForEndOfFrame();
		Messenger.Default.Send(default(MapDidLoadEvent));
	}
}
