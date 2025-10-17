using System;
using Game.State;
using Helpers;
using JetBrains.Annotations;
using Model;
using Network;
using Serilog;
using UnityEngine;

namespace Avatar;

public class RemoteAvatar : MonoBehaviour
{
	private struct Frame
	{
		public readonly long Tick;

		public readonly Vector3 Position;

		public readonly Vector3 Forward;

		public readonly Vector3 Look;

		public readonly Vector3 Velocity;

		[CanBeNull]
		public readonly string RelativeToCarId;

		public readonly AvatarPose Pose;

		public Frame(long tick, Vector3 position, Vector3 forward, Vector3 look, Vector3 velocity, string relativeToCarId, AvatarPose pose)
		{
			Tick = tick;
			Position = position;
			Forward = forward;
			Look = look;
			Velocity = velocity;
			RelativeToCarId = relativeToCarId;
			Pose = pose;
		}
	}

	[NonSerialized]
	public AvatarPrefab avatar;

	private const long Delay = 300L;

	private readonly CircularBuffer<Frame> _frames = new CircularBuffer<Frame>(4);

	private AvatarAnimator Animator => avatar.Animator;

	private Rigidbody Rigidbody => avatar.Rigidbody;

	private static long DisplayTick => StateManager.Now - 300;

	private void Awake()
	{
		GraphIt.GraphSetupSampleWindowSize("Avatar-MoveBetween", 500);
	}

	private void FixedUpdate()
	{
		UpdatePosition();
	}

	private void UpdatePosition()
	{
		if (!_frames.IsEmpty)
		{
			while (_frames.Length > 1 && _frames.Peek(1).Tick < DisplayTick)
			{
				_frames.Dequeue();
			}
			if (_frames.Length == 1)
			{
				Frame frame = _frames.Peek();
				Frame frame2 = Extrapolate(frame);
				MoveBetween(frame, frame2);
			}
			else
			{
				Frame frame3 = _frames.Peek();
				Frame frame4 = _frames.Peek(1);
				MoveBetween(frame3, frame4);
			}
		}
	}

	private Frame Extrapolate(Frame frame)
	{
		long displayTick = DisplayTick;
		float a = NetworkTime.Elapsed(frame.Tick, displayTick);
		a = Mathf.Min(a, 1f);
		return new Frame(displayTick, frame.Position + frame.Velocity * a, frame.Forward, frame.Look, frame.Velocity, frame.RelativeToCarId, frame.Pose);
	}

	private void MoveBetween(Frame frame0, Frame frame1)
	{
		float t = Mathf.Clamp01(Mathf.InverseLerp(frame0.Tick, frame1.Tick, DisplayTick));
		(Vector3, Quaternion, Quaternion, Vector3, Vector3)? tuple = TRVFromFrame(frame0);
		(Vector3, Quaternion, Quaternion, Vector3, Vector3)? tuple2 = TRVFromFrame(frame1);
		if (!tuple.HasValue || !tuple2.HasValue)
		{
			Log.Warning("Couldn't create TRV(s) for frame - bad car? {carId}", frame0.RelativeToCarId);
			return;
		}
		(Vector3, Quaternion, Quaternion, Vector3, Vector3) value = tuple.Value;
		Vector3 item = value.Item1;
		Quaternion item2 = value.Item2;
		Quaternion item3 = value.Item3;
		Vector3 item4 = value.Item4;
		Vector3 item5 = value.Item5;
		(Vector3, Quaternion, Quaternion, Vector3, Vector3) value2 = tuple2.Value;
		Vector3 item6 = value2.Item1;
		Quaternion item7 = value2.Item2;
		Quaternion item8 = value2.Item3;
		Vector3 item9 = value2.Item4;
		Vector3 item10 = value2.Item5;
		Vector3 position = Vector3.Lerp(item, item6, t);
		Quaternion look = Quaternion.Lerp(item3, item8, t);
		Quaternion rotation = Quaternion.Lerp(item2, item7, t);
		Vector3 velocity = Vector3.Lerp(item4, item9, t);
		Vector3 animationVelocity = Vector3.Lerp(item5, item10, t);
		AvatarPose pose = frame1.Pose;
		ApplyToAvatar(position, rotation, look, velocity, animationVelocity, pose);
	}

	private void ApplyToAvatar(Vector3 position, Quaternion rotation, Quaternion look, Vector3 velocity, Vector3 animationVelocity, AvatarPose pose)
	{
		Vector3 v = Quaternion.Inverse(rotation) * animationVelocity;
		Rigidbody.MovePosition(position);
		Rigidbody.MoveRotation(rotation);
		Animator.SetVelocity(v, position + look * Vector3.forward * 10f);
		Animator.SetPose(pose);
	}

	private (Vector3 worldPosition, Quaternion forwardRotation, Quaternion lookRotation, Vector3 velocity, Vector3 animationVelocity)? TRVFromFrame(Frame f)
	{
		if (f.RelativeToCarId == null)
		{
			return (WorldTransformer.GameToWorld(f.Position), Quaternion.LookRotation(f.Forward), Quaternion.LookRotation(f.Look), f.Velocity, f.Velocity);
		}
		Car car = TrainController.Shared.CarForId(f.RelativeToCarId);
		if (car == null)
		{
			Log.Warning("RemoteAvatar: Car not found for frame: {carId}", f.RelativeToCarId);
			return null;
		}
		Car.MotionSnapshot motionSnapshot = car.GetMotionSnapshot();
		Vector3 velocity = motionSnapshot.Velocity;
		Vector3 vector = motionSnapshot.Rotation * f.Position + motionSnapshot.Position;
		float num = 0f - NetworkTime.Elapsed(f.Tick, DisplayTick);
		return (vector + velocity * num, motionSnapshot.Rotation * Quaternion.LookRotation(f.Forward), motionSnapshot.Rotation * Quaternion.LookRotation(f.Look), motionSnapshot.Velocity + f.Velocity, f.Velocity);
	}

	private bool IsCloseTo(Frame frame)
	{
		float magnitude = (frame.Position - Rigidbody.position).magnitude;
		float magnitude2 = (frame.Look - Rigidbody.rotation * Vector3.forward).magnitude;
		if (magnitude < 0.001f)
		{
			return magnitude2 < 0.01f;
		}
		return false;
	}

	public void AddPosition(long tick, Vector3 position, Vector3 forward, Vector3 look, Vector3 velocity, [CanBeNull] string relativeToCarId, AvatarPose pose)
	{
		_frames.Enqueue(new Frame(tick, position, forward, look, velocity, relativeToCarId, pose));
	}
}
