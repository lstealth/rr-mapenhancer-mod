using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core;
using Track;
using UnityEngine;

internal class TrackMeshBuilder
{
	private struct ProfilePoint
	{
		public Vector2 p;

		public float u;

		public float n;

		public ProfilePoint(Vector2 p, float u, float n = 0f)
		{
			this.p = p;
			this.u = u;
			this.n = n;
		}
	}

	private static readonly ProfilePoint[] ProfilePoints = new ProfilePoint[21]
	{
		new ProfilePoint(new Vector2(0f, -0.177f), 0f, 180f),
		new ProfilePoint(new Vector2(0.078f, -0.177f), 0.09f, -133.845f),
		new ProfilePoint(new Vector2(0.078f, -0.167f), 0.108f, -51.12f),
		new ProfilePoint(new Vector2(0.025f, -0.153f), 0.171f, -31.109f),
		new ProfilePoint(new Vector2(0.013f, -0.14f), 0.192f, -67.514f),
		new ProfilePoint(new Vector2(0.009f, -0.071f), 0.272f, -91.606f),
		new ProfilePoint(new Vector2(0.012f, -0.043f), 0.303f, -127.17f),
		new ProfilePoint(new Vector2(0.038f, -0.033f), 0.335f, -122.043f),
		new ProfilePoint(new Vector2(0.036f, -0.005f), 0.367f, -59.311f),
		new ProfilePoint(new Vector2(0.03f, -0.001f), 0.385f, -17.437f),
		new ProfilePoint(new Vector2(0f, 0f), 0.419f),
		new ProfilePoint(new Vector2(-0.03f, -0.001f), 0.385f, 17.437f),
		new ProfilePoint(new Vector2(-0.036f, -0.005f), 0.367f, 59.311f),
		new ProfilePoint(new Vector2(-0.038f, -0.033f), 0.335f, 122.043f),
		new ProfilePoint(new Vector2(-0.012f, -0.043f), 0.303f, 127.17f),
		new ProfilePoint(new Vector2(-0.009f, -0.071f), 0.272f, 91.606f),
		new ProfilePoint(new Vector2(-0.013f, -0.14f), 0.192f, 67.514f),
		new ProfilePoint(new Vector2(-0.025f, -0.153f), 0.171f, 31.109f),
		new ProfilePoint(new Vector2(-0.078f, -0.167f), 0.108f, 51.12f),
		new ProfilePoint(new Vector2(-0.078f, -0.177f), 0.09f, 133.845f),
		new ProfilePoint(new Vector2(0f, -0.177f), 0f, 180f)
	};

	private static readonly Dictionary<Gauge, ProfilePoint[]> RailOnlyProfilePointsCache = new Dictionary<Gauge, ProfilePoint[]>();

	private static ProfilePoint[] MakeRailOnlyProfile(Gauge gauge)
	{
		if (RailOnlyProfilePointsCache.TryGetValue(gauge, out var value))
		{
			return value;
		}
		float rw = gauge.HeadWidth;
		ProfilePoint[] array = ProfilePoints.Select((ProfilePoint pp) => new ProfilePoint(pp.p + new Vector2(rw / 2f, 0f), pp.u, pp.n)).ToArray();
		RailOnlyProfilePointsCache[gauge] = array;
		return array;
	}

	private static Mesh ExtrudePoints(string name, Vector3[] points, Quaternion[] rotations, ProfilePoint[] profile, Func<int, float> profileScale, float uvVScale = 1f)
	{
		return ExtrudePoints(name, points, rotations, profile, profileScale, uvVScale, Matrix4x4.identity);
	}

