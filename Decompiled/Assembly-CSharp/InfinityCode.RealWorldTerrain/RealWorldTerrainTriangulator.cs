using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace InfinityCode.RealWorldTerrain;

public class RealWorldTerrainTriangulator
{
	private class Point
	{
		public bool isExternal;

		public Point next;

		public Point prev;

		public int pindex;

		public float weight;

		private int index;

		private float x;

		private float y;

		public Point(int index, float x, float y)
		{
			weight = 0f;
			this.index = index;
			this.x = x;
			this.y = y;
		}

		public Point(int index, Vector3 p)
			: this(index, p.x, p.z)
		{
		}

		public void Dispose()
		{
			next = null;
			prev = null;
		}

		private bool EqualTo(Point p)
		{
			if (Math.Abs(p.x - x) < float.Epsilon)
			{
				return Math.Abs(p.y - y) < float.Epsilon;
			}
			return false;
		}

		public bool HasIntersections()
		{
			Point point = prev;
			Point point2 = next;
			float num = point2.x - point.x;
			float num2 = point2.y - point.y;
			Point point3 = point2.next;
			for (Point point4 = point3.next; point4 != point; point4 = point4.next)
			{
				float num3 = (point4.y - point3.y) * num - (point4.x - point3.x) * num2;
				if (num3 > 0f)
				{
					float num4 = (point4.x - point3.x) * (point.y - point3.y) - (point4.y - point3.y) * (point.x - point3.x);
					float num5 = num * (point.y - point3.y) - num2 * (point.x - point3.x);
					if (num4 >= 0f && num4 <= num3 && num5 >= 0f && num5 <= num3 && !point.EqualTo(point3) && !point.EqualTo(point4) && !point2.EqualTo(point3) && !point2.EqualTo(point4))
					{
						return true;
					}
				}
				point3 = point4;
			}
			return false;
		}

		public bool IsExternal(bool clockwise)
		{
			Point point = prev;
			Point point2 = next;
			isExternal = ((point2.x - point.x) * (y - point.y) - (point2.y - point.y) * (x - point.x) >= 0f) ^ clockwise;
			return isExternal;
		}

		public void SetPrev(Point other)
		{
			if (other != null)
			{
				prev = other;
				other.next = this;
			}
		}

		public override string ToString()
		{
			if (prev == null)
			{
				return "Point i:" + index + ". Disposed";
			}
			return "Point i:" + index + ", p:" + prev.index + ", n:" + next.index + ", w:" + weight + ", pi:" + pindex;
		}

		public void UpdateWeight()
		{
			float num = prev.x;
			float num2 = prev.y;
			float num3 = next.x;
			float num4 = next.y;
			float num5 = num - x;
			float num6 = num2 - y;
			float num7 = num3 - x;
			float num8 = num4 - y;
			float num9 = num3 - num;
			float num10 = num4 - num2;
			float num11 = (float)Math.Sqrt(num5 * num5 + num6 * num6);
			float num12 = (float)Math.Sqrt(num7 * num7 + num8 * num8);
			float num13 = (float)Math.Sqrt(num9 * num9 + num10 * num10);
			float num14 = (num11 + num12 + num13) / 2f;
			weight = num14 * (num14 - num11) * (num14 - num12) * (num14 - num13);
		}

		public void WriteToResult(int[] results, ref int rindex)
		{
			results[rindex++] = index;
			results[rindex++] = next.index;
			results[rindex++] = prev.index;
		}
	}

	private static void AddHole(List<Vector3> input, List<Vector3> hole)
	{
		if (hole == null || hole.Count < 3)
		{
			return;
		}
		float num = float.MaxValue;
		int num2 = -1;
		int num3 = -1;
		int count = hole.Count;
		float num4 = float.MaxValue;
		float num5 = float.MinValue;
		float num6 = float.MaxValue;
		float num7 = float.MinValue;
		for (int i = 0; i < count; i++)
		{
			Vector3 vector = hole[i];
			float x = vector.x;
			float z = vector.z;
			if (x < num4)
			{
				num4 = x;
			}
			if (x > num5)
			{
				num5 = x;
			}
			if (z < num6)
			{
				num6 = z;
			}
			if (z > num7)
			{
				num7 = z;
			}
		}
		float num8 = (num5 + num4) / 2f;
		float num9 = (num7 + num6) / 2f;
		for (int j = 0; j < input.Count; j++)
		{
			Vector3 vector2 = input[j];
			float x2 = vector2.x;
			float z2 = vector2.z;
			float num10 = (x2 - num8) * (x2 - num8) + (z2 - num9) * (z2 - num9);
			if (num10 < num)
			{
				num = num10;
				num2 = j;
			}
		}
		num8 = input[num2].x;
		num9 = input[num2].z;
		num = float.MaxValue;
		for (int k = 0; k < count; k++)
		{
			Vector3 vector3 = hole[k];
			float x3 = vector3.x;
			float z3 = vector3.z;
			float num11 = (x3 - num8) * (x3 - num8) + (z3 - num9) * (z3 - num9);
			if (num11 < num)
			{
				num = num11;
				num3 = k;
			}
		}
		int num12 = count - num3;
		input.Insert(num2, input[num2]);
		num2++;
		input.InsertRange(num2, hole.Skip(num3).Take(num12));
		input.InsertRange(num2 + num12, hole.Take(num3 + 1));
	}

