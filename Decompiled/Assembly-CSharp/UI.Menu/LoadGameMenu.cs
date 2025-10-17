using System;
using System.Linq;
using Game.Persistence;
using TMPro;
using UI.Common;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu;

public class LoadGameMenu : MonoBehaviour, INavigationView
{
	[SerializeField]
	private TMP_Text titleLabel;

	[SerializeField]
	private LoadGameRow rowTemplate;

	[SerializeField]
	private RectTransform scrollViewContent;

	[SerializeField]
	private ToggleGroup toggleGroup;

	[SerializeField]
	private Button newButton;

	[SerializeField]
	private Button startButton;

	[SerializeField]
	private Button backButton;

	public Action OnNewGame;

	public Action<string> OnLoadGame;

	public RectTransform RectTransform => GetComponent<RectTransform>();

	private void Awake()
	{
		backButton.onClick.AddListener(delegate
		{
			this.NavigationController().Pop();
		});
		startButton.onClick.AddListener(delegate
		{
			Toggle toggle = toggleGroup.ActiveToggles().FirstOrDefault();
			if (!(toggle == null))
			{
				string saveName = toggle.GetComponentInParent<LoadGameRow>().SaveName;
				OnLoadGame?.Invoke(saveName);
			}
		});
		newButton.onClick.AddListener(delegate
		{
			OnNewGame?.Invoke();
		});
		rowTemplate.gameObject.SetActive(value: false);
	}

	public void Configure(string title, string startButtonTitle)
	{
		titleLabel.text = title;
		startButton.GetComponentInChildren<TMP_Text>().text = startButtonTitle;
	}

	private void Rebuild()
	{
		scrollViewContent.DestroyChildrenExcept(new Component[2] { rowTemplate, newButton });
		foreach (WorldStore.SaveInfo saveInfo in WorldStore.FindSaveInfos())
		{
			LoadGameRow loadGameRow = UnityEngine.Object.Instantiate(rowTemplate, scrollViewContent);
			loadGameRow.gameObject.SetActive(value: true);
			loadGameRow.OnClear = (Action)Delegate.Combine(loadGameRow.OnClear, (Action)delegate
			{
				ModalAlertController.Present("Delete " + saveInfo.Name + "?", "This cannot be undone.", new(bool, string)[2]
				{
					(true, "Delete"),
					(false, "Cancel")
				}, delegate(bool b)
				{
					if (b)
					{
						WorldStore.Clear(saveInfo.Name);
						Rebuild();
					}
				});
			});
			loadGameRow.Configure(saveInfo);
		}
		startButton.interactable = false;
		toggleGroup.SetAllTogglesOff();
	}

	public void ToggleValueChanged()
	{
		startButton.interactable = toggleGroup.AnyTogglesOn();
	}

	public void WillAppear()
	{
		Rebuild();
	}

	public void WillDisappear()
	{
	}

	public void DidPop()
	{
	}
}