	private static Mesh ExtrudePoints(string name, Vector3[] points, Quaternion[] rotations, ProfilePoint[] profile, Func<int, float> profileScale, float uvVScale, Matrix4x4 matrix)
	{
		TrackMeshData.EndCapData endCapData = TrackMeshData.endCapData;
		Vector3[] array = new Vector3[points.Length * profile.Length + endCapData.Vertices.Length * 2];
		int num = (points.Length - 1) * (profile.Length - 1);
		int num2 = endCapData.Triangles.Length;
		int[] array2 = new int[num * 6 + num2 * 3 * 2];
		Vector2[] array3 = new Vector2[array.Length];
		Vector3[] array4 = new Vector3[array.Length];
		int num3 = 0;
		int num4 = profile.Length;
		float num5 = 0f;
		for (int i = 0; i < points.Length; i++)
		{
			Vector3 vector = points[i];
			Quaternion quaternion = rotations[i];
			if (i > 0)
			{
				num5 += (vector - points[i - 1]).magnitude * uvVScale;
			}
			for (int j = 0; j < profile.Length; j++)
			{
				Vector2 p = profile[j].p;
				p.x *= profileScale(i);
				Vector3 vector2 = quaternion * p;
				int num6 = i * num4 + j;
				float n = profile[j].n;
				array[num6] = vector + (Vector3)(matrix * vector2);
				array3[num6] = new Vector2(profile[j].u, num5);
				array4[num6] = quaternion * Quaternion.Euler(0f, 0f, n) * Vector2.up;
				num3 = num6;
			}
		}
		int num7 = 0;
		for (int k = 0; k < points.Length - 1; k++)
		{
			for (int l = 0; l < profile.Length - 1; l++)
			{
				int num8 = k * (num4 - 1) + l;
				int num9 = (array2[num8 * 6] = k * num4 + l);
				array2[num8 * 6 + 1] = num9 + 1;
				array2[num8 * 6 + 2] = num9 + num4 + 1;
				array2[num8 * 6 + 3] = num9;
				array2[num8 * 6 + 4] = num9 + (num4 + 1);
				array2[num8 * 6 + 5] = num9 + num4;
				num7 = num8 * 6 + 5;
			}
		}
		int num10 = num3 + 1;
		int num11 = num10;
		Vector3 vector3 = new Vector3(0.0382f, 0f, 0f);
		Quaternion quaternion2 = Quaternion.Euler(0f, 180f, 0f);
		float num12 = profileScale(0);
		for (int m = 0; m < endCapData.Vertices.Length; m++)
		{
			Vector3 vector4 = endCapData.Vertices[m];
			vector4 += new Vector3(0f - vector3.x, vector3.y, vector3.z);
			vector4.x *= num12;
			Quaternion quaternion3 = quaternion2 * rotations[0];
			array[num10] = points[0] + (Vector3)(matrix * (quaternion3 * vector4));
			Vector2 vector5 = endCapData.UVs[m];
			vector5.x += -0.3f;
			array3[num10] = vector5;
			array4[num10] = quaternion3 * Vector3.forward;
			num10++;
		}
		int[] triangles = endCapData.Triangles;
		foreach (int num14 in triangles)
		{
			array2[++num7] = num11 + num14;
		}
		num11 = num10;
		float num15 = profileScale(points.Length - 1);
		for (int num16 = 0; num16 < endCapData.Vertices.Length; num16++)
		{
			Vector3 vector6 = endCapData.Vertices[num16];
			vector6 += vector3;
			vector6.x *= num15;
			Quaternion quaternion4 = rotations[^1];
			array[num10] = points[^1] + (Vector3)(matrix * (quaternion4 * vector6));
			Vector2 vector7 = endCapData.UVs[num16];
			vector7.x += -0.3f;
			array3[num10] = vector7;
			array4[num10] = quaternion4 * Vector3.forward;
			num10++;
		}
		triangles = endCapData.Triangles;
		foreach (int num17 in triangles)
		{
			array2[++num7] = num11 + num17;
		}
		Mesh mesh = new Mesh();
		mesh.name = name;
		mesh.vertices = array;
		mesh.triangles = array2;
		mesh.uv = array3;
		mesh.normals = array4;
		mesh.RecalculateBounds();
		return mesh;
	}

