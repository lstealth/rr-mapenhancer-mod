using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core;
using Helpers;
using Helpers.Culling;
using Track;
using UnityEngine;

namespace AutoTrestle;

public class AutoTrestle : MonoBehaviour, CullingManager.ICullingEventHandler
{
	[Serializable]
	public class ControlPoint
	{
		public Vector3 position;

		public Quaternion rotation;

		public Vector3 Transform(Vector3 point)
		{
			return rotation * point + position;
		}
	}

	public enum EndStyle
	{
		Block,
		Bent
	}

	private struct GirtPoint
	{
		public Vector3 point;

		public Vector3 outVector;
	}

	private const string TagGenerated = "AutoTrestleGenerated";

	public AutoTrestleProfile profile;

	[SerializeField]
	public List<ControlPoint> controlPoints = new List<ControlPoint>();

	public EndStyle headStyle;

	public EndStyle tailStyle;

	private const HideFlags _hideFlags = HideFlags.HideAndDontSave;

	private CullingManager.Token _cullingToken;

	private CoroutineTask _generateTask;

	private bool _hasGenerated;

	private readonly List<MeshRenderer> _meshRenderers = new List<MeshRenderer>();

	private const float inToM = 0.025400052f;

	private const float ftToM = 0.30480063f;

	private static readonly Vector3 dimCap = new Vector3(13f, 14f) * 0.025400052f;

	private static readonly Vector3 dimPost = new Vector3(12f, 12f) * 0.025400052f;

	private static readonly Vector3 dimGirt = new Vector3(6f, 8f) * 0.025400052f;

	private static readonly Vector3 dimStringer = new Vector3(21f, 16f) * 0.025400052f;

	private void OnEnable()
	{
		if (!Application.isPlaying)
		{
			GenerateIfNeeded();
			return;
		}
		Vector3 position = controlPoints[0].position;
		List<ControlPoint> list = controlPoints;
		float radius = Vector3.Distance(position, list[list.Count - 1].position);
		_cullingToken = CullingManager.Bridge.AddSphere(base.transform, radius, this);
	}

	private void OnDisable()
	{
		_cullingToken?.Dispose();
		_cullingToken = null;
		_generateTask?.Stop();
		_generateTask = null;
		DestroyGenerated();
	}

	private void OnDrawGizmos()
	{
		foreach (ControlPoint controlPoint in controlPoints)
		{
			Gizmos.DrawCube(base.transform.TransformPoint(controlPoint.position) + Vector3.down * 10f / 2f, new Vector3(0.3f, 10f, 0.3f));
		}
	}

	public void RemovePoint(int controlPointToDelete)
	{
		controlPoints.RemoveAt(controlPointToDelete);
	}

	public void AddPoint(Vector4 position)
	{
		controlPoints.Add(new ControlPoint
		{
			position = position,
			rotation = Quaternion.identity
		});
	}

	public void AddPointAfter(int idMin, Vector3 position)
	{
		controlPoints.Insert(idMin + 1, new ControlPoint
		{
			position = position,
			rotation = Quaternion.identity
		});
	}

	private IEnumerator GenerateCoroutine()
	{
		while (!CheckForTerrain())
		{
			yield return new WaitForSecondsRealtime(1f);
		}
		Generate();
		bool CheckForTerrain()
		{
			if (controlPoints.Count < 2)
			{
				return false;
			}
			Vector3 vector = Vector3.up * 10f;
			Vector3 origin = base.transform.TransformPoint(controlPoints[0].position) + vector;
			Transform obj = base.transform;
			List<ControlPoint> list = controlPoints;
			Vector3 origin2 = obj.TransformPoint(list[list.Count - 1].position) + vector;
			if (Raycast(origin, Vector3.down, out var hit))
			{
				return Raycast(origin2, Vector3.down, out hit);
			}
			return false;
		}
	}

