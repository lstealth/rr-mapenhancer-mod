using UnityEngine;
using UnityEngine.SceneManagement;

namespace WorldStreamer2;

public class ColliderStreamer : MonoBehaviour
{
	[Tooltip("Scene name that belongs to this collider.")]
	public string sceneName;

	[Tooltip("Path where collider streamer should find scene which has to loaded after collider hit.")]
	public string scenePath;

	[HideInInspector]
	public GameObject sceneGameObject;

	[HideInInspector]
	public ColliderStreamerManager colliderStreamerManager;

	[Tooltip("If it's checkboxed only player could activate collider to start loading, otherwise every physical hit could activate it.")]
	public bool playerOnlyActivate = true;

	[Tooltip("Time in seconds after which scene will be unloaded when \"Player\" or object that activate loading will left collider area.")]
	public float unloadTimer;

	private bool loaded;

	private void Start()
	{
		colliderStreamerManager = GameObject.FindGameObjectWithTag(ColliderStreamerManager.COLLIDERSTREAMERMANAGERTAG).GetComponent<ColliderStreamerManager>();
		colliderStreamerManager.AddColliderStreamer(this);
	}

	public void SetSceneGameObject(GameObject sceneGameObject)
	{
		this.sceneGameObject = sceneGameObject;
		this.sceneGameObject.transform.position = base.transform.position;
	}

	private void OnTriggerEnter(Collider other)
	{
		if ((!playerOnlyActivate || other.transform == colliderStreamerManager.player) && !loaded)
		{
			loaded = true;
			SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if ((!playerOnlyActivate || other.transform == colliderStreamerManager.player) && (bool)sceneGameObject)
		{
			loaded = false;
			Invoke("UnloadScene", unloadTimer);
		}
	}

	private void UnloadScene()
	{
		SceneManager.UnloadSceneAsync(sceneGameObject.scene);
	}
}
