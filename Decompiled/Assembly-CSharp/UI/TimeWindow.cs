using Game;
using Game.Messages;
using Game.State;
using TMPro;
using UI.Builder;
using UI.Common;
using UnityEngine;

namespace UI;

public class TimeWindow : MonoBehaviour, IProgrammaticWindow, IBuilderWindow
{
	private Window _window;

	private UIPanel _panel;

	public string WindowIdentifier => "Time";

	public Vector2Int DefaultSize => new Vector2Int(300, 150);

	public Window.Position DefaultPosition => Window.Position.UpperRight;

	public Window.Sizing Sizing => Window.Sizing.Fixed(DefaultSize);

	public UIBuilderAssets BuilderAssets { get; set; }

	public static TimeWindow Shared => WindowManager.Shared.GetWindow<TimeWindow>();

	public void Show()
	{
		Populate();
		_window.ShowWindow();
	}

	public static void Toggle()
	{
		if (Shared._window.IsShown)
		{
			Shared._window.CloseWindow();
		}
		else
		{
			Shared.Show();
		}
	}

	private void Awake()
	{
		_window = GetComponent<Window>();
	}

	private void OnDisable()
	{
		_panel?.Dispose();
		_panel = null;
	}

	private void Populate()
	{
		_window.Title = "Time";
		_panel?.Dispose();
		_panel = UIPanel.Create(_window.contentRectTransform, BuilderAssets, Build);
	}

	private void Build(UIPanelBuilder builder)
	{
		builder.Spacing = 8f;
		builder.AddLabel(GetTimeString, UIPanelBuilder.Frequency.Fast).HorizontalTextAlignment(HorizontalAlignmentOptions.Center);
		if (StateManager.CheckAuthorizedToSendMessage(default(WaitTime)))
		{
			builder.AddSection("Pass Time");
			builder.AddLabel("<style=Footnote>Does not affect train movement.");
			builder.HStack(delegate(UIPanelBuilder uIPanelBuilder)
			{
				uIPanelBuilder.AddButton("Wait 5 min", delegate
				{
					Wait(1f / 12f);
				});
				uIPanelBuilder.AddButton("15 min", delegate
				{
					Wait(0.25f);
				});
				uIPanelBuilder.AddButton("1 hr", delegate
				{
					Wait(1f);
				});
				uIPanelBuilder.AddButton("6 hr", delegate
				{
					Wait(6f);
				});
			});
			builder.HStack(delegate(UIPanelBuilder uIPanelBuilder)
			{
				uIPanelBuilder.AddButton("Sleep", delegate
				{
					GetSleepValues(out var _, out var _, out var hoursToSleep);
					Wait(hoursToSleep);
				}).Tooltip("Sleep", "Let time pass until the first scheduled interchange service of the day.");
				uIPanelBuilder.AddLabel(delegate
				{
					GetSleepValues(out var now, out var _, out var hoursToSleep);
					GameDateTime self = now.AddingHours(hoursToSleep);
					return self.IntervalString(now, GameDateTimeInterval.Style.Short) + " until " + self.TimeString();
				}, UIPanelBuilder.Frequency.Periodic).VerticalTextAlignment(VerticalAlignmentOptions.Middle);
			});
		}
		builder.AddExpandingVerticalSpacer();
	}

	private void GetSleepValues(out GameDateTime now, out int targetHour, out float hoursToSleep)
	{
		GameStorage storage = StateManager.Shared.Storage;
		now = TimeWeather.Now;
		float hours = now.Hours;
		targetHour = storage.InterchangeServeHour;
		hoursToSleep = ((hours < (float)targetHour) ? ((float)targetHour - hours) : (24f - hours + (float)targetHour));
	}

	private string GetTimeString()
	{
		GameDateTime now = TimeWeather.Now;
		float hours = now.Hours;
		int num = Mathf.FloorToInt(hours);
		float num2 = (hours - (float)num) * 60f;
		int num3 = Mathf.FloorToInt(num2);
		int num4 = Mathf.FloorToInt((num2 - (float)num3) * 60f);
		return $"<mspace=0.7em>{num}</mspace>:<mspace=0.7em>{num3:00}</mspace>:<mspace=0.7em>{num4:00}</mspace> Day {now.Day + 1}";
	}

	private static void Wait(float hours)
	{
		StateManager.ApplyLocal(new WaitTime
		{
			Hours = hours
		});
	}
}