	public static Mesh BuildFrogMesh(LinePoint[] points, Gauge gauge)
	{
		Vector3 lhs = Vector3.Cross(points[0].point - points[1].point, points[2].point - points[1].point);
		Vector3 vector = points[1].Rotation * Vector3.up;
		bool num = Vector3.Dot(lhs, vector) < 0f;
		LinePoint linePoint = (num ? points[0] : points[2]);
		LinePoint linePoint2 = points[1];
		LinePoint linePoint3 = (num ? points[2] : points[0]);
		Vector3[] array = new Vector3[3] { linePoint.point, linePoint2.point, linePoint3.point };
		Quaternion[] rotations = new Quaternion[3]
		{
			Quaternion.LookRotation((linePoint2.point - array[0]).normalized, vector),
			Quaternion.LookRotation(((array[2] - linePoint2.point).normalized - (array[0] - linePoint2.point).normalized).normalized, vector),
			Quaternion.LookRotation((array[2] - linePoint2.point).normalized, vector)
		};
		ProfilePoint[] profile = MakeRailOnlyProfile(gauge);
		float coeff = 1f / (float)Math.Sin(Vector3.Angle(linePoint.point - linePoint2.point, linePoint3.point - linePoint2.point) * (MathF.PI / 180f) / 2f);
		return ExtrudePoints("Frog", array, rotations, profile, (int i) => (i != 1) ? 1f : coeff);
	}

	public static Mesh BuildStockRailMesh(LineCurve curve, Vector3 switchHome, Gauge gauge, Func<int, float> profileScale)
	{
		Quaternion rot = Quaternion.identity;
		if (curve.hand == Hand.Left)
		{
			curve = curve.Reverse();
			rot = Quaternion.Euler(0f, 180f, 0f);
			int nMinus1 = curve.Points.Count() - 1;
			Func<int, float> profileScale2 = profileScale;
			profileScale = (int i) => profileScale2(nMinus1 - i);
		}
		Vector3[] points = curve.Points.Select((LinePoint p) => p.point).ToArray();
		Quaternion[] rotations = curve.Points.Select((LinePoint p) => p.Rotation * rot).ToArray();
		ProfilePoint[] profile = MakeRailOnlyProfile(gauge);
		return ExtrudePoints("StockRail", points, rotations, profile, profileScale);
	}

	public static Mesh BuildColliderMesh(BezierCurve center, Gauge gauge)
	{
		return BuildColliderMesh(center, gauge, Matrix4x4.identity);
	}

	public static Mesh BuildColliderMesh(BezierCurve center, Gauge gauge, Matrix4x4 matrix)
	{
		LineCurve lineCurve = new LineCurve(center.Approximate(), Hand.Left).Subdivide(20f);
		Vector3[] path = lineCurve.Points.Select((LinePoint p) => p.point).ToArray();
		Quaternion[] rotations = lineCurve.Points.Select((LinePoint p) => p.Rotation).ToArray();
		return BuildColliderMesh(path, rotations, gauge);
	}

	public static Mesh BuildColliderMesh(Vector3[] path, Quaternion[] rotations, Gauge gauge)
	{
		float railHeight = gauge.RailHeight;
		ProfilePoint[] profile = new ProfilePoint[4]
		{
			new ProfilePoint(new Vector2(2f, -0.75f), 0f, -45f),
			new ProfilePoint(new Vector2(1f, 0f - railHeight), 0f),
			new ProfilePoint(new Vector2(-1f, 0f - railHeight), 0f),
			new ProfilePoint(new Vector2(-2f, -0.75f), 0f, 45f)
		};
		return ExtrudePoints("Collider", path, rotations, profile, (int i) => 1f);
	}

