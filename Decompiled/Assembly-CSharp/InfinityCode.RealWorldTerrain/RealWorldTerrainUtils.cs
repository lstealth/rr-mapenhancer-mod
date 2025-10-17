using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

namespace InfinityCode.RealWorldTerrain;

public static class RealWorldTerrainUtils
{
	public const float EARTH_RADIUS = 6371f;

	public const int EQUATOR_LENGTH = 40075;

	public const int AVERAGE_TEXTURE_SIZE = 20000;

	public const double DEG2RAD = Math.PI / 180.0;

	public const int DOWNLOAD_TEXTURE_LIMIT = 90000000;

	public const int MAX_ELEVATION = 15000;

	public const short TILE_SIZE = 256;

	public const int MB = 1048576;

	public const float PI4 = MathF.PI * 4f;

	public static float Angle2D(Vector2 point1, Vector2 point2)
	{
		return Mathf.Atan2(point2.y - point1.y, point2.x - point1.x) * 57.29578f;
	}

	public static float Angle2D(Vector3 point1, Vector3 point2)
	{
		return Mathf.Atan2(point2.z - point1.z, point2.x - point1.x) * 57.29578f;
	}

	public static float Angle2D(Vector3 point1, Vector3 point2, Vector3 point3, bool unsigned = true)
	{
		float num = Angle2D(point1, point2);
		float num2 = Angle2D(point2, point3);
		float num3 = num - num2;
		if (num3 > 180f)
		{
			num3 -= 360f;
		}
		if (num3 < -180f)
		{
			num3 += 360f;
		}
		if (unsigned)
		{
			num3 = Mathf.Abs(num3);
		}
		return num3;
	}

	public static float Angle2DRad(Vector3 point1, Vector3 point2, float offset)
	{
		return Mathf.Atan2(point2.z - point1.z, point2.x - point1.x) + offset * (MathF.PI / 180f);
	}

	public static double Clamp(double n, double minValue, double maxValue)
	{
		if (n < minValue)
		{
			return minValue;
		}
		if (n > maxValue)
		{
			return maxValue;
		}
		return n;
	}

	public static double Clip(double n, double minValue, double maxValue)
	{
		if (n < minValue)
		{
			return minValue;
		}
		if (n > maxValue)
		{
			return maxValue;
		}
		return n;
	}

	public static string ColorToHex(Color32 color)
	{
		return color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
	}

	public static GameObject CreateGameObject(MonoBehaviour parent, string name)
	{
		return CreateGameObject(parent.gameObject, name, Vector3.zero);
	}

	public static GameObject CreateGameObject(GameObject parent, string name)
	{
		return CreateGameObject(parent, name, Vector3.zero);
	}

	public static GameObject CreateGameObject(GameObject parent, string name, Vector3 position)
	{
		GameObject gameObject = new GameObject(name);
		gameObject.transform.parent = parent.transform;
		gameObject.transform.localPosition = position;
		return gameObject;
	}

	public static void DeleteGameObject(Transform current, string name)
	{
		for (int num = current.childCount - 1; num >= 0; num--)
		{
			Transform child = current.GetChild(num);
			if (child.name == name)
			{
				UnityEngine.Object.DestroyImmediate(child.gameObject);
			}
			else
			{
				DeleteGameObject(child, name);
			}
		}
	}

	public static Vector2 DistanceBetweenPoints(Vector2 point1, Vector2 point2)
	{
		Vector2 vector = point1 - point2;
		double num = Math.Sin(point1.y * (MathF.PI / 180f));
		double num2 = Math.Sin(point2.y * (MathF.PI / 180f));
		double num3 = Math.Cos(point1.y * (MathF.PI / 180f));
		double num4 = Math.Cos(point2.y * (MathF.PI / 180f));
		double num5 = Math.Cos(vector.x * (MathF.PI / 180f));
		double num6 = Math.Abs(6371.0 * Math.Acos(num * num + num3 * num3 * num5));
		double num7 = Math.Abs(6371.0 * Math.Acos(num2 * num2 + num4 * num4 * num5));
		float x = (float)((num6 + num7) / 2.0);
		float y = (float)(6371.0 * Math.Acos(num * num2 + num3 * num4));
		return new Vector2(x, y);
	}

