using System.Collections.Generic;
using Core;
using Core.Bezier;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace BezierCurveMesh;

public static class CurveMeshGenerator
{
	private struct CurveSegmentInfo
	{
		public int SegmentCount;
	}

	[BurstCompile]
	private struct MeshGenerationJob : IJobParallelFor
	{
		[ReadOnly]
		public NativeArray<float3> InputVertices;

		[ReadOnly]
		public NativeArray<float3> InputNormals;

		[ReadOnly]
		public NativeArray<float2> InputUVs;

		[ReadOnly]
		public NativeArray<CubicBezierCurve> Curves;

		[ReadOnly]
		public NativeArray<CurveSegmentInfo> CurveInfos;

		public float MeshLength;

		public VectorAxis ForwardAxis;

		public float3 VertexScale;

		[NativeDisableParallelForRestriction]
		public NativeArray<float3> OutputVertices;

		[NativeDisableParallelForRestriction]
		public NativeArray<float3> OutputNormals;

		[NativeDisableParallelForRestriction]
		public NativeArray<float2> OutputUVs;

		public void Execute(int index)
		{
			int length = InputVertices.Length;
			int i = 0;
			int num;
			for (num = index; i < Curves.Length && num >= CurveInfos[i].SegmentCount * length; i++)
			{
				num -= CurveInfos[i].SegmentCount * length;
			}
			if (i < Curves.Length)
			{
				int num2 = num / length;
				int index2 = num % length;
				float3 @float = InputVertices[index2] * VertexScale;
				CubicBezierCurve curve = Curves[i];
				CurveSegmentInfo curveSegmentInfo = CurveInfos[i];
				float num3 = GetComponentForAxis(ForwardAxis, @float) / MeshLength;
				float x = (float)num2 / (float)curveSegmentInfo.SegmentCount + num3 / (float)curveSegmentInfo.SegmentCount;
				x = math.clamp(x, 0f, 1f);
				float3 offsetForAxis = GetOffsetForAxis(ForwardAxis, @float);
				BezierMath.GetPoint(in curve, x, out var result);
				BezierMath.GetRotation(in curve, x, math.up(), out var rotationOut);
				float3 value = result + math.mul(rotationOut, offsetForAxis);
				OutputVertices[index] = value;
				float3 v = math.normalize(InputNormals[index2] * VertexScale);
				float3 value2 = math.normalize(math.mul(rotationOut, v));
				OutputNormals[index] = value2;
				OutputUVs[index] = InputUVs[index2];
			}
		}

		private float3 GetOffsetForAxis(VectorAxis axis, float3 v)
		{
			if (axis != VectorAxis.X)
			{
				return new float3(v.x, v.z, 0f);
			}
			return new float3(v.y, v.z, 0f);
		}

		private float GetComponentForAxis(VectorAxis axis, float3 vector)
		{
			if (axis == VectorAxis.X)
			{
				return vector.x;
			}
			return vector.y;
		}
	}

	public static Mesh GenerateMeshAlongCurves(Mesh segmentMesh, List<BezierCurve> curves, VectorAxis forwardAxis, Vector3 vertexScale)
	{
		if (curves == null || curves.Count == 0)
		{
			Debug.LogError("No curves provided for mesh generation.");
			return null;
		}
		Vector3 vector = Vector3.Scale(segmentMesh.bounds.size, vertexScale);
		float num = Mathf.Abs(GetComponentForAxis(forwardAxis, vector));
		List<CurveSegmentInfo> list = new List<CurveSegmentInfo>();
		foreach (BezierCurve curf in curves)
		{
			int segmentCount = Mathf.CeilToInt(curf.CalculateLength() / num);
			list.Add(new CurveSegmentInfo
			{
				SegmentCount = segmentCount
			});
		}
		int num2 = 0;
		foreach (CurveSegmentInfo item in list)
		{
			num2 += item.SegmentCount * segmentMesh.vertices.Length;
		}
		NativeArray<float3> outputVertices = new NativeArray<float3>(num2, Allocator.TempJob);
		NativeArray<float3> outputNormals = new NativeArray<float3>(num2, Allocator.TempJob);
		NativeArray<float2> outputUVs = new NativeArray<float2>(num2, Allocator.TempJob);
		NativeArray<float3> inputVertices = new NativeArray<float3>(segmentMesh.vertices.Length, Allocator.TempJob);
		NativeArray<float3> inputNormals = new NativeArray<float3>(segmentMesh.normals.Length, Allocator.TempJob);
		NativeArray<float2> inputUVs = new NativeArray<float2>(segmentMesh.uv.Length, Allocator.TempJob);
		NativeArray<CubicBezierCurve> curves2 = new NativeArray<CubicBezierCurve>(curves.Count, Allocator.TempJob);
		NativeArray<CurveSegmentInfo> curveInfos = new NativeArray<CurveSegmentInfo>(list.Count, Allocator.TempJob);
		try
		{
			for (int i = 0; i < segmentMesh.vertices.Length; i++)
			{
				inputVertices[i] = segmentMesh.vertices[i];
				inputNormals[i] = segmentMesh.normals[i];
				inputUVs[i] = segmentMesh.uv[i];
			}
			for (int j = 0; j < curves.Count; j++)
			{
				curves2[j] = new CubicBezierCurve(curves[j]);
				curveInfos[j] = list[j];
			}
			IJobParallelForExtensions.Schedule(new MeshGenerationJob
			{
				InputVertices = inputVertices,
				InputNormals = inputNormals,
				InputUVs = inputUVs,
				Curves = curves2,
				CurveInfos = curveInfos,
				MeshLength = num,
				ForwardAxis = forwardAxis,
				VertexScale = vertexScale,
				OutputVertices = outputVertices,
				OutputNormals = outputNormals,
				OutputUVs = outputUVs
			}, num2, 64).Complete();
			Mesh mesh = new Mesh();
			mesh.vertices = outputVertices.Reinterpret<Vector3>().ToArray();
			mesh.normals = outputNormals.Reinterpret<Vector3>().ToArray();
			mesh.uv = outputUVs.Reinterpret<Vector2>().ToArray();
			mesh.subMeshCount = segmentMesh.subMeshCount;
			int num3 = 0;
			for (int k = 0; k < segmentMesh.subMeshCount; k++)
			{
				List<int> list2 = new List<int>();
				int[] triangles = segmentMesh.GetTriangles(k);
				foreach (CurveSegmentInfo item2 in list)
				{
					for (int l = 0; l < item2.SegmentCount; l++)
					{
						int num4 = num3 + l * segmentMesh.vertexCount;
						for (int m = 0; m < triangles.Length; m += 3)
						{
							list2.Add(triangles[m] + num4);
							list2.Add(triangles[m + 2] + num4);
							list2.Add(triangles[m + 1] + num4);
						}
					}
					num3 += item2.SegmentCount * segmentMesh.vertexCount;
				}
				mesh.SetTriangles(list2.ToArray(), k);
			}
			mesh.RecalculateBounds();
			mesh.RecalculateNormals();
			return mesh;
		}
		finally
		{
			outputVertices.Dispose();
			outputNormals.Dispose();
			outputUVs.Dispose();
			inputVertices.Dispose();
			inputNormals.Dispose();
			inputUVs.Dispose();
			curves2.Dispose();
			curveInfos.Dispose();
		}
	}

	private static float GetComponentForAxis(VectorAxis axis, Vector3 vector)
	{
		if (axis == VectorAxis.X)
		{
			return vector.x;
		}
		return vector.y;
	}
}
