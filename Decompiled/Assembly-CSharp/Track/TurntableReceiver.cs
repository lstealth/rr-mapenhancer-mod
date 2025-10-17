using System.Linq;
using Game.State;
using Helpers;
using Serilog;
using UnityEngine;

namespace Track;

public class TurntableReceiver : MonoBehaviour
{
	private struct Frame
	{
		public long Tick;

		public float Angle;

		public Frame(long tick, float angle)
		{
			Tick = tick;
			Angle = angle;
		}
	}

	private struct ScheduledStopIndex
	{
		public long Tick;

		public float Angle;

		public int? StopIndex;

		public ScheduledStopIndex(long tick, float angle, int? stopIndex)
		{
			Tick = tick;
			Angle = angle;
			StopIndex = stopIndex;
		}
	}

	public TurntableController turntableController;

	public const long Delay = 300L;

	private readonly CircularBuffer<Frame> _frames = new CircularBuffer<Frame>(4);

	private ScheduledStopIndex? _scheduledStopIndex;

	private long displayTick => StateManager.Now - 300;

	private void Start()
	{
	}

	private void FixedUpdate()
	{
		UpdatePosition();
	}

	public static TurntableReceiver ReceiverForTurntableId(string turntableId)
	{
		return Object.FindObjectsOfType<TurntableReceiver>().FirstOrDefault((TurntableReceiver tr) => tr.turntableController.turntable.id == turntableId);
	}

	public void HandleUpdateAngle(long tick, float angle)
	{
		Log.Debug("HandleUpdateAngle {tick} {angle}", tick, angle);
		_frames.Enqueue(new Frame(tick, angle));
	}

	public void HandleUpdateStopIndex(long tick, float angle, int? stopIndex)
	{
		Log.Debug("HandleUpdateStopIndex {tick} {angle}, {stopIndex}", tick, angle, stopIndex);
		_scheduledStopIndex = new ScheduledStopIndex(tick, angle, stopIndex);
	}

	private void UpdatePosition()
	{
		if (_scheduledStopIndex.HasValue && _scheduledStopIndex.Value.Tick <= displayTick)
		{
			turntableController.SetAngle(_scheduledStopIndex.Value.Angle);
			turntableController.turntable.SetStopIndex(_scheduledStopIndex.Value.StopIndex);
			_scheduledStopIndex = null;
		}
		if (!_frames.IsEmpty)
		{
			while (_frames.Length > 1 && _frames.Peek(1).Tick < displayTick)
			{
				_frames.Dequeue();
			}
			if (_frames.Length != 1)
			{
				Frame frame = _frames.Peek();
				Frame frame2 = _frames.Peek(1);
				MoveBetween(frame, frame2);
			}
		}
	}

	private Frame Extrapolate(Frame frame)
	{
		return new Frame(displayTick, frame.Angle);
	}

	private void MoveBetween(Frame frame0, Frame frame1)
	{
		if (turntableController.turntable.StopIndex.HasValue)
		{
			Log.Debug("Turntable: MoveBetween: Do nothing, stopIndex.hasvalue");
			return;
		}
		float t = Mathf.Clamp01(Mathf.InverseLerp(frame0.Tick, frame1.Tick, displayTick));
		float angle = Mathf.LerpAngle(frame0.Angle, frame1.Angle, t);
		turntableController.SetAngle(angle);
	}
}