	public static void DistanceBetweenPoints(double x1, double y1, double x2, double y2, out double dx, out double dy)
	{
		double num = x1 - x2;
		double num2 = Math.Sin(y1 * 0.01745329238474369);
		double num3 = Math.Sin(y2 * 0.01745329238474369);
		double num4 = Math.Cos(y1 * 0.01745329238474369);
		double num5 = Math.Cos(y2 * 0.01745329238474369);
		double num6 = Math.Cos(num * 0.01745329238474369);
		double num7 = Math.Abs(6371.0 * Math.Acos(num2 * num2 + num4 * num4 * num6));
		double num8 = Math.Abs(6371.0 * Math.Acos(num3 * num3 + num5 * num5 * num6));
		dx = (num7 + num8) / 2.0;
		dy = 6371.0 * Math.Acos(num2 * num3 + num4 * num5);
	}

	public static void ExportMesh(string filename, params MeshFilter[] mfs)
	{
		StringBuilder stringBuilder = new StringBuilder();
		int num = 0;
		foreach (MeshFilter meshFilter in mfs)
		{
			Mesh sharedMesh = meshFilter.sharedMesh;
			Material[] sharedMaterials = meshFilter.GetComponent<Renderer>().sharedMaterials;
			stringBuilder.Append("g ").Append(meshFilter.name).Append("\n");
			for (int j = 0; j < sharedMesh.vertices.Length; j++)
			{
				Vector3 vector = sharedMesh.vertices[j];
				stringBuilder.Append("v ").Append(vector.x).Append(" ")
					.Append(vector.y)
					.Append(" ")
					.Append(vector.z)
					.Append("\n");
			}
			stringBuilder.Append("\n");
			for (int k = 0; k < sharedMesh.normals.Length; k++)
			{
				Vector3 vector2 = sharedMesh.normals[k];
				stringBuilder.Append("vn ").Append(vector2.x).Append(" ")
					.Append(vector2.y)
					.Append(" ")
					.Append(vector2.z)
					.Append("\n");
			}
			stringBuilder.Append("\n");
			for (int l = 0; l < sharedMesh.uv.Length; l++)
			{
				Vector2 vector3 = sharedMesh.uv[l];
				stringBuilder.Append("vt ").Append(vector3.x).Append(" ")
					.Append(vector3.y)
					.Append("\n");
			}
			for (int m = 0; m < sharedMesh.subMeshCount; m++)
			{
				stringBuilder.Append("\nusemtl ").Append(sharedMaterials[m].name).Append("\n");
				stringBuilder.Append("usemap ").Append(sharedMaterials[m].name).Append("\n");
				int[] triangles = sharedMesh.GetTriangles(m);
				for (int n = 0; n < triangles.Length; n += 3)
				{
					int value = triangles[n] + 1 + num;
					int value2 = triangles[n + 1] + 1 + num;
					int value3 = triangles[n + 2] + 1 + num;
					stringBuilder.Append("f ").Append(value).Append("/")
						.Append(value)
						.Append("/")
						.Append(value);
					stringBuilder.Append(" ").Append(value2).Append("/")
						.Append(value2)
						.Append("/")
						.Append(value2);
					stringBuilder.Append(" ").Append(value3).Append("/")
						.Append(value3)
						.Append("/")
						.Append(value3)
						.Append("\n");
				}
			}
			stringBuilder.Append("\n");
			num += sharedMesh.normals.Length;
		}
		StreamWriter streamWriter = new StreamWriter(filename);
		streamWriter.Write(stringBuilder.ToString());
		streamWriter.Close();
	}

