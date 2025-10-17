using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace InfinityCode.RealWorldTerrain;

[AddComponentMenu("")]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RealWorldTerrainBuilding : MonoBehaviour
{
	public float baseHeight;

	public Vector3[] baseVertices;

	public RealWorldTerrainContainer container;

	public string id;

	public bool invertRoof;

	public bool invertWall;

	public float roofHeight;

	public RealWorldTerrainRoofType roofType;

	public bool generateWall;

	public Material roofMaterial;

	public float startHeight;

	public Vector2 tileSize = new Vector2(30f, 30f);

	public Vector2 uvOffset = Vector2.zero;

	public Material wallMaterial;

	private MeshFilter _meshFilter;

	public MeshFilter meshFilter
	{
		get
		{
			if (_meshFilter == null)
			{
				_meshFilter = GetComponent<MeshFilter>();
			}
			return _meshFilter;
		}
	}

	[Obsolete("Use meshFilter instead.")]
	public MeshFilter roof => meshFilter;

	[Obsolete("Use meshFilter instead.")]
	public MeshFilter wall => meshFilter;

	private void CreateRoofDome(List<Vector3> vertices, List<int> triangles)
	{
		Vector3 zero = Vector3.zero;
		zero = vertices.Aggregate(zero, (Vector3 current, Vector3 point) => current + point) / vertices.Count;
		zero.y = (baseHeight + roofHeight) * container.scale.y;
		int count = vertices.Count;
		for (int num = 0; num < vertices.Count; num++)
		{
			int num2 = num;
			int num3 = num + 1;
			if (num3 >= vertices.Count)
			{
				num3 -= vertices.Count;
			}
			triangles.AddRange(new int[3] { num2, num3, count });
		}
		vertices.Add(zero);
	}

	private void CreateRoofMesh(List<Vector3> vertices, out List<Vector2> uv, out List<int> triangles)
	{
		List<Vector2> roofPoints = CreateRoofVertices(vertices);
		triangles = CreateRoofTriangles(vertices, roofPoints);
		if (invertRoof)
		{
			triangles.Reverse();
		}
		float minX = vertices.Min((Vector3 p) => p.x);
		float minZ = vertices.Min((Vector3 p) => p.z);
		float num = vertices.Max((Vector3 p) => p.x);
		float num2 = vertices.Max((Vector3 p) => p.z);
		float offX = num - minX;
		float offZ = num2 - minZ;
		uv = vertices.Select((Vector3 v) => new Vector2((v.x - minX) / offX, (v.z - minZ) / offZ)).ToList();
	}

	private List<int> CreateRoofTriangles(List<Vector3> vertices, List<Vector2> roofPoints)
	{
		List<int> list = new List<int>();
		if (roofType == RealWorldTerrainRoofType.flat)
		{
			int[] array = RealWorldTerrainTriangulator.Triangulate(roofPoints);
			if (array != null)
			{
				list.AddRange(array);
			}
		}
		else if (roofType == RealWorldTerrainRoofType.dome)
		{
			CreateRoofDome(vertices, list);
		}
		return list;
	}

	private List<Vector2> CreateRoofVertices(List<Vector3> vertices)
	{
		Vector3[] array = new Vector3[baseVertices.Length];
		Array.Copy(baseVertices, array, baseVertices.Length);
		if (container.prefs.buildingBottomMode == RealWorldTerrainBuildingBottomMode.followTerrain)
		{
			Vector3 position = base.transform.position;
			RealWorldTerrainItem itemByWorldPosition = container.GetItemByWorldPosition(baseVertices[0] + position);
			if (itemByWorldPosition != null)
			{
				TerrainData terrainData = itemByWorldPosition.terrainData;
				Vector3 vector = position - itemByWorldPosition.transform.position;
				for (int i = 0; i < array.Length; i++)
				{
					Vector3 vector2 = array[i];
					Vector3 vector3 = vector + vector2;
					float interpolatedHeight = terrainData.GetInterpolatedHeight(vector3.x / terrainData.size.x, vector3.z / terrainData.size.z);
					vector2.y = itemByWorldPosition.transform.position.y + interpolatedHeight - position.y;
					array[i] = vector2;
				}
			}
		}
		List<Vector2> list = new List<Vector2>();
		float y = array.Max((Vector3 v) => v.y) + baseHeight * container.scale.y;
		Vector3[] array2 = array;
		for (int num = 0; num < array2.Length; num++)
		{
			Vector3 vector4 = array2[num];
			Vector3 item = new Vector3(vector4.x, y, vector4.z);
			Vector2 item2 = new Vector2(vector4.x, vector4.z);
			vertices.Add(item);
			list.Add(item2);
		}
		return list;
	}

	private void CreateWallMesh(List<Vector3> vertices, List<Vector2> uv, out List<int> triangles)
	{
		List<Vector3> list = new List<Vector3>();
		List<Vector2> list2 = new List<Vector2>();
		bool flag = CreateWallVertices(list, list2);
		if (invertWall)
		{
			flag = !flag;
		}
		triangles = CreateWallTriangles(list, vertices.Count, flag);
		vertices.AddRange(list);
		uv.AddRange(list2);
	}

	private List<int> CreateWallTriangles(List<Vector3> vertices, int offset, bool reversed)
	{
		List<int> list = new List<int>();
		for (int i = 0; i < vertices.Count / 4; i++)
		{
			int num = i * 4;
			int num2 = i * 4 + 2;
			int num3 = i * 4 + 3;
			int num4 = i * 4 + 1;
			if (num2 >= vertices.Count)
			{
				num2 -= vertices.Count;
			}
			if (num3 >= vertices.Count)
			{
				num3 -= vertices.Count;
			}
			num += offset;
			num2 += offset;
			num3 += offset;
			num4 += offset;
			if (reversed)
			{
				list.AddRange(new int[6] { num, num4, num3, num, num3, num2 });
			}
			else
			{
				list.AddRange(new int[6] { num2, num3, num, num3, num4, num });
			}
		}
		return list;
	}

	private bool CreateWallVertices(List<Vector3> vertices, List<Vector2> uv)
	{
		Vector3[] array = new Vector3[baseVertices.Length];
		Array.Copy(baseVertices, array, baseVertices.Length);
		if (container.prefs.buildingBottomMode == RealWorldTerrainBuildingBottomMode.followTerrain)
		{
			Vector3 position = base.transform.position;
			RealWorldTerrainItem itemByWorldPosition = container.GetItemByWorldPosition(baseVertices[0] + position);
			if (itemByWorldPosition != null)
			{
				TerrainData terrainData = itemByWorldPosition.terrainData;
				Vector3 vector = position - itemByWorldPosition.transform.position;
				for (int i = 0; i < array.Length; i++)
				{
					Vector3 vector2 = array[i];
					Vector3 vector3 = vector + vector2;
					float interpolatedHeight = terrainData.GetInterpolatedHeight(vector3.x / terrainData.size.x, vector3.z / terrainData.size.z);
					vector2.y = itemByWorldPosition.transform.position.y + interpolatedHeight - position.y;
					array[i] = vector2;
				}
			}
		}
		float num = array.Max((Vector3 v) => v.y) + baseHeight * container.scale.y;
		float num2 = startHeight * container.scale.y;
		float num3 = ((num2 < 0f) ? num2 : 0f);
		for (int num4 = 0; num4 < array.Length; num4++)
		{
			Vector3 item = array[num4];
			Vector3 item2 = ((num4 < array.Length - 1) ? array[num4 + 1] : array[0]);
			if (item.y < num2)
			{
				item.y = num2;
			}
			if (item2.y < num2)
			{
				item2.y = num2;
			}
			item.y += num3;
			item2.y += num3;
			vertices.Add(item);
			vertices.Add(new Vector3(item.x, num, item.z));
			vertices.Add(item2);
			vertices.Add(new Vector3(item2.x, num, item2.z));
		}
		float num5 = 0f;
		float num6 = float.MaxValue;
		for (int num7 = 0; num7 < vertices.Count / 4; num7++)
		{
			int index = Mathf.RoundToInt(Mathf.Repeat(num7 * 4, vertices.Count));
			int index2 = Mathf.RoundToInt(Mathf.Repeat((num7 + 1) * 4, vertices.Count));
			Vector3 vector4 = vertices[index];
			Vector3 vector5 = vertices[index2];
			vector4.y = (vector5.y = 0f);
			num5 += (vector4 - vector5).magnitude;
			if (num6 > array[num7].y)
			{
				num6 = array[num7].y;
			}
		}
		Vector3 vector6 = vertices[vertices.Count - 4];
		Vector3 vector7 = vertices[0];
		vector6.y = (vector7.y = 0f);
		num5 += (vector6 - vector7).magnitude;
		float num8 = 0f;
		float num9 = 0f;
		float num10 = num5 / tileSize.x;
		float num11 = num / tileSize.y;
		float num12 = container.scale.y * tileSize.y;
		for (int num13 = 0; num13 < vertices.Count / 4; num13++)
		{
			int index3 = Mathf.RoundToInt(Mathf.Repeat(num13 * 4, vertices.Count));
			int index4 = Mathf.RoundToInt(Mathf.Repeat((num13 + 1) * 4, vertices.Count));
			float num14 = num9;
			uv.Add(new Vector2(num14 * num10 + uvOffset.x, (vertices[num13 * 4].y - num6) / num12 + uvOffset.y));
			uv.Add(new Vector2(num14 * num10 + uvOffset.x, num11 + uvOffset.y));
			Vector3 vector8 = vertices[index3];
			Vector3 vector9 = vertices[index4];
			vector8.y = (vector9.y = 0f);
			num8 += (vector8 - vector9).magnitude;
			num9 = num8 / num5;
			uv.Add(new Vector2(num9 * num10 + uvOffset.x, (vertices[num13 * 4 + 2].y - num6) / num12 + uvOffset.y));
			uv.Add(new Vector2(num9 * num10 + uvOffset.x, num11 + uvOffset.y));
		}
		int num15 = -1;
		float num16 = float.MaxValue;
		for (int num17 = 0; num17 < array.Length; num17++)
		{
			if (array[num17].z < num16)
			{
				num16 = array[num17].z;
				num15 = num17;
			}
		}
		int num18 = num15 - 1;
		if (num18 < 0)
		{
			num18 = array.Length - 1;
		}
		int num19 = num15 + 1;
		if (num19 >= array.Length)
		{
			num19 = 0;
		}
		float num20 = RealWorldTerrainUtils.Angle2D(array[num15], array[num19]);
		float num21 = RealWorldTerrainUtils.Angle2D(array[num15], array[num18]);
		return num20 < num21;
	}

	public void Generate()
	{
		Mesh mesh;
		if (meshFilter.sharedMesh != null)
		{
			mesh = meshFilter.sharedMesh;
		}
		else
		{
			mesh = new Mesh();
			mesh.name = "Building " + id;
			mesh.subMeshCount = 2;
			meshFilter.sharedMesh = mesh;
		}
		List<Vector3> vertices = new List<Vector3>();
		List<int> triangles = null;
		CreateRoofMesh(vertices, out var uv, out var triangles2);
		if (generateWall)
		{
			CreateWallMesh(vertices, uv, out triangles);
		}
		mesh.SetVertices(vertices);
		mesh.SetUVs(0, uv);
		mesh.SetTriangles(triangles2, 0);
		if (generateWall)
		{
			mesh.SetTriangles(triangles, 1);
		}
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		GetComponent<MeshRenderer>().materials = new Material[2] { roofMaterial, wallMaterial };
	}
}
