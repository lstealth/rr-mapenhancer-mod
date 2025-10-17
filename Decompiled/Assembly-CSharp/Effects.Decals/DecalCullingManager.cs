using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Helpers;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Effects.Decals;

public class DecalCullingManager : MonoBehaviour
{
	private class Entry
	{
		public DecalProjector DecalProjector;

		public bool? Visible;

		public Action<bool> VisibleDidChange;
	}

	[BurstCompile]
	private struct DecalCullingJob : IJobParallelFor
	{
		[ReadOnly]
		public NativeArray<float3> DecalPositions;

		[ReadOnly]
		public NativeArray<float3> DecalForwards;

		[ReadOnly]
		public NativeArray<float3> DecalSizes;

		[ReadOnly]
		public float3 CameraPosition;

		[ReadOnly]
		public float CullDistance;

		[ReadOnly]
		public NativeArray<float4> FrustumPlanes;

		[ReadOnly]
		public float4x4 ProjectionMatrix;

		[ReadOnly]
		public float4x4 WorldToViewMatrix;

		[ReadOnly]
		public float MinScreenSize;

		[ReadOnly]
		public float ScreenHeight;

		public NativeArray<bool> DecalVisibility;

		public void Execute(int index)
		{
			float3 @float = DecalPositions[index];
			if (math.distance(@float, CameraPosition) > CullDistance)
			{
				DecalVisibility[index] = false;
				return;
			}
			if (!IsInFrustum(new AABB(@float - DecalSizes[index] * 0.5f, @float + DecalSizes[index] * 0.5f), FrustumPlanes))
			{
				DecalVisibility[index] = false;
				return;
			}
			float3 xyz = math.mul(WorldToViewMatrix, new float4(@float, 1f)).xyz;
			float4 float2 = math.mul(ProjectionMatrix, new float4(xyz, 1f));
			float num = math.abs(DecalSizes[index].xy / float2.w).y * ScreenHeight * 0.5f;
			DecalVisibility[index] = num > MinScreenSize;
		}

		private static bool IsInFrustum(AABB bounds, NativeArray<float4> planes)
		{
			for (int i = 0; i < 6; i++)
			{
				float4 @float = planes[i];
				float3 xyz = @float.xyz;
				float3 min = bounds.min;
				if (xyz.x >= 0f)
				{
					min.x = bounds.max.x;
				}
				if (xyz.y >= 0f)
				{
					min.y = bounds.max.y;
				}
				if (xyz.z >= 0f)
				{
					min.z = bounds.max.z;
				}
				if (math.dot(xyz, min) + @float.w < 0f)
				{
					return false;
				}
			}
			return true;
		}
	}

	public struct AABB
	{
		public float3 min;

		public float3 max;

		public float3 Center => (max + min) * 0.5f;

		public float3 Size => max - min;

