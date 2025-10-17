using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using Helpers.Animation;
using Model;
using Model.Definition.Data;
using Track;
using UnityEngine;

namespace RollingStock.Steam;

[RequireComponent(typeof(Animator))]
public class SteamLocomotiveWheelAnimator : MonoBehaviour, ISteamLocomotiveSubcomponent
{
	[Serializable]
	public struct WheelAnimation
	{
		public AnimationClip clip;

		public float diameter;

		public bool isDriver;

		public Transform transform;

		[NonSerialized]
		public float Parameter;

		[NonSerialized]
		public Vector3 InitialPosition;

		[NonSerialized]
		public Quaternion InitialRotation;

		[NonSerialized]
		public Vector3 InitialPositionLocalToBody;

		[NonSerialized]
		public Quaternion InitialParentRotationLocalToBody;

		[NonSerialized]
		public Vector3 TargetPosition;

		[NonSerialized]
		public Quaternion TargetRotation;
	}

	public WheelAnimation[] wheels;

	private PlayableHandle[] _playables;

	private SteamLocomotive _locomotive;

	private Coroutine _updateWheelTargetsCoroutine;

	private WheelAudio _wheelAudio;

	private SteamLocomotive Locomotive
	{
		get
		{
			if (_locomotive != null)
			{
				return _locomotive;
			}
			_locomotive = base.gameObject.GetComponentInParent<SteamLocomotive>();
			return _locomotive;
		}
	}

	public float DriverPhase { get; private set; }

	public void Configure(List<SteamLocomotiveDefinition.Wheelset> wheelsets, int mainDriverIndex, Animator animator, IDefinitionReferenceResolver referenceResolver)
	{
		CleanupPlayables();
		wheels = new WheelAnimation[wheelsets.Count];
		_playables = new PlayableHandle[wheelsets.Count];
		PlayableGraphAnimatorAdapter playableGraphAnimatorAdapter = animator.PlayableGraphAdapter();
		for (int i = 0; i < wheelsets.Count; i++)
		{
			SteamLocomotiveDefinition.Wheelset wheelset = wheelsets[i];
			WheelAnimation wheelAnimation = new WheelAnimation
			{
				diameter = wheelset.Diameter,
				isDriver = (i == mainDriverIndex),
				transform = referenceResolver.Resolve(wheelset.Transform),
				clip = referenceResolver.Resolve(wheelset.Animation)
			};
			if (wheelAnimation.clip != null)
			{
				_playables[i] = playableGraphAnimatorAdapter.AddPlayable(wheelAnimation.clip);
			}
			wheels[i] = wheelAnimation;
		}
		_wheelAudio = base.gameObject.AddComponent<WheelAudio>();
		float[] clackOffsets = GenerateWheelPositions();
		_wheelAudio.Configure(TrainController.Shared.wheelClackProfile, clackOffsets, Locomotive);
	}

	private void OnEnable()
	{
		_updateWheelTargetsCoroutine = StartCoroutine(UpdateWheelTransformTargets());
	}

	private void OnDisable()
	{
		if (_updateWheelTargetsCoroutine != null)
		{
			StopCoroutine(_updateWheelTargetsCoroutine);
		}
		_updateWheelTargetsCoroutine = null;
	}

	private void OnDestroy()
	{
		CleanupPlayables();
	}

	private void CleanupPlayables()
	{
		if (_playables != null)
		{
			PlayableHandle[] playables = _playables;
			for (int i = 0; i < playables.Length; i++)
			{
				playables[i]?.Dispose();
			}
			_playables = null;
		}
	}

	private void Update()
	{
		if (wheels == null)
		{
			return;
		}
		for (int i = 0; i < wheels.Length; i++)
		{
			WheelAnimation wheel = wheels[i];
			PlayableHandle playableHandle = _playables[i];
			if (playableHandle != null)
			{
				playableHandle.Time = wheel.Parameter * wheel.clip.length;
			}
			if (wheel.transform != null && wheel.TargetRotation.IsValid())
			{
				ApplyTargetPositionRotation(wheel);
			}
		}
	}

	private static void ApplyTargetPositionRotation(WheelAnimation wheel)
	{
		Vector3 localPosition;
		Quaternion localRotation;
		if (Vector3.SqrMagnitude(wheel.transform.localPosition - wheel.TargetPosition) > 25f)
		{
			localPosition = wheel.TargetPosition;
			localRotation = wheel.TargetRotation;
		}
		else
		{
			float t = Time.deltaTime * 10f;
			localPosition = Vector3.Lerp(wheel.transform.localPosition, wheel.TargetPosition, t);
			localRotation = Quaternion.Lerp(wheel.transform.localRotation, wheel.TargetRotation, t);
		}
		wheel.transform.localPosition = localPosition;
		wheel.transform.localRotation = localRotation;
	}

	private IEnumerator UpdateWheelTransformTargets()
	{
		WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();
		while (Locomotive.BodyTransform == null || wheels == null || wheels.Length == 0 || !Locomotive.LocationF.IsValid)
		{
			yield return waitForFixedUpdate;
		}
		PopulateWheelInitialValues();
		while (true)
		{
			UpdateWheelTransformTargetsActual();
			for (int i = 0; i < 10; i++)
			{
				yield return waitForFixedUpdate;
			}
		}
	}