	public static void GetCenterPointAndZoom(double[] positions, out Vector2 center, out int zoom)
	{
		double num = 3.4028234663852886E+38;
		double num2 = 3.4028234663852886E+38;
		double num3 = -3.4028234663852886E+38;
		double num4 = -3.4028234663852886E+38;
		for (int i = 0; i < positions.Length; i += 2)
		{
			double num5 = positions[i];
			double num6 = positions[i + 1];
			if (num5 < num)
			{
				num = num5;
			}
			if (num6 < num2)
			{
				num2 = num6;
			}
			if (num5 > num3)
			{
				num3 = num5;
			}
			if (num6 > num4)
			{
				num4 = num6;
			}
		}
		double num7 = num3 - num;
		double num8 = num4 - num2;
		double num9 = num7 / 2.0 + num;
		double num10 = num8 / 2.0 + num2;
		center = new Vector2((float)num9, (float)num10);
		int num11 = 1024;
		int num12 = 1024;
		float num13 = (float)num11 / 256f / 2f;
		float num14 = (float)num12 / 256f / 2f;
		for (int num15 = 20; num15 > 4; num15--)
		{
			bool flag = true;
			LatLongToTile(num9, num10, num15, out var tx, out var ty);
			for (int j = 0; j < positions.Length; j += 2)
			{
				double dx = positions[j];
				double dy = positions[j + 1];
				LatLongToTile(dx, dy, num15, out var tx2, out var ty2);
				tx2 -= tx - (double)num13;
				ty2 -= ty - (double)num14;
				if (tx2 < 0.0 || ty2 < 0.0 || tx2 > (double)num11 || ty2 > (double)num12)
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				zoom = num15;
				return;
			}
		}
		zoom = 3;
	}

	public static void GetCenterPointAndZoom(Vector2[] positions, out Vector2 center, out int zoom)
	{
		float num = float.MaxValue;
		float num2 = float.MaxValue;
		float num3 = float.MinValue;
		float num4 = float.MinValue;
		Vector2[] array = positions;
		for (int i = 0; i < array.Length; i++)
		{
			Vector2 vector = array[i];
			if (vector.x < num)
			{
				num = vector.x;
			}
			if (vector.y < num2)
			{
				num2 = vector.y;
			}
			if (vector.x > num3)
			{
				num3 = vector.x;
			}
			if (vector.y > num4)
			{
				num4 = vector.y;
			}
		}
		float num5 = num3 - num;
		float num6 = num4 - num2;
		double num7 = num5 / 2f + num;
		double num8 = num6 / 2f + num2;
		center = new Vector2((float)num7, (float)num8);
		int num9 = 1024;
		int num10 = 1024;
		float num11 = (float)num9 / 256f / 2f;
		float num12 = (float)num10 / 256f / 2f;
		for (int num13 = 20; num13 > 4; num13--)
		{
			bool flag = true;
			LatLongToTile(num7, num8, num13, out var tx, out var ty);
			array = positions;
			for (int i = 0; i < array.Length; i++)
			{
				Vector2 vector2 = array[i];
				LatLongToTile(vector2.x, vector2.y, num13, out var tx2, out var ty2);
				tx2 -= tx - (double)num11;
				ty2 -= ty - (double)num12;
				if (tx2 < 0.0 || ty2 < 0.0 || tx2 > (double)num9 || ty2 > (double)num10)
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				zoom = num13;
				return;
			}
		}
		zoom = 3;
	}

	public static long GetDirectorySize(DirectoryInfo folder)
	{
		return folder.GetFiles().Sum((FileInfo fi) => fi.Length) + folder.GetDirectories().Sum((DirectoryInfo dir) => GetDirectorySize(dir));
	}

	public static long GetDirectorySize(string folderPath)
	{
		return GetDirectorySize(new DirectoryInfo(folderPath));
	}

	public static long GetDirectorySizeMB(string folderPath)
	{
		return GetDirectorySize(folderPath) / 1048576;
	}

	public static Vector2 GetIntersectionPointOfTwoLines(Vector2 p11, Vector2 p12, Vector2 p21, Vector2 p22, out int state)
	{
		state = -2;
		Vector2 result = default(Vector2);
		float num = (p22.x - p21.x) * (p11.y - p21.y) - (p22.y - p21.y) * (p11.x - p21.x);
		float num2 = (p22.y - p21.y) * (p12.x - p11.x) - (p22.x - p21.x) * (p12.y - p11.y);
		float num3 = num / num2;
		if (num2 == 0f && num != 0f)
		{
			state = -1;
		}
		else if (num == 0f && num2 == 0f)
		{
			state = 0;
		}
		else
		{
			result.x = p11.x + num3 * (p12.x - p11.x);
			result.y = p11.y + num3 * (p12.y - p11.y);
			if ((result.x >= p11.x || result.x <= p11.x) && (result.x >= p21.x || result.x <= p21.x) && (result.y >= p11.y || result.y <= p11.y) && (result.y >= p21.y || result.y <= p21.y))
			{
				state = 1;
			}
		}
		return result;
	}

