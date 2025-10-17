using System.Collections.Generic;
using Model;
using TMPro;
using UnityEngine;

namespace UI;

public class FPSDisplay : MonoBehaviour
{
	public enum DisplayMode
	{
		FPS,
		MS
	}

	private bool _run;

	[SerializeField]
	private TMP_Text label;

	[SerializeField]
	public DisplayMode displayMode = DisplayMode.MS;

	[SerializeField]
	[Range(0.1f, 2f)]
	private float refreshPeriod = 1f;

	[SerializeField]
	private Gradient gradient;

	private const int FrameBufferSize = 120;

	private readonly List<float> _frameTimes = new List<float>(120);

	private readonly List<float> _sortedFrameTimes = new List<float>(120);

	private const float GoodFrameDuration = 1f / 60f;

	private const float BadFrameDuration = 0.05f;

	private int _frames;

	private float _duration;

	private float _onePercentLowDuration;

	private float _onePercentHighDuration;

	public bool Run
	{
		set
		{
			_run = value;
			if (_run)
			{
				ClearState();
			}
			else
			{
				label.text = null;
			}
		}
	}

	private void OnEnable()
	{
		if (label != null)
		{
			label.text = null;
		}
	}

	private void Update()
	{
		float unscaledDeltaTime = Time.unscaledDeltaTime;
		_frameTimes.Add(unscaledDeltaTime);
		if (_frameTimes.Count > 120)
		{
			_frameTimes.RemoveAt(0);
		}
		_frames++;
		_duration += unscaledDeltaTime;
		if (_run && _duration >= refreshPeriod)
		{
			var (num, num2) = GetCarCounts();
			CalculatePercentiles();
			float num3 = _duration / (float)_frames;
			if (displayMode == DisplayMode.FPS)
			{
				label.SetText("{0}/{1} {2:0} {3:0} {4:0}", num, num2, 1f / _onePercentHighDuration, 1f / num3, 1f / _onePercentLowDuration);
			}
			else
			{
				label.SetText("{0}/{1} {2:1} {3:1} {4:1}", num, num2, 1000f * _onePercentHighDuration, 1000f * num3, 1000f * _onePercentLowDuration);
			}
			label.color = gradient.Evaluate(Mathf.InverseLerp(1f / 60f, 0.05f, _onePercentLowDuration));
			ClearState();
		}
	}

	private void CalculatePercentiles()
	{
		if (_frameTimes.Count != 0)
		{
			_sortedFrameTimes.Clear();
			_sortedFrameTimes.AddRange(_frameTimes);
			_sortedFrameTimes.Sort();
			int index = Mathf.Min(_sortedFrameTimes.Count - 1, Mathf.FloorToInt((float)_sortedFrameTimes.Count * 0.01f));
			int index2 = Mathf.Max(0, Mathf.FloorToInt((float)_sortedFrameTimes.Count * 0.99f));
			_onePercentHighDuration = _sortedFrameTimes[index];
			_onePercentLowDuration = _sortedFrameTimes[index2];
		}
	}

	private void ClearState()
	{
		_frames = 0;
		_duration = 0f;
	}

	private static (int numVisibleCars, int numTotalCars) GetCarCounts()
	{
		TrainController shared = TrainController.Shared;
		int num = 0;
		int num2 = 0;
		foreach (Car car in shared.Cars)
		{
			if (car.BodyTransform != null)
			{
				num++;
			}
			num2++;
		}
		return (numVisibleCars: num, numTotalCars: num2);
	}
}