	private static void AddHoles(List<Vector3> input, List<List<Vector3>> holes)
	{
		if (holes == null)
		{
			return;
		}
		int num = 0;
		foreach (List<Vector3> hole in holes)
		{
			if (hole != null && hole.Count >= 3)
			{
				num += hole.Count + 1;
			}
		}
		if (input.Capacity < input.Count + num)
		{
			input.Capacity = input.Count + num;
		}
		foreach (List<Vector3> hole2 in holes)
		{
			AddHole(input, hole2);
		}
	}

	private static int[] GenerateTriangles(Point[] points, bool clockwise)
	{
		int num = points.Length;
		int num2 = num;
		int[] array = new int[(num2 - 2) * 3];
		for (int i = 0; i < num2; i++)
		{
			points[i].UpdateWeight();
		}
		Sort(points, 0, num - 1);
		for (int j = 0; j < num2; j++)
		{
			points[j].pindex = j;
		}
		int rindex = 0;
		int num3 = 0;
		int num4 = num3;
		while (num > 2)
		{
			bool flag = true;
			for (int k = num4; k < num2; k++)
			{
				Point point = points[k];
				if (point.isExternal || point.IsExternal(clockwise) || (num > 4 && point.HasIntersections()))
				{
					continue;
				}
				num4 = k + 1;
				point.WriteToResult(array, ref rindex);
				Point next = point.next;
				Point prev = point.prev;
				num--;
				point.Dispose();
				next.SetPrev(prev);
				if (num > 3)
				{
					next.isExternal = (prev.isExternal = false);
					for (int num5 = k; num5 > num3; num5--)
					{
						(points[num5] = points[num5 - 1]).pindex = num5;
					}
					num3++;
					int num6 = UpdateWeight(points, prev, num3, num2);
					if (num4 > num6)
					{
						num4 = num6;
					}
					num6 = UpdateWeight(points, next, num3, num2);
					if (num4 > num6)
					{
						num4 = num6;
					}
				}
				else
				{
					next.WriteToResult(array, ref rindex);
					prev.Dispose();
					next.Dispose();
					num--;
				}
				flag = false;
				break;
			}
			if (flag)
			{
				return null;
			}
		}
		return array;
	}

	private static void Sort(Point[] points, int left, int right)
	{
		int i = left;
		int num = right;
		float weight = points[(left + right) / 2].weight;
		while (i <= num)
		{
			for (; points[i].weight < weight; i++)
			{
			}
			while (points[num].weight > weight)
			{
				num--;
			}
			if (i <= num)
			{
				Point point = points[i];
				points[i] = points[num];
				points[num] = point;
				i++;
				num--;
			}
		}
		if (left < num)
		{
			Sort(points, left, num);
		}
		if (i < right)
		{
			Sort(points, i, right);
		}
	}

	public static int[] Triangulate(List<Vector2> input, List<List<Vector3>> holes = null, bool clockwise = true)
	{
		if (input == null)
		{
			return null;
		}
		if (input.Count < 3)
		{
			return null;
		}
		return Triangulate(input.Select((Vector2 i) => new Vector3(i.x, 0f, i.y)).ToList(), holes, clockwise);
	}

	public static int[] Triangulate(List<Vector3> input, List<List<Vector3>> holes = null, bool clockwise = true)
	{
		if (input == null)
		{
			return null;
		}
		if (input.Count < 3)
		{
			return null;
		}
		AddHoles(input, holes);
		int count = input.Count;
		switch (count)
		{
		case 3:
			return new int[3] { 0, 1, 2 };
		case 4:
			return new int[6] { 0, 1, 2, 0, 2, 3 };
		default:
		{
			Point[] array = new Point[count];
			Point prev = null;
			for (int i = 0; i < count; i++)
			{
				Point point = new Point(i, input[i]);
				point.SetPrev(prev);
				array[i] = point;
				prev = point;
			}
			array[0].SetPrev(prev);
			return GenerateTriangles(array, clockwise);
		}
		}
	}

	private static int UpdateWeight(Point[] points, Point point, int start, int total)
	{
		float weight = point.weight;
		point.UpdateWeight();
		float weight2 = point.weight;
		int pindex = point.pindex;
		int num = pindex;
		if (weight2 < weight)
		{
			for (pindex--; pindex >= start; pindex--)
			{
				Point point2 = points[pindex];
				if (point2.weight < weight2)
				{
					points[num] = point;
					break;
				}
				points[num] = point2;
				point2.pindex = num;
				num = pindex;
			}
			if (num == start)
			{
				points[num] = point;
			}
		}
		else
		{
			for (pindex++; pindex < total; pindex++)
			{
				Point point3 = points[pindex];
				if (point3.weight > weight2)
				{
					points[num] = point;
					break;
				}
				points[num] = point3;
				point3.pindex = num;
				num = pindex;
			}
			if (pindex == total)
			{
				points[num] = point;
			}
		}
		point.pindex = num;
		return num;
	}
}