	public static Vector2 GetIntersectionPointOfTwoLines(Vector3 p11, Vector3 p12, Vector3 p21, Vector3 p22, out int state)
	{
		return GetIntersectionPointOfTwoLines(new Vector2(p11.x, p11.z), new Vector2(p12.x, p12.z), new Vector2(p21.x, p21.z), new Vector2(p22.x, p22.z), out state);
	}

	public static Rect GetRectFromPoints(List<Vector3> points)
	{
		return new Rect
		{
			x = points.Min((Vector3 p) => p.x),
			y = points.Min((Vector3 p) => p.z),
			xMax = points.Max((Vector3 p) => p.x),
			yMax = points.Max((Vector3 p) => p.z)
		};
	}

	public static Color HexToColor(string hex)
	{
		byte r = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
		byte g = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
		byte b = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
		return new Color32(r, g, b, byte.MaxValue);
	}

	public static bool IsClockWise(Vector3 A, Vector3 B, Vector3 C)
	{
		return (B.x - A.x) * (C.z - A.z) - (C.x - A.x) * (B.z - A.z) > 0f;
	}

	public static bool IsClockwise(Vector3[] points, int count)
	{
		double num = 0.0;
		for (int i = 0; i < count; i++)
		{
			Vector3 vector = points[i];
			Vector3 vector2 = points[(i + 1) % count];
			num += (double)((vector2.x - vector.x) * (vector2.z + vector.z));
		}
		return num > 0.0;
	}

	public static bool IsPointInPolygon(Vector3[] poly, float x, float y)
	{
		bool flag = false;
		int num = 0;
		int num2 = poly.Length - 1;
		while (num < poly.Length)
		{
			if (((poly[num].z <= y && y < poly[num2].z) || (poly[num2].z <= y && y < poly[num].z)) && x < (poly[num2].x - poly[num].x) * (y - poly[num].z) / (poly[num2].z - poly[num].z) + poly[num].x)
			{
				flag = !flag;
			}
			num2 = num++;
		}
		return flag;
	}

	public static bool IsPointInPolygon(Vector3[] poly, int length, float x, float y)
	{
		bool flag = false;
		int num = 0;
		int num2 = length - 1;
		while (num < length)
		{
			if (((poly[num].z <= y && y < poly[num2].z) || (poly[num2].z <= y && y < poly[num].z)) && x < (poly[num2].x - poly[num].x) * (y - poly[num].z) / (poly[num2].z - poly[num].z) + poly[num].x)
			{
				flag = !flag;
			}
			num2 = num++;
		}
		return flag;
	}

	public static bool IsPointInPolygon(List<Vector3> poly, float x, float y)
	{
		bool flag = false;
		int num = 0;
		int index = poly.Count - 1;
		while (num < poly.Count)
		{
			if (((poly[num].z <= y && y < poly[index].z) || (poly[index].z <= y && y < poly[num].z)) && x < (poly[index].x - poly[num].x) * (y - poly[num].z) / (poly[index].z - poly[num].z) + poly[num].x)
			{
				flag = !flag;
			}
			index = num++;
		}
		return flag;
	}

	public static bool IsPointInPolygon(List<Vector3> poly, Vector3 point)
	{
		return IsPointInPolygon(poly, point.x, point.z);
	}

	public static Vector2 LanLongToFlat(Vector2 pos)
	{
		return new Vector2(Mathf.FloorToInt(pos.x / 5f) * 5 + 180, 90 - Mathf.FloorToInt(pos.y / 5f) * 5);
	}

