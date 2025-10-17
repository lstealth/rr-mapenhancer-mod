using System;
using RLD;
using UnityEngine;

namespace Helpers;

public static class CapsuleMesh
{
	private static Mesh _halfCircleXYMesh;

	private static Mesh _unitCircleXYMesh;

	private static void BuildMeshesIfNeeded()
	{
		if ((object)_halfCircleXYMesh == null)
		{
			_halfCircleXYMesh = CreateWireCircleXYDegrees(0.5f, 180f, 100, Color.white);
		}
		if ((object)_unitCircleXYMesh == null)
		{
			_unitCircleXYMesh = CreateWireCircleXYDegrees(0.5f, 360f, 200, Color.white);
		}
	}

	public static void DrawCapsuleY(Vector3 position, Quaternion rotation, float radius, float height)
	{
		BuildMeshesIfNeeded();
		float num = radius * 2f;
		Vector3 center = position + rotation * Vector3.up * (height / 2f - radius);
		Vector3 vector = position + rotation * Vector3.down * (height / 2f - radius);
		Vector3 size = Vector3.one * num;
		OBB oBB = new OBB(center, size, rotation * Quaternion.Euler(90f, 0f, 0f));
		Graphics.DrawMeshNow(_unitCircleXYMesh, oBB.GetUnitBoxTransform());
		OBB oBB2 = new OBB(center, size, rotation * Quaternion.Euler(0f, 0f, 90f));
		Graphics.DrawMeshNow(_halfCircleXYMesh, oBB2.GetUnitBoxTransform());
		OBB oBB3 = new OBB(center, size, rotation * Quaternion.Euler(0f, 90f, 90f));
		Graphics.DrawMeshNow(_halfCircleXYMesh, oBB3.GetUnitBoxTransform());
		OBB oBB4 = new OBB(vector, size, rotation * Quaternion.Euler(90f, 0f, 0f));
		Graphics.DrawMeshNow(_unitCircleXYMesh, oBB4.GetUnitBoxTransform());
		OBB oBB5 = new OBB(vector, size, rotation * Quaternion.Euler(0f, 0f, -90f));
		Graphics.DrawMeshNow(_halfCircleXYMesh, oBB5.GetUnitBoxTransform());
		OBB oBB6 = new OBB(vector, size, rotation * Quaternion.Euler(0f, 90f, -90f));
		Graphics.DrawMeshNow(_halfCircleXYMesh, oBB6.GetUnitBoxTransform());
		Mesh unitSegmentX = Singleton<MeshPool>.Get.UnitSegmentX;
		Vector3 size2 = new Vector3(height - num, 1f, 1f);
		Vector3 vector2 = new Vector3(radius, 0f, 0f);
		for (int i = 0; i < 4; i++)
		{
			int num2 = i * 90;
			Graphics.DrawMeshNow(unitSegmentX, new OBB(vector + rotation * Quaternion.Euler(0f, num2, 0f) * vector2, size2, rotation * Quaternion.Euler(0f, 0f, 90f)).GetUnitBoxTransform());
		}
	}

	public static void DrawCircleXY(Vector3 position, Quaternion rotation, float radius)
	{
		BuildMeshesIfNeeded();
		Graphics.DrawMeshNow(_unitCircleXYMesh, Matrix4x4.TRS(position, rotation, Vector3.one * radius * 2f));
	}

	private static Mesh CreateWireCircleXYDegrees(float circleRadius, float degrees, int numBorderPoints, Color color)
	{
		if (circleRadius < 0.0001f || numBorderPoints < 4)
		{
			return null;
		}
		Vector3[] array = new Vector3[numBorderPoints];
		int[] array2 = new int[numBorderPoints];
		float num = degrees / (float)(numBorderPoints - 1);
		for (int i = 0; i < numBorderPoints; i++)
		{
			float f = num * (float)i * (MathF.PI / 180f);
			array[i] = new Vector3(Mathf.Sin(f) * circleRadius, Mathf.Cos(f) * circleRadius, 0f);
			array2[i] = i;
		}
		Mesh mesh = new Mesh();
		mesh.vertices = array;
		mesh.colors = ColorEx.GetFilledColorArray(numBorderPoints, color);
		mesh.SetIndices(array2, MeshTopology.LineStrip, 0);
		mesh.UploadMeshData(markNoLongerReadable: false);
		return mesh;
	}
}