	public static void GenerateProfileMeshString(Mesh trackProfileMesh)
	{
		List<ProfilePoint> output = new List<ProfilePoint>();
		Vector3 vector = new Vector3(100f, 100f, 100f);
		Vector3 vector2 = new Vector3(-100f, -100f, -100f);
		Vector3[] vertices = trackProfileMesh.vertices;
		for (int i = 0; i < vertices.Length; i++)
		{
			Vector3 vector3 = vertices[i];
			vector.x = Mathf.Min(vector.x, vector3.x);
			vector.y = Mathf.Min(vector.y, vector3.y);
			vector.z = Mathf.Min(vector.z, vector3.z);
			vector2.x = Mathf.Max(vector2.x, vector3.x);
			vector2.y = Mathf.Max(vector2.y, vector3.y);
			vector2.z = Mathf.Max(vector2.z, vector3.z);
		}
		Debug.Log($"min = {vector * 100f}, max = {vector2 * 100f}");
		int num = trackProfileMesh.triangles.Length / 3;
		for (int j = 0; j < num; j++)
		{
			int num2 = trackProfileMesh.triangles[j * 3];
			int num3 = trackProfileMesh.triangles[j * 3 + 1];
			int num4 = trackProfileMesh.triangles[j * 3 + 2];
			Vector3 v = trackProfileMesh.vertices[num2] * 100f;
			Vector3 v2 = trackProfileMesh.vertices[num3] * 100f;
			Vector3 v3 = trackProfileMesh.vertices[num4] * 100f;
			if ((!IsClose(v.y, vector.y) || !IsClose(v2.y, vector.y) || !IsClose(v3.y, vector.y)) && (!IsClose(v.y, vector2.y) || !IsClose(v2.y, vector2.y) || !IsClose(v3.y, vector2.y)))
			{
				if (Math.Abs(v.y) < 0.001f)
				{
					AddUnique(v, trackProfileMesh.normals[num2], trackProfileMesh.uv[num2]);
				}
				if (Math.Abs(v2.y) < 0.001f)
				{
					AddUnique(v2, trackProfileMesh.normals[num3], trackProfileMesh.uv[num3]);
				}
				if (Math.Abs(v3.y) < 0.001f)
				{
					AddUnique(v3, trackProfileMesh.normals[num4], trackProfileMesh.uv[num4]);
				}
			}
		}
		output.Sort(delegate(ProfilePoint a, ProfilePoint b)
		{
			if (a.p.x < 0f && b.p.x > 0f)
			{
				return -1;
			}
			if (a.p.x > 0f && b.p.x < 0f)
			{
				return 1;
			}
			if (!(a.p.x < 0f) || !(b.p.x < 0f))
			{
				if (!(b.u < a.u))
				{
					return 1;
				}
				return -1;
			}
			return (!(a.u < b.u)) ? 1 : (-1);
		});
		output.Reverse();
		string text = "";
		foreach (ProfilePoint item in output)
		{
			text += $"new ProfilePoint(new Vector2({item.p.x:F3}f, {item.p.y:F3}f), {item.u:F3}f, {item.n:F3}f),\n";
		}
		Debug.Log("Output:\n" + text);
		void AddUnique(Vector3 vector4, Vector3 n, Vector2 uv)
		{
			ProfilePoint profPoint = new ProfilePoint(new Vector2(vector4.x, vector4.z), uv.x, Vector2.SignedAngle(Vector2.up, new Vector2(n.x, n.z)));
			if (!output.Exists((ProfilePoint pp) => pp.p.Equals(profPoint.p)))
			{
				output.Add(profPoint);
			}
		}
		static bool IsClose(float a, float b)
		{
			return Mathf.Abs(a - b) < 0.001f;
		}
	}

