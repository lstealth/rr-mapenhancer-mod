using System.Collections.Generic;
using Core;
using Helpers;
using Helpers.Culling;
using UnityEngine;

namespace BezierCurveMesh;

public abstract class CurveMeshBuilderBase : MonoBehaviour, CullingManager.ICullingEventHandler
{
	[SerializeField]
	protected Transform meshContainer;

	[SerializeField]
	protected CurveMeshProfile meshProfile;

	private Renderer[] _renderers;

	private CullingManager.Token _cullingToken;

	private Vector3 _boundsCenterLocal;

	private float _boundsRadius;

	protected virtual bool ZeroMeshRotation => false;

	private void OnEnable()
	{
		CullingManager scenery = CullingManager.Scenery;
		_cullingToken = scenery.AddSphere(base.transform.position + _boundsCenterLocal, _boundsRadius, this);
	}

	private void OnDisable()
	{
		_cullingToken?.Dispose();
		_cullingToken = null;
	}

	protected abstract List<BezierCurve> GetCurves();

	[ContextMenu("Generate Mesh")]
	public void Generate()
	{
		List<BezierCurve> curves = GetCurves();
		Generate(curves);
	}

	private void Generate(List<BezierCurve> bezierCurves)
	{
		if (meshContainer == null)
		{
			Debug.LogError("Mesh container is not set");
			return;
		}
		meshContainer.DestroyAllChildren();
		_renderers = null;
		if (bezierCurves.Count == 0)
		{
			return;
		}
		if (meshProfile == null)
		{
			Debug.LogError("Mesh profile is not set");
			return;
		}
		if (meshProfile.meshFilter == null)
		{
			Debug.LogError("Prefab mesh filter is not set");
			return;
		}
		MeshFilter meshFilter = meshProfile.meshFilter;
		Vector3 vector = base.transform.GamePosition();
		for (int i = 0; i < bezierCurves.Count; i++)
		{
			BezierCurve value = bezierCurves[i].OffsetBy(-vector);
			bezierCurves[i] = value;
		}
		_ = Time.realtimeSinceStartupAsDouble;
		Mesh mesh = CurveMeshGenerator.GenerateMeshAlongCurves(meshFilter.sharedMesh, bezierCurves, meshProfile.forwardAxis, meshProfile.vertexScale);
		_ = Time.realtimeSinceStartupAsDouble;
		mesh.name = base.name;
		mesh.hideFlags = HideFlags.DontSave;
		GameObject gameObject = new GameObject("Curve Mesh");
		gameObject.hideFlags = HideFlags.DontSave;
		gameObject.transform.SetParent(meshContainer, worldPositionStays: false);
		gameObject.AddComponent<MeshFilter>().mesh = mesh;
		gameObject.AddComponent<MeshRenderer>().sharedMaterial = meshFilter.GetComponent<MeshRenderer>()?.sharedMaterial;
		if (ZeroMeshRotation)
		{
			gameObject.transform.rotation = Quaternion.identity;
		}
		_renderers = gameObject.GetComponentsInChildren<Renderer>();
		_boundsCenterLocal = mesh.bounds.center;
		_boundsRadius = mesh.bounds.extents.magnitude;
	}

	public void CullingSphereStateChanged(bool isVisible, int distanceBand)
	{
		if (_renderers == null && (isVisible || distanceBand <= 1))
		{
			Generate();
		}
		if (_renderers != null)
		{
			Renderer[] renderers = _renderers;
			for (int i = 0; i < renderers.Length; i++)
			{
				renderers[i].enabled = isVisible;
			}
		}
	}

	public void RequestUpdateCullingPosition()
	{
		_cullingToken?.UpdatePosition(base.transform.position + _boundsCenterLocal, _boundsRadius);
	}
}
