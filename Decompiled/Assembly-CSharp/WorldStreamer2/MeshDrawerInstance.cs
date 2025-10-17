using System.Collections.Generic;
using UnityEngine;

namespace WorldStreamer2;

[ExecuteInEditMode]
public class MeshDrawerInstance : MonoBehaviour
{
	public bool noFrustrum = true;

	public MeshDistance[] meshDistances;

	public Matrix4x4[] matrices;

	public List<Dictionary<Vector3Int, Cell>> cells;

	private void Start()
	{
		GenerateCells();
	}

	private void GenerateCells()
	{
		cells = new List<Dictionary<Vector3Int, Cell>>();
		for (int i = 0; i < meshDistances.Length; i++)
		{
			cells.Add(new Dictionary<Vector3Int, Cell>(new Vector3IntArrayComparer()));
			float cellSize = meshDistances[i].cellSize;
			for (int j = 0; j < matrices.Length; j++)
			{
				Vector3 point = matrices[j].GetColumn(3);
				int x = Mathf.FloorToInt(point.x / cellSize);
				int z = Mathf.FloorToInt(point.z / cellSize);
				Vector3Int key = new Vector3Int(x, 0, z);
				if (cells[i].ContainsKey(key))
				{
					cells[i][key].matrices.Add(matrices[j]);
					cells[i][key].bounds.Encapsulate(point);
					if (cells[i][key].size.x > point.x)
					{
						cells[i][key].size.x = point.x;
					}
					if (cells[i][key].size.y < point.x)
					{
						cells[i][key].size.y = point.x;
					}
					if (cells[i][key].size.z > point.z)
					{
						cells[i][key].size.z = point.z;
					}
					if (cells[i][key].size.w < point.z)
					{
						cells[i][key].size.w = point.z;
					}
					continue;
				}
				cells[i].Add(key, new Cell
				{
					matrices = new List<Matrix4x4>(),
					size = new Vector4(float.MaxValue, float.MinValue, float.MaxValue, float.MinValue),
					bounds = default(Bounds)
				});
				cells[i][key].matrices.Add(matrices[j]);
				cells[i][key].bounds.Encapsulate(point);
				if (cells[i][key].size.x > point.x)
				{
					cells[i][key].size.x = point.x;
				}
				if (cells[i][key].size.y < point.x)
				{
					cells[i][key].size.y = point.x;
				}
				if (cells[i][key].size.z > point.z)
				{
					cells[i][key].size.z = point.z;
				}
				if (cells[i][key].size.w < point.z)
				{
					cells[i][key].size.w = point.z;
				}
			}
			foreach (Vector3Int key2 in cells[i].Keys)
			{
				cells[i][key2].matricesArray = cells[i][key2].matrices.ToArray();
			}
		}
	}

	private void Update()
	{
		Camera main = Camera.main;
		Vector3 position = main.transform.position;
		if (meshDistances == null)
		{
			return;
		}
		if (cells == null || cells.Count == 0)
		{
			GenerateCells();
			return;
		}
		Plane[] planes = GeometryUtility.CalculateFrustumPlanes(main);
		for (int i = 0; i < meshDistances.Length; i++)
		{
			if (!meshDistances[i].on)
			{
				continue;
			}
			float cellSize = meshDistances[i].cellSize;
			int num = Mathf.FloorToInt(meshDistances[i].distance / cellSize) + 1;
			if (num > 100)
			{
				break;
			}
			int num2 = 0;
			if (i > 0)
			{
				num2 = Mathf.FloorToInt(meshDistances[i - 1].distance * 2f / cellSize);
			}
			int num3 = Mathf.FloorToInt(position.x / cellSize);
			int num4 = Mathf.FloorToInt(position.z / cellSize);
			Vector3Int key = new Vector3Int(num3, 0, num4);
			for (int j = num3 - num; j < num3 + num; j++)
			{
				for (int k = num4 - num; k < num4 + num; k++)
				{
					key.x = j;
					key.z = k;
					if ((Mathf.Abs(j - num3) < num2 && Mathf.Abs(k - num4) < num2) || !cells[i].ContainsKey(key))
					{
						continue;
					}
					for (int l = 0; l < meshDistances[i].meshMaterials.Count; l++)
					{
						MeshMaterials meshMaterials = meshDistances[i].meshMaterials[l];
						if (GeometryUtility.TestPlanesAABB(planes, cells[i][key].bounds) || noFrustrum)
						{
							for (int m = 0; m < meshMaterials.materials.Length; m++)
							{
								Graphics.DrawMeshInstanced(meshMaterials.mesh, m, meshMaterials.materials[m], cells[i][key].matricesArray);
							}
						}
					}
				}
			}
		}
	}

	private void OnDrawGizmosSelected()
	{
		Vector3 position = Camera.main.transform.position;
		Gizmos.color = Color.red;
		Gizmos.DrawSphere(position, 5f);
		Color[] array = new Color[5]
		{
			Color.red,
			Color.green,
			Color.blue,
			Color.cyan,
			Color.magenta
		};
		int num = meshDistances.Length - 1;
		while (0 <= num)
		{
			Gizmos.color = Color.yellow;
			if (meshDistances[num].on)
			{
				Gizmos.DrawWireCube(position, Vector3.one * meshDistances[num].distance);
				float cellSize = meshDistances[num].cellSize;
				int num2 = Mathf.FloorToInt(meshDistances[num].distance / cellSize) + 1;
				if (num2 > 20)
				{
					break;
				}
				int num3 = 0;
				if (num > 0)
				{
					num3 = Mathf.FloorToInt(meshDistances[num - 1].distance * 2f / cellSize);
				}
				int num4 = Mathf.FloorToInt(position.x / cellSize);
				int num5 = Mathf.FloorToInt(position.z / cellSize);
				Vector2Int vector2Int = new Vector2Int(num4, num5);
				for (int i = num4 - num2; i < num4 + num2; i++)
				{
					for (int j = num5 - num2; j < num5 + num2; j++)
					{
						vector2Int.x = i;
						vector2Int.y = j;
						Gizmos.color = array[num];
						if (Mathf.Abs(i - num4) >= num3 || Mathf.Abs(j - num5) >= num3)
						{
							Gizmos.DrawWireCube(new Vector3((float)i * cellSize + cellSize * 0.5f, position.y, (float)j * cellSize + cellSize * 0.5f), Vector3.one * cellSize);
						}
					}
				}
			}
			num--;
		}
	}
}