	public static void GenerateEndCapMeshString(Mesh trackProfileMesh)
	{
		Vector3 vector = new Vector3(100f, 100f, 100f);
		Vector3 vector2 = new Vector3(-100f, -100f, -100f);
		Vector3[] vertices = trackProfileMesh.vertices;
		for (int i = 0; i < vertices.Length; i++)
		{
			Vector3 vector3 = vertices[i];
			vector.x = Mathf.Min(vector.x, vector3.x);
			vector.y = Mathf.Min(vector.y, vector3.y);
			vector.z = Mathf.Min(vector.z, vector3.z);
			vector2.x = Mathf.Max(vector2.x, vector3.x);
			vector2.y = Mathf.Max(vector2.y, vector3.y);
			vector2.z = Mathf.Max(vector2.z, vector3.z);
		}
		Debug.Log($"min = {vector * 100f}, max = {vector2 * 100f}");
		List<int> list = new List<int>();
		int num = trackProfileMesh.triangles.Length / 3;
		for (int j = 0; j < num; j++)
		{
			int num2 = trackProfileMesh.triangles[j * 3];
			int num3 = trackProfileMesh.triangles[j * 3 + 1];
			int num4 = trackProfileMesh.triangles[j * 3 + 2];
			Vector3 vector4 = trackProfileMesh.vertices[num2] * 100f;
			Vector3 vector5 = trackProfileMesh.vertices[num3] * 100f;
			Vector3 vector6 = trackProfileMesh.vertices[num4] * 100f;
			if (IsClose(vector4.y, vector.y) && IsClose(vector5.y, vector.y) && IsClose(vector6.y, vector.y))
			{
				list.Add(j);
			}
		}
		Dictionary<int, int> vertexIndexMap = new Dictionary<int, int>();
		List<int> list2 = new List<int>();
		List<Vector3> vertices2 = new List<Vector3>();
		List<Vector2> uvs = new List<Vector2>();
		List<Vector3> normals = new List<Vector3>();
		foreach (int item4 in list)
		{
			int item = MapVertex(trackProfileMesh.triangles[item4 * 3]);
			int item2 = MapVertex(trackProfileMesh.triangles[item4 * 3 + 1]);
			int item3 = MapVertex(trackProfileMesh.triangles[item4 * 3 + 2]);
			list2.Add(item);
			list2.Add(item2);
			list2.Add(item3);
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("internal static EndCapData endCapData = new EndCapData(");
		stringBuilder.AppendLine("new Vector3[] {");
		foreach (Vector3 item5 in vertices2)
		{
			stringBuilder.AppendLine(VectorString(item5));
		}
		stringBuilder.AppendLine("},");
		stringBuilder.AppendLine("new Vector2[] {");
		foreach (Vector2 item6 in uvs)
		{
			stringBuilder.AppendLine($"new Vector2({item6.x}f, {item6.y}f),");
		}
		stringBuilder.AppendLine("},");
		stringBuilder.AppendLine("new int[] {");
		stringBuilder.AppendLine(string.Join(", ", list2) ?? "");
		stringBuilder.AppendLine("}");
		stringBuilder.AppendLine(");");
		Debug.Log($"{stringBuilder}");
		static bool IsClose(float a, float b)
		{
			return Mathf.Abs(a - b) < 0.001f;
		}
		int MapVertex(int vertexIndex)
		{
			if (vertexIndexMap.TryGetValue(vertexIndex, out var value))
			{
				return value;
			}
			value = vertices2.Count;
			vertexIndexMap[vertexIndex] = value;
			Vector3 vector7 = trackProfileMesh.vertices[vertexIndex] * 100f;
			vector7 = new Vector3(vector7.x, vector7.z, vector7.y);
			vertices2.Add(vector7);
			uvs.Add(trackProfileMesh.uv[vertexIndex]);
			normals.Add(trackProfileMesh.normals[vertexIndex]);
			return value;
		}
		static string VectorString(Vector3 v)
		{
			float num5 = 1E-05f;
			if (Mathf.Abs(v.x) < num5)
			{
				v.x = 0f;
			}
			if (Mathf.Abs(v.y) < num5)
			{
				v.y = 0f;
			}
			if (Mathf.Abs(v.z) < num5)
			{
				v.z = 0f;
			}
			return $"new Vector3({v.x}f, {v.y}f, {v.z}f),";
		}
	}
}
