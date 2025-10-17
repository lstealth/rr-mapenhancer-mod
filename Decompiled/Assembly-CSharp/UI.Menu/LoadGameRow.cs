using System;
using Game.Persistence;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu;

public class LoadGameRow : MonoBehaviour
{
	[SerializeField]
	private Toggle toggle;

	[SerializeField]
	private TMP_Text titleLabel;

	[SerializeField]
	private TMP_Text subtitleLabel;

	[SerializeField]
	private TMP_Text dateLabel;

	[SerializeField]
	private Button clearButton;

	public Action OnClear;

	public string SaveName { get; private set; }

	public void Configure(WorldStore.SaveInfo info)
	{
		SaveName = info.Name;
		titleLabel.text = info.Name;
		subtitleLabel.text = "";
		dateLabel.text = info.Date.ToShortDateString() + " " + info.Date.ToShortTimeString();
	}

	private void Awake()
	{
		clearButton.onClick.AddListener(delegate
		{
			OnClear?.Invoke();
		});
	}
}