	public static void LatLongToMercat(ref double x, ref double y)
	{
		double num = Math.Sin(y * (Math.PI / 180.0));
		x = (x + 180.0) / 360.0;
		y = 0.5 - Math.Log((1.0 + num) / (1.0 - num)) / (Math.PI * 4.0);
	}

	public static void LatLongToMercat(double x, double y, out double mx, out double my)
	{
		double num = Math.Sin(y * (Math.PI / 180.0));
		mx = (x + 180.0) / 360.0;
		my = 0.5 - Math.Log((1.0 + num) / (1.0 - num)) / (Math.PI * 4.0);
	}

	public static void LatLongToTile(double dx, double dy, int zoom, out double tx, out double ty)
	{
		LatLongToMercat(ref dx, ref dy);
		uint num = (uint)(256 << zoom);
		double num2 = Clamp(dx * (double)num + 0.5, 0.0, num - 1);
		double num3 = Clamp(dy * (double)num + 0.5, 0.0, num - 1);
		tx = num2 / 256.0;
		ty = num3 / 256.0;
	}

	public static int Limit(int val, int min = 32, int max = 4096)
	{
		return Mathf.Clamp(val, min, max);
	}

	public static int LimitPowTwo(int val, int min = 32, int max = 4096)
	{
		return Mathf.Clamp(Mathf.ClosestPowerOfTwo(val), min, max);
	}

	public static void MercatToLatLong(double mx, double my, out double x, out double y)
	{
		uint num = 268435456u;
		double num2 = Clamp(mx * (double)num + 0.5, 0.0, num - 1);
		double num3 = Clamp(my * (double)num + 0.5, 0.0, num - 1);
		mx = num2 / 256.0;
		my = num3 / 256.0;
		TileToLatLong(mx, my, 20, out x, out y);
	}

