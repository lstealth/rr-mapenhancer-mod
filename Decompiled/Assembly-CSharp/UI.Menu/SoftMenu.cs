using System;
using TMPro;
using UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu;

public class SoftMenu : MonoBehaviour, INavigationView
{
	[SerializeField]
	private TMP_Text titleText;

	[SerializeField]
	private RectTransform buttonTransform;

	[SerializeField]
	private Button buttonPrefab;

	[SerializeField]
	private Button backButton;

	public RectTransform RectTransform => GetComponent<RectTransform>();

	public void Configure(string title, bool wantsBackButton = true)
	{
		if (titleText != null)
		{
			titleText.text = title;
		}
		backButton.gameObject.SetActive(wantsBackButton);
	}

	public void AddButton(string title, Action action)
	{
		Button button = UnityEngine.Object.Instantiate(buttonPrefab, buttonTransform);
		button.GetComponentInChildren<TMP_Text>().text = title;
		button.onClick.AddListener(action.Invoke);
		backButton.transform.SetAsLastSibling();
	}

	protected virtual void Awake()
	{
		backButton.onClick.AddListener(delegate
		{
			this.NavigationController().Pop();
		});
	}

	public void WillAppear()
	{
	}

	public void WillDisappear()
	{
	}

	public void DidPop()
	{
	}
}
