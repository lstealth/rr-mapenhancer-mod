using System;
using System.Collections;
using System.Collections.Generic;
using GalaSoft.MvvmLight.Messaging;
using Game.Events;
using Serilog;
using UnityEngine;

namespace Helpers;

[DefaultExecutionOrder(1000)]
public class WorldTransformer : MonoBehaviour
{
	private static WorldTransformer _shared = null;

	[Tooltip("Frequency distance of world position restarting, distance in is grid elements.")]
	public Vector2Int tileRange = new Vector2Int(3, 3);

	private const float MoveDelay = 1f;

	private Vector2Int currentTile;

	private static Vector3 _currentOffset = Vector3.zero;

	public Vector2 tileSize = new Vector2(500f, 500f);

	private bool waitForMover;

	private Vector2Int? _pendingMove;

	private Coroutine _checkCoroutine;

	private Coroutine _scheduledMoveCoroutine;

	private static WorldTransformer Shared
	{
		get
		{
			if (_shared == null)
			{
				_shared = UnityEngine.Object.FindObjectOfType<WorldTransformer>();
			}
			return _shared;
		}
	}

	private static HashSet<Transform> ObjectsToMove => WorldTransformerTargetList.Targets;

	public event Action<Vector3> OnDidMove;

	public static bool TryGetShared(out WorldTransformer shared)
	{
		shared = Shared;
		return shared != null;
	}

	public void OnEnable()
	{
		_currentOffset = Vector3.zero;
		_checkCoroutine = StartCoroutine(CheckForChangeCoroutine());
	}

	private void OnDisable()
	{
		if (_checkCoroutine != null)
		{
			StopCoroutine(_checkCoroutine);
		}
		_checkCoroutine = null;
	}

	private void FixedUpdate()
	{
		if (_pendingMove.HasValue)
		{
			Vector2Int value = _pendingMove.Value;
			_pendingMove = null;
			PerformMove(value);
			waitForMover = false;
		}
	}

	private void OnDestroy()
	{
		_currentOffset = Vector3.zero;
	}

	[ContextMenu("Move Now")]
	public void MoveNow()
	{
		CancelMove();
		Vector2Int vector2Int = CurrentTarget();
		if (vector2Int != currentTile)
		{
			PerformMove(vector2Int);
		}
	}

	private IEnumerator CheckForChangeCoroutine()
	{
		while (true)
		{
			Vector2Int target = CurrentTarget();
			MoveWorldIfNeeded(target);
			yield return new WaitForSeconds(1f);
		}
	}

	private Vector2Int CurrentTarget()
	{
		Vector3 currentCameraPosition = CameraSelector.shared.CurrentCameraPosition;
		return TilePosition(currentCameraPosition);
	}

	private Vector2Int TilePosition(Vector3 pos)
	{
		if (tileSize.x == 0f || tileSize.y == 0f)
		{
			return default(Vector2Int);
		}
		return new Vector2Int(Mathf.FloorToInt(pos.x / tileSize.x), Mathf.FloorToInt(pos.z / tileSize.y));
	}

	private void MoveWorldIfNeeded(Vector2Int target)
	{
		if (!waitForMover)
		{
			Vector2Int vector2Int = target - currentTile;
			if (Mathf.Abs(vector2Int.x) > tileRange.x || Mathf.Abs(vector2Int.y) > tileRange.y)
			{
				waitForMover = true;
				_scheduledMoveCoroutine = StartCoroutine(ScheduleMoveWorldDelayed(target));
			}
		}
	}

	private IEnumerator ScheduleMoveWorldDelayed(Vector2Int target)
	{
		Log.Debug("PreMoveWorld {target}", target);
		yield return new WaitForSeconds(1f);
		_pendingMove = target;
		_scheduledMoveCoroutine = null;
	}

	private void CancelMove()
	{
		if (_scheduledMoveCoroutine != null)
		{
			StopCoroutine(_scheduledMoveCoroutine);
			_scheduledMoveCoroutine = null;
		}
		_pendingMove = null;
		waitForMover = false;
	}

	private void PerformMove(Vector2Int target)
	{
		Log.Information("MoveWorld {target}", target);
		Vector2Int vector2Int = target - currentTile;
		Vector3 vector = new Vector3((float)(-vector2Int.x) * tileSize.x, 0f, (float)(-vector2Int.y) * tileSize.y);
		_currentOffset += vector;
		foreach (Transform item in ObjectsToMove)
		{
			if (!(item == null))
			{
				item.position += vector;
			}
		}
		currentTile = target;
		TranslateParticleSystems(vector);
		Log.Debug("WorldTransformer OnDidMove({offset})", vector);
		this.OnDidMove?.Invoke(vector);
		Messenger.Default.Send(new WorldDidMoveEvent(vector));
	}

	private static void TranslateParticleSystems(Vector3 offset)
	{
		ParticleSystem.Particle[] array = null;
		ParticleSystem[] array2 = UnityEngine.Object.FindObjectsOfType<ParticleSystem>();
		foreach (ParticleSystem particleSystem in array2)
		{
			if (particleSystem.main.simulationSpace != ParticleSystemSimulationSpace.World)
			{
				continue;
			}
			int maxParticles = particleSystem.main.maxParticles;
			if (maxParticles > 0)
			{
				bool isPaused = particleSystem.isPaused;
				bool isPlaying = particleSystem.isPlaying;
				if (!isPaused)
				{
					particleSystem.Pause();
				}
				if (array == null || array.Length < maxParticles)
				{
					array = new ParticleSystem.Particle[maxParticles];
				}
				int particles = particleSystem.GetParticles(array);
				for (int j = 0; j < particles; j++)
				{
					array[j].position += offset;
				}
				particleSystem.SetParticles(array, particles);
				if (isPlaying)
				{
					particleSystem.Play();
				}
			}
		}
	}

	private void MoveObject(Transform objectTransform)
	{
		objectTransform.position += _currentOffset;
	}

	public void AddObjectToMove(Transform objectToMove)
	{
		MoveObject(objectToMove);
		ObjectsToMove.Add(objectToMove);
	}

	public void RemoveObjectToMove(Transform transform1)
	{
		ObjectsToMove.Remove(transform1);
	}

	public static Vector3 GameToWorld(Vector3 v)
	{
		return _currentOffset + v;
	}

	public static Vector3 WorldToGame(Vector3 worldPosition)
	{
		return worldPosition - _currentOffset;
	}
}