	public static Vector2 NearestPointStrict(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
	{
		Vector2 vector = lineEnd - lineStart;
		Vector2 normalized = vector.normalized;
		float value = Vector2.Dot(point - lineStart, normalized) / Vector2.Dot(normalized, normalized);
		return lineStart + Mathf.Clamp(value, 0f, vector.magnitude) * normalized;
	}

	public static double Repeat(double n, double minValue, double maxValue)
	{
		if (double.IsInfinity(n) || double.IsInfinity(minValue) || double.IsInfinity(maxValue) || double.IsNaN(n) || double.IsNaN(minValue) || double.IsNaN(maxValue))
		{
			return n;
		}
		double num = maxValue - minValue;
		while (n < minValue || n > maxValue)
		{
			if (n < minValue)
			{
				n += num;
			}
			else if (n > maxValue)
			{
				n -= num;
			}
		}
		return n;
	}

	public static string ReplaceString(string str, string[] oldValues, string newValue)
	{
		foreach (string oldValue in oldValues)
		{
			str = str.Replace(oldValue, newValue);
		}
		return str;
	}

	public static string ReplaceString(string str, string[] oldValues, string[] newValues)
	{
		for (int i = 0; i < oldValues.Length; i++)
		{
			str = str.Replace(oldValues[i], newValues[i]);
		}
		return str;
	}

	public static void SafeDeleteDirectory(string directoryName)
	{
		try
		{
			Directory.Delete(directoryName, recursive: true);
		}
		catch
		{
		}
	}

	public static void SafeDeleteFile(string filename, int tryCount = 10)
	{
		while (tryCount-- > 0)
		{
			try
			{
				File.Delete(filename);
				break;
			}
			catch (Exception)
			{
				Thread.Sleep(10);
			}
		}
	}

	public static List<T> SpliceList<T>(List<T> list, int offset, int count = 1)
	{
		List<T> result = list.Skip(offset).Take(count).ToList();
		list.RemoveRange(offset, count);
		return result;
	}

	public static Color StringToColor(string str)
	{
		str = str.ToLower();
		switch (str)
		{
		case "black":
			return Color.black;
		case "blue":
			return Color.blue;
		case "cyan":
			return Color.cyan;
		case "gray":
			return Color.gray;
		case "green":
			return Color.green;
		case "magenta":
			return Color.magenta;
		case "red":
			return Color.red;
		case "white":
			return Color.white;
		case "yellow":
			return Color.yellow;
		default:
			try
			{
				string hex = (str + "000000").Substring(1, 6);
				byte[] array = (from x in Enumerable.Range(0, hex.Length)
					where x % 2 == 0
					select Convert.ToByte(hex.Substring(x, 2), 16)).ToArray();
				return new Color32(array[0], array[1], array[2], byte.MaxValue);
			}
			catch
			{
				return Color.white;
			}
		}
	}

	public static void TileToLatLong(double tx, double ty, int zoom, out double lx, out double ly)
	{
		double num = 256 << zoom;
		lx = 360.0 * (Repeat(tx * 256.0, 0.0, num - 1.0) / num - 0.5);
		ly = 90.0 - 360.0 * Math.Atan(Math.Exp((0.0 - (0.5 - Clamp(ty * 256.0, 0.0, num - 1.0) / num)) * 2.0 * Math.PI)) / Math.PI;
	}

	public static string TileToQuadKey(int x, int y, int zoom)
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int num = zoom; num > 0; num--)
		{
			char c = '0';
			int num2 = 1 << num - 1;
			if ((x & num2) != 0)
			{
				c = (char)(c + 1);
			}
			if ((y & num2) != 0)
			{
				c = (char)(c + 1);
				c = (char)(c + 1);
			}
			stringBuilder.Append(c);
		}
		return stringBuilder.ToString();
	}

	public static IEnumerable<int> Triangulate(List<Vector2> points)
	{
		List<int> list = new List<int>();
		int count = points.Count;
		if (count < 3)
		{
			return list;
		}
		int[] array = new int[count];
		if (TriangulateArea(points) > 0f)
		{
			for (int i = 0; i < count; i++)
			{
				array[i] = i;
			}
		}
		else
		{
			for (int j = 0; j < count; j++)
			{
				array[j] = count - 1 - j;
			}
		}
		int num = count;
		int num2 = 2 * num;
		int num3 = num - 1;
		while (num > 2)
		{
			if (num2-- <= 0)
			{
				return list;
			}
			int num4 = num3;
			if (num <= num4)
			{
				num4 = 0;
			}
			num3 = num4 + 1;
			if (num <= num3)
			{
				num3 = 0;
			}
			int num5 = num3 + 1;
			if (num <= num5)
			{
				num5 = 0;
			}
			if (TriangulateSnip(points, num4, num3, num5, num, array))
			{
				list.Add(array[num4]);
				list.Add(array[num3]);
				list.Add(array[num5]);
				int num6 = num3;
				for (int k = num3 + 1; k < num; k++)
				{
					array[num6] = array[k];
					num6++;
				}
				num--;
				num2 = 2 * num;
			}
		}
		list.Reverse();
		return list;
	}

	private static float TriangulateArea(List<Vector2> points)
	{
		int count = points.Count;
		float num = 0f;
		int index = count - 1;
		int num2 = 0;
		while (num2 < count)
		{
			Vector2 vector = points[index];
			Vector2 vector2 = points[num2];
			num += vector.x * vector2.y - vector2.x * vector.y;
			index = num2++;
		}
		return num * 0.5f;
	}

	private static bool TriangulateInsideTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 P)
	{
		float num = (C.x - B.x) * (P.y - B.y) - (C.y - B.y) * (P.x - B.x);
		float num2 = (B.x - A.x) * (P.y - A.y) - (B.y - A.y) * (P.x - A.x);
		float num3 = (A.x - C.x) * (P.y - C.y) - (A.y - C.y) * (P.x - C.x);
		if (num >= 0f && num3 >= 0f)
		{
			return num2 >= 0f;
		}
		return false;
	}

	private static bool TriangulateSnip(List<Vector2> points, int u, int v, int w, int n, int[] V)
	{
		Vector2 a = points[V[u]];
		Vector2 b = points[V[v]];
		Vector2 c = points[V[w]];
		if (Mathf.Epsilon > (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x))
		{
			return false;
		}
		for (int i = 0; i < n; i++)
		{
			if (i != u && i != v && i != w && TriangulateInsideTriangle(a, b, c, points[V[i]]))
			{
				return false;
			}
		}
		return true;
	}
}
