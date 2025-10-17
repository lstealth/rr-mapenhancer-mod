using System.Collections;
using System.Collections.Generic;
using Core;
using Helpers.Culling;
using Map.Runtime.MaskComponents;
using Serilog;
using Track;
using UnityEngine;

namespace Helpers;

[RequireComponent(typeof(RiverPath))]
public class RiverBuilder : MonoBehaviour, CullingManager.ICullingEventHandler
{
	[SerializeField]
	private SplineProfile splineProfile;

	private RiverPath _riverPath;

	private readonly List<RamSpline> _splines = new List<RamSpline>();

	private Vector3 _boundsCenterLocal;

	private float _boundsRadius;

	private CullingManager.Token _cullingToken;

	private CoroutineTask _generateTask;

	private static CullingManager CullingManager => CullingManager.Scenery;

	private void OnEnable()
	{
		_riverPath = GetComponent<RiverPath>();
		UpdateBounds();
		_cullingToken = CullingManager.AddSphere(base.transform.position + _boundsCenterLocal, _boundsRadius, this);
	}

	private void OnDisable()
	{
		_cullingToken?.Dispose();
		_cullingToken = null;
		_generateTask?.Stop();
		_generateTask = null;
	}

	private void UpdateBounds()
	{
		Bounds bounds = default(Bounds);
		float num = 0f;
		foreach (var item4 in EnumerateCurves())
		{
			BezierCurve item = item4.curve;
			float item2 = item4.width0;
			float item3 = item4.width1;
			Bounds bounds2 = item.GetBounds();
			if (bounds.size == Vector3.zero)
			{
				bounds = bounds2;
			}
			else
			{
				bounds.Encapsulate(bounds2);
			}
			num = Mathf.Max(num, Mathf.Max(item2, item3));
		}
		_boundsCenterLocal = bounds.center;
		_boundsRadius = bounds.extents.magnitude + num;
	}

	[ContextMenu("Build River")]
	public void BuildSpline()
	{
		_riverPath = GetComponent<RiverPath>();
		Log.Debug("Building {name} with {count} splines", base.name, _riverPath.points.Count - 1);
		while (base.transform.childCount > 0)
		{
			Object.DestroyImmediate(base.transform.GetChild(0).gameObject);
		}
		_generateTask?.Stop();
		_splines.Clear();
		RamSpline beginningSpline = null;
		foreach (var item4 in EnumerateCurves())
		{
			BezierCurve item = item4.curve;
			float item2 = item4.width0;
			float item3 = item4.width1;
			RamSpline ramSpline = CreateNextSpline(item, item2, item3);
			ramSpline.beginningSpline = beginningSpline;
			ramSpline.beginningMinWidth = 0f;
			ramSpline.beginningMaxWidth = 1f;
			_splines.Add(ramSpline);
			beginningSpline = ramSpline;
		}
		_generateTask = CoroutineTask.Start(GenerateCoroutine(), this);
	}

	private IEnumerable<(BezierCurve curve, float width0, float width1)> EnumerateCurves()
	{
		if (!(_riverPath == null))
		{
			List<RiverPath.Point> points = _riverPath.points;
			for (int i = 1; i < points.Count; i++)
			{
				BezierCurve item = _riverPath.MakeCurve(i).OffsetBy(-base.transform.position);
				yield return (curve: item, width0: points[i - 1].width, width1: points[i].width);
			}
		}
	}

	private IEnumerator GenerateCoroutine()
	{
		foreach (RamSpline spline in _splines)
		{
			yield return null;
			spline.ResetToProfile();
		}
	}

	private RamSpline CreateNextSpline(BezierCurve curve, float width0, float width1)
	{
		Vector3 vector = Vector3.Lerp(curve.EndPoint1, curve.EndPoint2, 0.5f);
		GameObject gameObject = new GameObject("Spline")
		{
			hideFlags = HideFlags.HideAndDontSave,
			tag = "TrackMeshGenerated"
		};
		gameObject.SetActive(value: false);
		gameObject.transform.SetParent(base.transform, worldPositionStays: false);
		gameObject.transform.localPosition = vector;
		RamSpline ramSpline = gameObject.AddComponent<RamSpline>();
		ramSpline.currentProfile = splineProfile;
		gameObject.AddComponent<MeshRenderer>().sharedMaterial = splineProfile.splineMaterial;
		List<LinePoint> list = curve.Approximate(1.0001f, 3f, 16, 50f);
		float num = curve.CalculateLength();
		float num2 = 0f;
		for (int i = 0; i < list.Count; i++)
		{
			LinePoint linePoint = list[i];
			num2 += ((i > 0) ? Vector3.Distance(list[i - 1].point, linePoint.point) : 0f);
			float t = num2 / num;
			Vector3 vector2 = linePoint.point - vector;
			Quaternion rotation = linePoint.Rotation;
			float w = Mathf.Lerp(width0, width1, t);
			ramSpline.AddPoint(new Vector4(vector2.x, vector2.y, vector2.z, w));
			List<Quaternion> controlPointsRotations = ramSpline.controlPointsRotations;
			controlPointsRotations[controlPointsRotations.Count - 1] = Quaternion.Euler(rotation.eulerAngles.x * 4f, 0f, rotation.eulerAngles.z);
		}
		RendererCuller rendererCuller = gameObject.AddComponent<RendererCuller>();
		rendererCuller.radius = Vector3.Distance(vector, curve.EndPoint1) + width0 + width1;
		rendererCuller.visibleDistanceBand = 2;
		gameObject.SetActive(value: true);
		return ramSpline;
	}

	public void CullingSphereStateChanged(bool isVisible, int distanceBand)
	{
		if (_splines.Count == 0 && (isVisible || distanceBand <= 1))
		{
			BuildSpline();
		}
	}

	public void RequestUpdateCullingPosition()
	{
		UpdateCulling();
	}

	private void UpdateCulling()
	{
		_cullingToken?.UpdatePosition(base.transform.position + _boundsCenterLocal, _boundsRadius);
	}
}