	private void UpdateWheelTransformTargetsActual()
	{
		SteamLocomotive locomotive = Locomotive;
		if (locomotive == null || locomotive.BodyTransform == null)
		{
			return;
		}
		SteamLocomotiveDefinition locoDefinition = locomotive.LocoDefinition;
		for (int i = 0; i < wheels.Length; i++)
		{
			WheelAnimation wheelAnimation = wheels[i];
			if (!(wheelAnimation.transform == null) && i < locoDefinition.Wheelsets.Count)
			{
				SteamLocomotiveDefinition.Wheelset wheelset = locoDefinition.Wheelsets[i];
				CalculateWheelPositionRotation(locomotive, wheelAnimation, wheelset, out wheelAnimation.TargetPosition, out wheelAnimation.TargetRotation);
				wheels[i] = wheelAnimation;
			}
		}
	}

	private void PopulateWheelInitialValues()
	{
		Transform bodyTransform = Locomotive.BodyTransform;
		for (int i = 0; i < wheels.Length; i++)
		{
			WheelAnimation wheelAnimation = wheels[i];
			if (!(wheelAnimation.transform == null))
			{
				wheels[i].InitialPosition = wheelAnimation.transform.localPosition;
				wheels[i].InitialRotation = wheelAnimation.transform.localRotation;
				Transform parent = wheelAnimation.transform.parent;
				wheels[i].InitialPositionLocalToBody = bodyTransform.InverseTransformPoint(wheelAnimation.transform.position);
				wheels[i].InitialParentRotationLocalToBody = Quaternion.LookRotation(bodyTransform.InverseTransformDirection(parent.forward));
			}
		}
	}

	private static void CalculateWheelPositionRotation(SteamLocomotive car, WheelAnimation wheel, SteamLocomotiveDefinition.Wheelset wheelset, out Vector3 targetPosition, out Quaternion targetRotation)
	{
		SteamLocomotiveDefinition locoDefinition = car.LocoDefinition;
		Graph graph = TrainController.Shared.graph;
		(Car.MotionSnapshot, Location) motionSnapshotPositionFrontNonTransient = car.MotionSnapshotPositionFrontNonTransient;
		Car.MotionSnapshot motion = motionSnapshotPositionFrontNonTransient.Item1;
		Location item = motionSnapshotPositionFrontNonTransient.Item2;
		Location location = graph.LocationByMoving(item, wheelset.Offset - (locoDefinition.PositionHead - car.wheelInsetF), checkSwitchAgainstMovement: false, stopAtEndOfTrack: true);
		Graph.PositionRotation positionRotation = graph.GetPositionRotation(location, PositionAccuracy.High);
		Vector3 vector = CarInverseTransformPoint(WorldTransformer.GameToWorld(positionRotation.Position));
		Vector3 vector2 = wheelset.Offset * Vector3.forward - wheel.InitialPositionLocalToBody;
		Vector3 vector3 = Quaternion.Inverse(motion.Rotation) * positionRotation.Rotation * -vector2 + vector - wheel.InitialPositionLocalToBody;
		targetPosition = vector3 + wheel.InitialPositionLocalToBody;
		targetRotation = Quaternion.Inverse(motion.Rotation * wheel.InitialParentRotationLocalToBody) * positionRotation.Rotation * wheel.InitialRotation;
		Graph.PositionRotation positionRotation2 = car.TransformForDerailment(new Graph.PositionRotation(targetPosition, targetRotation), motion.Position, location);
		targetPosition = positionRotation2.Position;
		targetRotation = positionRotation2.Rotation;
		Vector3 CarInverseTransformPoint(Vector3 point)
		{
			return Quaternion.Inverse(motion.Rotation) * (point - motion.Position);
		}
	}

	public void ApplyDistanceMoved(MovementInfo info, float driverVelocity, float absReverser, float absThrottle, float driverPhase)
	{
		if (wheels.Length == 0)
		{
			return;
		}
		float velocity = Locomotive.velocity;
		for (int i = 0; i < wheels.Length; i++)
		{
			WheelAnimation wheelAnimation = wheels[i];
			float num = wheelAnimation.diameter * MathF.PI;
			if (num != 0f)
			{
				float num2 = (wheelAnimation.isDriver ? driverVelocity : velocity);
				float num3 = info.DeltaTime * num2 / num;
				wheels[i].Parameter = Mathf.Repeat(wheelAnimation.Parameter + num3, 1f);
				if (wheels[i].isDriver)
				{
					DriverPhase = wheels[i].Parameter;
				}
			}
		}
		_wheelAudio.Roll(info.Distance * Mathf.Sign(velocity), velocity);
	}

	private float[] GenerateWheelPositions()
	{
		SteamLocomotiveDefinition locoDefinition = Locomotive.LocoDefinition;
		float[] array = new float[locoDefinition.Wheelsets.Sum((SteamLocomotiveDefinition.Wheelset ws) => ws.NumberOfAxles)];
		int num = 0;
		foreach (SteamLocomotiveDefinition.Wheelset wheelset in locoDefinition.Wheelsets)
		{
			float num2 = 0f - wheelset.Offset - wheelset.Length / 2f;
			int numberOfAxles = wheelset.NumberOfAxles;
			if (numberOfAxles <= 0)
			{
				continue;
			}
			if (numberOfAxles == 1)
			{
				array[num++] = num2;
				continue;
			}
			float num3 = wheelset.Length / (float)(wheelset.NumberOfAxles - 1);
			for (int num4 = 0; num4 < wheelset.NumberOfAxles; num4++)
			{
				array[num++] = num2;
				num2 += num3;
			}
		}
		return array;
	}

	public void SetLinearOffset(float linearOffset)
	{
		_wheelAudio.LinearOffset = linearOffset;
	}
}
