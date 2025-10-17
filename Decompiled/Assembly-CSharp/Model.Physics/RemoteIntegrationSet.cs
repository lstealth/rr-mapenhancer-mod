using System;
using System.Collections.Generic;
using Network;
using Network.Client;
using RollingStock;
using Serilog;
using Track;
using UnityEngine;

namespace Model.Physics;

public class RemoteIntegrationSet : IntegrationSet
{
	private struct Frame
	{
		public long Tick;

		public Location[] Locations;

		public float[] Velocities;

		public Frame(Location[] locations, float[] velocities, long tick)
		{
			Locations = locations;
			Velocities = velocities;
			Tick = tick;
		}
	}

	private readonly List<Frame> _frames = new List<Frame>(4);

	private Frame _extrapolated;

	private long DisplayTick
	{
		get
		{
			ClientManager client = Multiplayer.Client;
			if (client == null)
			{
				Log.Error("displayTick called with null Multiplayer.Client");
				return 0L;
			}
			return client.Tick - 300;
		}
	}

	public override bool ShouldSkipTick => false;

	public RemoteIntegrationSet(uint id, IReadOnlyCollection<Car> cars, Graph graph, bool activeCars, IIntegrationSetEventHandler eventHandler)
		: base(id, cars, graph, activeCars, eventHandler)
	{
		int count = cars.Count;
		_extrapolated.Locations = new Location[count];
	}

	public override void Tick(float dt)
	{
		long displayTick = DisplayTick;
		while (_frames.Count >= 2 && _frames[1].Tick < displayTick)
		{
			_frames.RemoveAt(0);
		}
		if (_frames.Count == 0)
		{
			return;
		}
		Frame frame = _frames[0];
		if (_frames.Count == 1 || displayTick < frame.Tick)
		{
			Frame frame2 = Extrapolate(frame, displayTick);
			MoveTo(dt, frame2);
			return;
		}
		Frame frame3 = _frames[1];
		try
		{
			MoveBetween(dt, frame, frame3, displayTick);
		}
		catch (EndOfTrack)
		{
			_frames.RemoveAt(0);
		}
	}

	private void MoveBetween(float dt, Frame frame0, Frame frame1, long displayTick)
	{
		if (frame0.Locations.Length == frame1.Locations.Length && frame0.Locations.Length == _elements.Count)
		{
			float num = Mathf.Clamp01(Mathf.InverseLerp(frame0.Tick, frame1.Tick, displayTick));
			for (int i = 0; i < _elements.Count; i++)
			{
				Element element = _elements[i];
				Location loc = _graph.Lerp(frame0.Locations[i], frame1.Locations[i], num);
				float vel = Mathf.Lerp(frame0.Velocities[i], frame1.Velocities[i], num);
				MoveCarTo(element.car, loc, vel, dt);
			}
		}
	}

	private void MoveTo(float dt, Frame frame)
	{
		for (int i = 0; i < _elements.Count; i++)
		{
			Element element = _elements[i];
			Location loc = frame.Locations[i];
			float vel = frame.Velocities[i];
			MoveCarTo(element.car, loc, vel, dt);
		}
	}

	private void MoveCarTo(Car car, Location loc, float vel, float dt)
	{
		float distanceBetweenClose = _graph.GetDistanceBetweenClose(car.WheelBoundsA, loc);
		car.PositionWheelBoundsA(movementInfo: new MovementInfo(dt, distanceBetweenClose, car.NormalizedTractiveEffort), wbA: loc, graph: _graph, update: true);
		float orientation = car.Orientation;
		car.velocity = vel * orientation;
	}

	private Frame Extrapolate(Frame frame, long displayTick)
	{
		float value = NetworkTime.Elapsed(frame.Tick, displayTick);
		value = Mathf.Clamp(value, -4f, 4f);
		Location[] locations = _extrapolated.Locations;
		for (int i = 0; i < locations.Length; i++)
		{
			float distance = value * frame.Velocities[i];
			locations[i] = _graph.LocationByMoving(frame.Locations[i], distance, checkSwitchAgainstMovement: false, stopAtEndOfTrack: true);
		}
		_extrapolated.Tick = displayTick;
		_extrapolated.Velocities = frame.Velocities;
		return _extrapolated;
	}

	private void AddFrame(Frame frame)
	{
		_frames.Add(frame);
		_frames.Sort((Frame a, Frame b) => a.Tick.CompareTo(b.Tick));
	}

	public override void HandleCarPositionUpdate(Location wheelBoundsA, float[] positions, float[] velocities, long updateTick)
	{
		Location[] locations = LocationsFrom(wheelBoundsA, positions);
		AddFrame(new Frame(locations, velocities, updateTick));
	}

	private Location[] LocationsFrom(Location wheelBoundsA, float[] positions)
	{
		if (positions.Length == 0)
		{
			return Array.Empty<Location>();
		}
		Location[] array = new Location[positions.Length];
		array[0] = wheelBoundsA;
		for (int i = 1; i < positions.Length; i++)
		{
			float num = positions[i];
			Location start = array[i - 1];
			array[i] = _graph.LocationByMoving(start, 0f - num, checkSwitchAgainstMovement: false, stopAtEndOfTrack: true);
		}
		return array;
	}
}
