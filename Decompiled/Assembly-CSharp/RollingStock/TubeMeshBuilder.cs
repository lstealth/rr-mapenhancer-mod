using System;
using UnityEngine;

namespace RollingStock;

public class TubeMeshBuilder
{
	private readonly Vector3[] _vertices;

	private readonly Vector3[] _normals;

	private readonly int _numControlPoints;

	private readonly int _numAxialPoints;

	public Mesh Mesh { get; private set; }

	public TubeMeshBuilder(int numControlPoints, int numAxialPoints)
	{
		if (numControlPoints < 2)
		{
			throw new ArgumentException("Must have 2 or more control points");
		}
		if (numAxialPoints < 2)
		{
			throw new ArgumentException("Must have 2 or more axial points");
		}
		_numControlPoints = numControlPoints;
		_numAxialPoints = numAxialPoints;
		_vertices = new Vector3[_numControlPoints * _numAxialPoints];
		_normals = new Vector3[_vertices.Length];
		MakeStaticArrays(out var tris, out var uv);
		Mesh = new Mesh
		{
			vertices = _vertices,
			triangles = tris,
			uv = uv,
			normals = _normals
		};
	}

	public void UpdateWithPoints(Vector3[] points, Quaternion[] rotations, float radius)
	{
		int numAxialPoints = _numAxialPoints;
		float num = 0f;
		for (int i = 0; i < _numControlPoints; i++)
		{
			Vector3 vector = points[i];
			Quaternion quaternion = rotations[i];
			if (i > 0)
			{
				num += (vector - points[i - 1]).magnitude;
			}
			float num2 = MathF.PI * 2f * (1f / (float)(_numAxialPoints - 1));
			for (int j = 0; j < _numAxialPoints; j++)
			{
				float f = num2 * (float)j;
				Vector3 vector2 = Vector3.right * Mathf.Sin(f) + Vector3.up * Mathf.Cos(f);
				Vector3 vector3 = quaternion * vector2 * radius;
				int num3 = i * numAxialPoints + j;
				_vertices[num3] = vector + vector3;
				_normals[num3] = quaternion * vector2;
			}
		}
		Mesh.SetVertices(_vertices);
		Mesh.SetNormals(_normals);
		Mesh.RecalculateBounds();
	}

	private void MakeStaticArrays(out int[] tris, out Vector2[] uv)
	{
		int num = (_numControlPoints - 1) * (_numAxialPoints - 1);
		int numAxialPoints = _numAxialPoints;
		uv = new Vector2[_vertices.Length];
		for (int i = 0; i < uv.Length; i++)
		{
			uv[i] = Vector2.zero;
		}
		tris = new int[num * 6];
		for (int j = 0; j < _numControlPoints - 1; j++)
		{
			for (int k = 0; k < _numAxialPoints - 1; k++)
			{
				int num2 = j * (numAxialPoints - 1) + k;
				int num3 = j * numAxialPoints + k;
				tris[num2 * 6 + 2] = num3;
				tris[num2 * 6 + 1] = num3 + 1;
				tris[num2 * 6] = num3 + numAxialPoints + 1;
				tris[num2 * 6 + 5] = num3;
				tris[num2 * 6 + 4] = num3 + (numAxialPoints + 1);
				tris[num2 * 6 + 3] = num3 + numAxialPoints;
			}
		}
	}
}