		public AABB(float3 min, float3 max)
		{
			this.min = min;
			this.max = max;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Contains(float3 point)
		{
			if (point.x >= min.x && point.x <= max.x && point.y >= min.y && point.y <= max.y && point.z >= min.z)
			{
				return point.z <= max.z;
			}
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Intersects(AABB other)
		{
			if (max.x >= other.min.x && min.x <= other.max.x && max.y >= other.min.y && min.y <= other.max.y && max.z >= other.min.z)
			{
				return min.z <= other.max.z;
			}
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float3 ClosestPoint(float3 point)
		{
			return math.clamp(point, min, max);
		}
	}

	[Header("Culling Settings")]
	[SerializeField]
	private float cullDistance = 600f;

	[SerializeField]
	private float updateInterval = 0.25f;

	[SerializeField]
	private float screenSizeThresholdHighQuality = 1f;

	[SerializeField]
	private float screenSizeThresholdLowQuality = 2.5f;

	[SerializeField]
	private float frustumScale = 0.7f;

	[SerializeField]
	private int decalBudget = 200;

	[SerializeField]
	private float cameraVelocityThreshold = 1f;

	[SerializeField]
	private float cameraAngularVelocityThreshold = 1f;

	private Camera _mainCamera;

	private readonly List<Entry> _decalProjectors = new List<Entry>();

	private float _timeSinceLastUpdate;

	private bool _updateFast;

	private int _visibleCount;

	private float _screenSizeThreshold;

	private Plane[] _frustumPlanes = new Plane[6];

	private Vector3 _lastCameraPosition;

	private Vector3 _lastCameraEulerAngles;

	private static DecalCullingManager _shared;

	public static DecalCullingManager Shared
	{
		get
		{
			if (_shared == null)
			{
				_shared = new GameObject("Decal Culling Manager")
				{
					hideFlags = HideFlags.DontSave
				}.AddComponent<DecalCullingManager>();
			}
			return _shared;
		}
	}

	private void OnEnable()
	{
		_screenSizeThreshold = screenSizeThresholdLowQuality;
	}

	private void Update()
	{
		_timeSinceLastUpdate += Time.deltaTime;
		float num = (_updateFast ? (updateInterval * 0.5f) : updateInterval);
		if (_timeSinceLastUpdate >= num && MainCameraHelper.TryGetIfNeeded(ref _mainCamera))
		{
			UpdateDecalVisibilityJob();
			_timeSinceLastUpdate = 0f;
		}
	}

	private void FixedUpdate()
	{
		if (MainCameraHelper.TryGetIfNeeded(ref _mainCamera))
		{
			float deltaTime = Time.deltaTime;
			Transform obj = _mainCamera.transform;
			Vector3 eulerAngles = obj.eulerAngles;
			Vector3 vector = MathF.PI / 180f * (eulerAngles - _lastCameraEulerAngles) / deltaTime;
			_lastCameraEulerAngles = eulerAngles;
			Vector3 vector2 = obj.GamePosition();
			Vector3 vector3 = (vector2 - _lastCameraPosition) / deltaTime;
			_lastCameraPosition = vector2;
			_updateFast = vector3.sqrMagnitude > cameraVelocityThreshold * cameraVelocityThreshold || vector.sqrMagnitude > cameraAngularVelocityThreshold * cameraAngularVelocityThreshold;
		}
	}

	private void RequestUpdateNextFrame()
	{
		_timeSinceLastUpdate = updateInterval;
	}

	private void UpdateDecalVisibilityJob()
	{
		int count = _decalProjectors.Count;
		if (count == 0)
		{
			return;
		}
		NativeArray<float3> decalPositions = new NativeArray<float3>(count, Allocator.TempJob);
		NativeArray<float3> decalForwards = new NativeArray<float3>(count, Allocator.TempJob);
		NativeArray<float3> decalSizes = new NativeArray<float3>(count, Allocator.TempJob);
		NativeArray<bool> decalVisibility = new NativeArray<bool>(count, Allocator.TempJob);
		GeometryUtility.CalculateFrustumPlanes(_mainCamera, _frustumPlanes);
		NativeArray<float4> nativeArray = new NativeArray<float4>(6, Allocator.TempJob);
		for (int i = 0; i < count; i++)
		{
			DecalProjector decalProjector = _decalProjectors[i].DecalProjector;
			decalPositions[i] = decalProjector.transform.position;
			decalForwards[i] = decalProjector.transform.forward;
			decalSizes[i] = decalProjector.size;
		}
		Transform transform = _mainCamera.transform;
		Vector3 forward = transform.forward;
		Vector3 position = transform.position;
		Plane[] frustumPlanes = _frustumPlanes;
		CalculateNativeFrustumPlanes(forward, frustumPlanes, position, nativeArray, frustumScale);
		IJobParallelForExtensions.Schedule(new DecalCullingJob
		{
			DecalPositions = decalPositions,
			DecalForwards = decalForwards,
			DecalSizes = decalSizes,
			CameraPosition = transform.position,
			CullDistance = cullDistance,
			FrustumPlanes = nativeArray,
			DecalVisibility = decalVisibility,
			ProjectionMatrix = _mainCamera.projectionMatrix,
			WorldToViewMatrix = _mainCamera.worldToCameraMatrix,
			MinScreenSize = _screenSizeThreshold,
			ScreenHeight = Screen.height
		}, count, 32).Complete();
		int num = 0;
		for (int j = 0; j < count; j++)
		{
			Entry entry = _decalProjectors[j];
			bool flag = decalVisibility[j];
			if (flag != entry.Visible)
			{
				entry.Visible = flag;
				entry.VisibleDidChange?.Invoke(flag);
			}
			if (flag)
			{
				num++;
			}
		}
		_visibleCount = num;
		decalPositions.Dispose();
		decalForwards.Dispose();
		decalSizes.Dispose();
		decalVisibility.Dispose();
		nativeArray.Dispose();
		UpdateScreenSizeThreshold(_visibleCount);
	}

	private void UpdateScreenSizeThreshold(int visibleCount)
	{
		float value;
		if ((float)visibleCount > (float)decalBudget * 1.5f)
		{
			value = Mathf.Lerp(_screenSizeThreshold, screenSizeThresholdLowQuality, 0.5f);
		}
		else if ((float)visibleCount > (float)decalBudget * 1f)
		{
			value = Mathf.Lerp(_screenSizeThreshold, screenSizeThresholdLowQuality, 0.1f);
		}
		else if ((float)visibleCount < (float)decalBudget * 0.5f)
		{
			value = Mathf.Lerp(_screenSizeThreshold, screenSizeThresholdHighQuality, 0.5f);
		}
		else
		{
			if (!((float)visibleCount < (float)decalBudget * 0.75f))
			{
				return;
			}
			value = Mathf.Lerp(_screenSizeThreshold, screenSizeThresholdHighQuality, 0.1f);
		}
		_screenSizeThreshold = Mathf.Clamp(value, screenSizeThresholdHighQuality, screenSizeThresholdLowQuality);
	}

	[BurstCompile]
	private static void CalculateNativeFrustumPlanes(Vector3 cameraForward, Plane[] frustumPlanes, Vector3 cameraPosition, NativeArray<float4> nativePlanes, float scale)
	{
		float3 @float = new float3(cameraForward.x, cameraForward.y, cameraForward.z);
		for (int i = 0; i < 6; i++)
		{
			Vector3 normal = frustumPlanes[i].normal;
			float3 float2 = new float3(normal.x, normal.y, normal.z);
			if (i < 4)
			{
				float3 float3 = math.dot(float2, @float) * @float;
				float3 float4 = float2 - float3;
				float3 x = float3 + float4 * scale;
				x = math.normalize(x);
				float w = 0f - math.dot(x, new float3(cameraPosition.x, cameraPosition.y, cameraPosition.z));
				nativePlanes[i] = new float4(x.x, x.y, x.z, w);
			}
			else
			{
				nativePlanes[i] = new float4(normal.x, normal.y, normal.z, frustumPlanes[i].distance);
			}
		}
	}

	public void RegisterDecal(DecalProjector decal, Action<bool> onVisibilityDidChange)
	{
		Entry item = new Entry
		{
			DecalProjector = decal,
			Visible = null,
			VisibleDidChange = onVisibilityDidChange
		};
		_decalProjectors.Add(item);
		RequestUpdateNextFrame();
	}

	public void UnregisterDecal(DecalProjector decal)
	{
		for (int i = 0; i < _decalProjectors.Count; i++)
		{
			if (_decalProjectors[i].DecalProjector == decal)
			{
				_decalProjectors.RemoveAtSwapBack(i);
				break;
			}
		}
	}
}
