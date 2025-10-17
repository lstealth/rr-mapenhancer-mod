using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace WorldStreamer2;

public class UILoadingStreamer : MonoBehaviour
{
	[Tooltip("List of streamers objects that should affect loading screen. Drag and drop here all your streamer objects from scene hierarchy which should be used in loading screen.")]
	public Streamer[] streamers;

	public Image progressImg;

	[Tooltip("Time in seconds that you give your loading screen to get data from whole streamers about scene that they must load before loading screen will be switched off.")]
	public float waitTime = 2f;

	public UnityEvent onDone;

	private void Awake()
	{
		Streamer[] array = streamers;
		foreach (Streamer obj in array)
		{
			obj.loadingStreamer = this;
			obj.showLoadingScreen = true;
		}
		progressImg.fillAmount = 0f;
	}

	private void Update()
	{
		if (streamers.Length != 0)
		{
			bool flag = true;
			progressImg.fillAmount = 0f;
			Streamer[] array = streamers;
			foreach (Streamer streamer in array)
			{
				progressImg.fillAmount += streamer.LoadingProgress / (float)streamers.Length;
				flag = flag && streamer.initialized;
			}
			if (flag && progressImg.fillAmount >= 1f)
			{
				if (onDone != null)
				{
					onDone.Invoke();
				}
				StartCoroutine(TurnOff());
			}
		}
		else
		{
			Debug.Log("No streamer Attached");
		}
	}

	public IEnumerator TurnOff()
	{
		yield return new WaitForSeconds(waitTime);
		base.gameObject.SetActive(value: false);
	}

	public void Show()
	{
		progressImg.fillAmount = 0f;
		base.gameObject.SetActive(value: true);
	}
}
