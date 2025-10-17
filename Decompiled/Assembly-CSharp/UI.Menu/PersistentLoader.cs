using Core;
using Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu;

public class PersistentLoader : MonoBehaviour
{
	[SerializeField]
	private GlobalGameManager gameManager;

	[SerializeField]
	private GameObject loadingScreen;

	[SerializeField]
	private Slider progressSlider;

	[SerializeField]
	private TextMeshProUGUI progressText;

	private void Awake()
	{
		gameManager.OnPersistentLoaderAwake(this);
	}

	private void OnDestroy()
	{
		Multiplayer.StopServer();
		BezierCurve.DestroyStaticStorage();
	}

	public void ShowLoadingScreen(bool show)
	{
		loadingScreen.SetActive(show);
	}

	public void ShowProgress(int intProgress, string text)
	{
		progressSlider.value = intProgress;
		progressText.text = text;
	}
}
