using System;
using System.Collections;
using GalaSoft.MvvmLight.Messaging;
using Game;
using Game.Events;
using Game.State;
using TMPro;
using UI.CompanyWindow;
using UI.Console;
using UI.EngineRoster;
using UI.Equipment;
using UI.Guide;
using UI.Placer;
using UI.PreferencesWindow;
using UI.SwitchList;
using UI.Timetable;
using UI.Tutorial;
using UnityEngine;
using UnityEngine.UI;

namespace UI;

public class TopRightArea : MonoBehaviour
{
	[SerializeField]
	private CanvasGroup canvasGroup;

	[SerializeField]
	private TMP_Text clockText;

	[SerializeField]
	private Button tutorialButton;

	[SerializeField]
	private Button timetableButton;

	[SerializeField]
	private bool showSeconds = true;

	private Coroutine _coroutine;

	private IDisposable _timetableFeatureObserver;

	private static bool ShowAlways => Preferences.ShowClockAlways;

	private void OnEnable()
	{
		canvasGroup.alpha = 1f;
		Messenger.Default.Register<UISettingDidChange>(this, delegate
		{
			HandleShowAlwaysChanged();
		});
		Messenger.Default.Register<PropertiesDidRestore>(this, delegate
		{
			HandlePropertiesDidRestore();
		});
		if (!ShowAlways)
		{
			LTSeq lTSeq = LeanTween.sequence();
			lTSeq.append(5f);
			lTSeq.append(LeanTween.alphaCanvas(canvasGroup, 0f, 0.25f));
		}
		_coroutine = StartCoroutine(UpdateCoroutine());
	}

	private void HandlePropertiesDidRestore()
	{
		tutorialButton.gameObject.SetActive(StateManager.Shared.HasTutorial && !TutorialManager.Shared.Complete);
		GameStorage storage = StateManager.Shared.Storage;
		_timetableFeatureObserver = storage.ObserveTimetableFeature(delegate(bool timetableFeature)
		{
			timetableButton.gameObject.SetActive(timetableFeature);
		}, callInitial: true);
	}

	private void OnDisable()
	{
		Messenger.Default.Unregister(this);
		StopCoroutine(_coroutine);
		_coroutine = null;
		_timetableFeatureObserver?.Dispose();
		_timetableFeatureObserver = null;
	}

	private void HandleShowAlwaysChanged()
	{
		SetVisible(ShowAlways);
	}

	private IEnumerator UpdateCoroutine()
	{
		WaitForSecondsRealtime wait = new WaitForSecondsRealtime(0.05f);
		while (true)
		{
			float hours = TimeWeather.Now.Hours;
			int num = Mathf.FloorToInt(hours);
			float num2 = (hours - (float)num) * 60f;
			int num3 = Mathf.FloorToInt(num2);
			int num4 = Mathf.FloorToInt((num2 - (float)num3) * 60f);
			if (showSeconds)
			{
				int num5 = Mathf.RoundToInt(TimeWeather.TimeMultiplier);
				int num6 = ((num5 > 6) ? 10 : (num5 switch
				{
					1 => 1, 
					2 => 2, 
					_ => 5, 
				}));
				int num7 = num6;
				num4 = Mathf.FloorToInt((float)num4 / (float)num7) * num7;
				clockText.SetText("<mspace=0.7em>{0}</mspace>:<mspace=0.7em>{1:00}</mspace>:<mspace=0.7em>{2:00}</mspace>", num, num3, num4);
			}
			else
			{
				clockText.SetText("{0}:{1:00}", num, num3);
			}
			yield return wait;
		}
	}

	private void SetVisible(bool visible)
	{
		LeanTween.cancel(canvasGroup.gameObject);
		LTSeq lTSeq = LeanTween.sequence();
		if (visible)
		{
			lTSeq.append(LeanTween.alphaCanvas(canvasGroup, 1f, 0.125f));
			return;
		}
		lTSeq.append(0.5f);
		lTSeq.append(LeanTween.alphaCanvas(canvasGroup, 0f, 0.25f));
	}

	public void OnHover(bool isHovering)
	{
		SetVisible(isHovering || ShowAlways);
	}

	public void ClickedCompany()
	{
		UI.CompanyWindow.CompanyWindow.Toggle();
	}

	public void ClickedTimetable()
	{
		TimetableWindow.Toggle();
	}

	public void ClickedSwitchList()
	{
		SwitchListPanel.Toggle();
	}

	public void ClickedProfile()
	{
		UI.PreferencesWindow.PreferencesWindow.Toggle();
	}

	public void ClickedGuide()
	{
		GuideWindow.Toggle();
	}

	public void ClickedTutorial()
	{
		TutorialManager.Shared.Show();
	}

	public void ClickedConsole()
	{
		UI.Console.Console.shared.Toggle();
	}

	public void ClickedRoster()
	{
		EngineRosterPanel.Toggle();
	}

	public void ClickedPurchase()
	{
		if (StateManager.Shared.GameMode == GameMode.Sandbox)
		{
			PlacerWindow.Toggle();
		}
		else
		{
			EquipmentWindow.Toggle();
		}
	}

	public void ClickedBalance()
	{
		UI.CompanyWindow.CompanyWindow.Shared.ShowFinance();
	}

	public void ClickedClock()
	{
		TimeWindow.Shared.Show();
	}
}
