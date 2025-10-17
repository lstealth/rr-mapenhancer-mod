using System;
using UnityEngine;

namespace Helpers;

public static class CylinderMeshBuilder
{
	public static Mesh BuildCapless(Matrix4x4 matrix)
	{
		Vector3[] array = new Vector3[32];
		for (int i = 0; i < 16; i++)
		{
			float f = (float)i / 16f * (MathF.PI * 2f);
			float y = Mathf.Sin(f) / 2f;
			float z = Mathf.Cos(f) / 2f;
			array[i * 2] = matrix.MultiplyPoint3x4(new Vector3(0.5f, y, z));
			array[i * 2 + 1] = matrix.MultiplyPoint3x4(new Vector3(-0.5f, y, z));
		}
		int[] array2 = new int[96];
		for (int j = 0; j < 16; j++)
		{
			int num = j * 2;
			array2[j * 6] = num % 32;
			array2[j * 6 + 1] = (num + 2) % 32;
			array2[j * 6 + 2] = (num + 1) % 32;
			array2[j * 6 + 3] = (num + 2) % 32;
			array2[j * 6 + 4] = (num + 3) % 32;
			array2[j * 6 + 5] = (num + 1) % 32;
		}
		Mesh mesh = new Mesh();
		mesh.vertices = array;
		mesh.triangles = array2;
		mesh.RecalculateBounds();
		return mesh;
	}
}