	private static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hit, float maxDistance = 50f)
	{
		int layerMask = 1 << Layers.Terrain;
		return Physics.Raycast(origin, direction, out hit, maxDistance, layerMask);
	}

	[ContextMenu("Generate Trestle")]
	public void Generate()
	{
		double realtimeSinceStartupAsDouble = Time.realtimeSinceStartupAsDouble;
		DestroyGenerated();
		BezierCurve[] array = new BezierCurve[controlPoints.Count - 1];
		for (int i = 0; i < controlPoints.Count - 1; i++)
		{
			ControlPoint a = controlPoints[i];
			ControlPoint b = controlPoints[i + 1];
			array[i] = MakeCurve(a, b);
		}
		LineCurve lineCurve = new LineCurve(array[0].Approximate(), Hand.Left);
		float bentSpacing = CalculateBentSpacing(lineCurve.Length);
		List<Vector4> bentPoints = CalculateBentPoints(lineCurve, bentSpacing);
		GenerateStringers(bentPoints, lineCurve);
		GenerateBents(bentPoints, bentSpacing, out var girtPoints);
		GenerateGirtsAndBraces(bentPoints, girtPoints, 2f);
		double num = Time.realtimeSinceStartupAsDouble - realtimeSinceStartupAsDouble;
		Debug.Log($"Generated trestle in {num:F3}");
		_hasGenerated = true;
	}

	private static float CalculateBentSpacing(float length)
	{
		length -= 2f;
		int num = Mathf.RoundToInt(length / 3.6576076f);
		if (num <= 1)
		{
			return 3.6576076f;
		}
		if (num % 2 == 1)
		{
			num++;
		}
		return length / (float)num;
	}

	private void DestroyGenerated()
	{
		foreach (GameObject item in base.transform.FindObjectsWithTag("AutoTrestleGenerated"))
		{
			UnityEngine.Object.DestroyImmediate(item);
		}
		_meshRenderers.Clear();
		_hasGenerated = false;
	}

	private float[] GenerateBents(List<Vector4> bentPoints, float bentSpacing, out GirtPoint[,,] girtPoints)
	{
		Vector3 aboveTrack = Vector3.up;
		bool warnedNoHit = false;
		float[] array = bentPoints.Select(delegate(Vector4 vector)
		{
			Quaternion quaternion = Quaternion.Euler(0f, vector.w, 0f);
			Vector3 vector2 = (Vector3)vector + base.transform.position;
			Vector3 vector3 = 2f * Vector3.right;
			if (Raycast(aboveTrack + vector2 + quaternion * vector3, Vector3.down, out var hit) && Raycast(aboveTrack + vector2 - quaternion * vector3, Vector3.down, out var hit2))
			{
				return Mathf.Min(hit.distance, hit2.distance) - aboveTrack.magnitude;
			}
			if (!warnedNoHit)
			{
				Debug.LogWarning($"Trestle {this}: No raycast hit for bent - below grade?", this);
				warnedNoHit = true;
			}
			return 10f;
		}).ToArray();
		float bentHeight = array.Max();
		float[] array2 = CalculateGirtHeights(bentPoints, bentHeight, bentSpacing * 0.8f);
		girtPoints = new GirtPoint[2, bentPoints.Count, array2.Length + 1];
		for (int num = 0; num < bentPoints.Count; num++)
		{
			Vector4 bentPoint = bentPoints[num];
			float[] array3 = new float[array2.Length + 1];
			array3[0] = bentPoint.y - dimCap.y;
			Array.Copy(array2, 0, array3, 1, array2.Length);
			MakeBent(bentPoint, array[num], array3, out var girtPoints2);
			for (int num2 = 0; num2 < 2; num2++)
			{
				for (int num3 = 0; num3 < array2.Length + 1; num3++)
				{
					girtPoints[num2, num, num3] = girtPoints2[num2, num3];
				}
			}
		}
		return array2;
	}

	private void GenerateGirtsAndBraces(List<Vector4> bentPoints, GirtPoint[,,] girtPoints, float minGirtSpacing)
	{
		Vector3 girtPostOffset = (dimPost.x / 2f + dimGirt.x / 2f) * Vector3.forward;
		int length = girtPoints.GetLength(2);
		for (int i = 0; i < 2; i++)
		{
			for (int j = 0; j < bentPoints.Count - 1; j++)
			{
				for (int k = 1; k < length; k++)
				{
					GirtPoint gp = girtPoints[i, j, k];
					GirtPoint gp2 = girtPoints[i, j + 1, k];
					if (!gp.outVector.IsZero() && !gp2.outVector.IsZero())
					{
						MakeBeamBetween(dimGirt, FacePoint(gp, 1f, dimGirt.y), gp.outVector, FacePoint(gp2, 1f, dimGirt.y), gp2.outVector, "Brace");
					}
				}
			}
		}
		for (int l = 0; l < 2; l++)
		{
			for (int m = 0; m < bentPoints.Count - 1; m++)
			{
				for (int n = 0; n < length - 1; n++)
				{
					GirtPoint gp3 = girtPoints[l, m, n];
					GirtPoint gp4 = girtPoints[l, m + 1, n];
					GirtPoint gp5 = girtPoints[l, m, n + 1];
					GirtPoint gp6 = girtPoints[l, m + 1, n + 1];
					if (!((gp3.point - gp4.point).magnitude < minGirtSpacing) && !gp3.outVector.IsZero() && !gp5.outVector.IsZero() && !gp4.outVector.IsZero() && !gp6.outVector.IsZero())
					{
						float v = dimGirt.y * 2f;
						MakeBeamBetween(dimGirt, FacePoint(gp3, 1f, 0f), gp3.outVector, FacePoint(gp6, 1f, v), gp6.outVector, "Cross");
						MakeBeamBetween(dimGirt, FacePoint(gp4, -1f, 0f), gp4.outVector, FacePoint(gp5, -1f, v), gp5.outVector, "Cross");
					}
				}
			}
		}
		Vector3 FacePoint(GirtPoint girtPoint, float insideOutside, float num)
		{
			Quaternion quaternion = Quaternion.LookRotation(girtPoint.outVector);
			return girtPoint.point + quaternion * (insideOutside * girtPostOffset + num * Vector3.up);
		}
	}

	private void GenerateStringers(List<Vector4> bentPoints, LineCurve lc)
	{
		float stringerRadius = 0.75f;
		LinePoint head = lc.Head;
		LinePoint tail = lc.Tail;
		for (int i = 0; i < bentPoints.Count; i++)
		{
			Vector4 vector = bentPoints[i];
			if (i == 0)
			{
				MakeStringer(MakeBentPoint(head), vector);
			}
			if (i == bentPoints.Count - 1)
			{
				MakeStringer(vector, MakeBentPoint(tail));
			}
			else
			{
				Vector4 bp = bentPoints[i + 1];
				MakeStringer(vector, bp);
			}
			bentPoints[i] = vector + (Vector4)(dimStringer.y * Vector3.down);
		}
		if (headStyle == EndStyle.Block)
		{
			MakeBlock(MakeBentPoint(head));
		}
		if (tailStyle == EndStyle.Block)
		{
			MakeBlock(MakeBentPoint(tail));
		}
		static Vector4 MakeBentPoint(LinePoint lp)
		{
			Vector4 result = lp.point;
			result.w = Quaternion.LookRotation(lp.direction).eulerAngles.y;
			return result;
		}
		void MakeBlock(Vector4 vec4)
		{
			Vector3 vector2 = dimCap;
			vec4.y -= vector2.y / 2f;
			for (int j = 0; j < 8; j++)
			{
				float num = 13f + (float)j;
				MakeCrossBeamCentered(vector2, num * 0.30480063f, vec4);
				vec4.y -= vector2.y;
			}
		}
		void MakeStringer(Vector4 vector3, Vector4 vector4)
		{
			Vector3 vector2 = dimStringer.y / 2f * Vector3.down;
			Quaternion quaternion = Quaternion.Euler(0f, vector3.w, 0f);
			Quaternion quaternion2 = Quaternion.Euler(0f, vector4.w, 0f);
			Vector3 a = (Vector3)vector3 + vector2 + quaternion * (stringerRadius * Vector3.right);
			Vector3 a2 = (Vector3)vector3 + vector2 + quaternion * ((0f - stringerRadius) * Vector3.right);
			Vector3 b = (Vector3)vector4 + vector2 + quaternion2 * (stringerRadius * Vector3.right);
			Vector3 b2 = (Vector3)vector4 + vector2 + quaternion2 * ((0f - stringerRadius) * Vector3.right);
			MakeBeamBetween(dimStringer, a, Vector3.up, b, Vector3.up, "Stringer L");
			MakeBeamBetween(dimStringer, a2, Vector3.up, b2, Vector3.up, "Stringer R");
		}
	}

	private List<Vector4> CalculateBentPoints(LineCurve lc, float bentSpacing)
	{
		lc.Split(lc.Length / 2f, out var before, out var after);
		List<Vector4> list = new List<Vector4>();
		list.Add(BentPoint(before.Tail));
		before = before.Reverse();
		while (before.Length > bentSpacing)
		{
			before = before.Skip(bentSpacing);
			if (before.Length > bentSpacing)
			{
				list.Insert(0, BentPoint(before.Head));
			}
			if (headStyle == EndStyle.Bent && before.Length < bentSpacing)
			{
				LineCurve lineCurve = before.Reverse();
				if (lineCurve.Length > 0.67499995f)
				{
					list.Insert(0, BentPoint(lineCurve.LinePointAtDistance(0.67499995f)));
				}
				if (lineCurve.Length > 0.225f)
				{
					list.Insert(0, BentPoint(lineCurve.LinePointAtDistance(0.225f)));
				}
			}
		}
		while (after.Length > bentSpacing)
		{
			after = after.Skip(bentSpacing);
			if (after.Length > bentSpacing)
			{
				list.Add(BentPoint(after.Head));
			}
			if (tailStyle == EndStyle.Bent && after.Length < bentSpacing)
			{
				LineCurve lineCurve2 = after.Reverse();
				if (lineCurve2.Length > 0.67499995f)
				{
					list.Add(BentPoint(lineCurve2.LinePointAtDistance(0.67499995f)));
				}
				if (lineCurve2.Length > 0.225f)
				{
					list.Add(BentPoint(lineCurve2.LinePointAtDistance(0.225f)));
				}
			}
		}
		return list;
		static Vector4 BentPoint(LinePoint pt)
		{
			return new Vector4(pt.point.x, pt.point.y, pt.point.z, Quaternion.LookRotation(pt.direction).eulerAngles.y);
		}
	}

	private static float[] CalculateGirtHeights(List<Vector4> bentPoints, float bentHeight, float bentSpacing)
	{
		List<float> list = new List<float>();
		float y = bentPoints[bentPoints.Count / 2].y;
		float num = y - bentHeight;
		for (float num2 = y - bentSpacing; num2 > num; num2 -= bentSpacing)
		{
			list.Add(num2);
		}
		return list.ToArray();
	}

	private void MakeBent(Vector4 bentPoint, float bentHeight, float[] girtHeights, out GirtPoint[,] girtPoints)
	{
		int num = 4;
		float num2 = 0.5f;
		float num3 = 3.962408f;
		girtPoints = new GirtPoint[2, girtHeights.Length];
		MakeCrossBeamCentered(dimCap, num3, bentPoint + (Vector4)(dimCap.y / 2f * Vector3.down));
		Quaternion bentRotation = Quaternion.Euler(0f, bentPoint.w, 0f);
		Vector3 vector = bentPoint;
		vector.y = bentPoint.y - dimCap.y / 2f;
		float num4 = ((float)num - 1f) / 2f;
		float num5 = (num3 - num2 * 2f) / ((float)num - 1f);
		for (int i = 0; i < num; i++)
		{
			float tilt = 3f * (num4 - (float)i);
			float num6 = num5 * (num4 - (float)i);
			Vector3 a = vector + bentRotation * (Vector3.right * num6);
			Vector3 b = PostPoint(bentHeight + 3f);
			Vector3 vector2 = bentRotation * Quaternion.Euler(0f, 0f, tilt) * Quaternion.Euler(90f, 0f, 0f) * Vector3.forward;
			MakeBeamBetween(dimPost, a, vector2, b, vector2, $"Post {i} {num4:F1}");
			if (i != 0 && i != num - 1)
			{
				continue;
			}
			int num7 = ((i != 0) ? 1 : 0);
			for (int j = 0; j < girtHeights.Length; j++)
			{
				float num8 = girtHeights[j];
				if (num8 < bentPoint.y - bentHeight)
				{
					girtPoints[num7, j] = new GirtPoint
					{
						point = Vector3.zero,
						outVector = Vector3.zero
					};
				}
				else
				{
					Vector3 point = PostPoint(bentPoint.y - num8);
					Vector3 outVector = bentRotation * ((i == 0) ? Vector3.right : (-Vector3.right));
					girtPoints[num7, j] = new GirtPoint
					{
						point = point,
						outVector = outVector
					};
				}
			}
			Vector3 PostPoint(float depthBelowTop)
			{
				return a - depthBelowTop * Vector3.up + bentRotation * (Vector3.right * depthBelowTop * Mathf.Tan(tilt * (MathF.PI / 180f)));
			}
		}
		for (int k = 1; k < girtHeights.Length; k++)
		{
			GirtPoint girtPoint = girtPoints[0, k];
			GirtPoint girtPoint2 = girtPoints[1, k];
			if (!girtPoint.outVector.IsZero() && !girtPoint2.outVector.IsZero())
			{
				Vector3 vector3 = bentRotation * ((dimPost.x / 2f + dimGirt.x / 2f) * -Vector3.forward);
				Vector3 vector4 = bentRotation * (0.5f * Vector3.right);
				MakeBeamBetween(dimGirt, girtPoint.point + vector3 + vector4, girtPoint.outVector, girtPoint2.point + vector3 - vector4, -girtPoint2.outVector, "Girt");
			}
		}
	}

	private void MakeCrossBeamCentered(Vector2 dimension, float length, Vector4 at)
	{
		Transform obj = MakeBeam(new Vector3(dimension.x, length, dimension.y));
		obj.localPosition = at;
		obj.localRotation = Quaternion.Euler(0f, at.w + 90f, 0f) * Quaternion.Euler(-90f, 0f, 0f);
	}

	private void MakeBeamBetween(Vector2 dimension, Vector3 a, Vector3 aUp, Vector3 b, Vector3 bUp, string name)
	{
		float magnitude = (a - b).magnitude;
		Transform obj = MakeBeam(new Vector3(dimension.x, magnitude, dimension.y));
		obj.localPosition = Vector3.Lerp(a, b, 0.5f);
		obj.name = name;
		Quaternion quaternion = Quaternion.LookRotation(Vector3.Lerp(aUp, bUp, 0.5f));
		quaternion = Quaternion.LookRotation(a - b);
		obj.localRotation = quaternion * Quaternion.Euler(-90f, 0f, 0f);
	}

	private Transform MakeBeam(Vector3 dimensions)
	{
		Vector3 vector = new Vector3(0.23f, 2.59f, 0.18f);
		GameObject gameObject = UnityEngine.Object.Instantiate(profile.tiePrefab, base.transform, worldPositionStays: true);
		gameObject.hideFlags = HideFlags.HideAndDontSave;
		gameObject.name = "Beam";
		gameObject.tag = "AutoTrestleGenerated";
		gameObject.transform.localScale = new Vector3(dimensions.x / vector.x, dimensions.y / vector.y, dimensions.z / vector.z);
		_meshRenderers.AddRange(gameObject.GetComponentsInChildren<MeshRenderer>());
		return gameObject.transform;
	}

	private static BezierCurve MakeCurve(ControlPoint a, ControlPoint b)
	{
		Vector3 position = a.position;
		Vector3 position2 = b.position;
		float d = (position - position2).magnitude * TrackSegment.BezierTangentFactorForTangents(position, position2);
		Vector3 vector = TangentPointAlongSegment(a, b, d);
		Vector3 vector2 = TangentPointAlongSegment(b, a, d);
		return new BezierCurve(new Vector3[4] { position, vector, vector2, position2 }, a.rotation * Vector3.up, b.rotation * Vector3.up);
	}

	private static Vector3 TangentPointAlongSegment(ControlPoint controlPoint, ControlPoint other, float d)
	{
		Vector3 vector = controlPoint.Transform(Vector3.forward);
		Vector3 vector2 = controlPoint.Transform(Vector3.back);
		float magnitude = (vector - other.position).magnitude;
		float magnitude2 = (vector2 - other.position).magnitude;
		float num = ((magnitude < magnitude2) ? d : (0f - d));
		return controlPoint.Transform(Vector3.forward * num);
	}

	public void CullingSphereStateChanged(bool isVisible, int distanceBand)
	{
		if (distanceBand < 1)
		{
			GenerateIfNeeded();
		}
		foreach (MeshRenderer meshRenderer in _meshRenderers)
		{
			if (!(meshRenderer == null))
			{
				meshRenderer.enabled = isVisible;
			}
		}
	}

	public void RequestUpdateCullingPosition()
	{
		_cullingToken.UpdatePosition(base.transform);
	}

	private void GenerateIfNeeded()
	{
		if (!_hasGenerated && _generateTask == null)
		{
			_generateTask = CoroutineTask.Start(GenerateCoroutine(), this);
		}
	}
}
