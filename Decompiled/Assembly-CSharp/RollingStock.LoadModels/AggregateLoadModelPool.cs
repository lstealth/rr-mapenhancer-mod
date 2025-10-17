using System;
using System.Collections.Generic;
using UnityEngine;

namespace RollingStock.LoadModels;

public class AggregateLoadModelPool : MonoBehaviour
{
	[SerializeField]
	private Texture2D meshHeightMapTexture;

	private readonly Dictionary<string, Mesh> _meshes = new Dictionary<string, Mesh>();

	[SerializeField]
	private Vector2Int meshHeightMapDimensions = new Vector2Int(4, 2);

	private static AggregateLoadModelPool _instance;

	public static AggregateLoadModelPool Shared
	{
		get
		{
			if (_instance == null)
			{
				_instance = UnityEngine.Object.FindObjectOfType<AggregateLoadModelPool>();
			}
			return _instance;
		}
	}

	private void OnDisable()
	{
		DestroyAllMeshes();
	}

	private void DestroyAllMeshes()
	{
		foreach (Mesh value in _meshes.Values)
		{
			UnityEngine.Object.Destroy(value);
		}
		_meshes.Clear();
	}

	public Mesh GetMesh(int atlasIndex, Vector2 intendedSize, int lod)
	{
		string key = KeyForMesh(atlasIndex, intendedSize, lod);
		if (_meshes.TryGetValue(key, out var value))
		{
			return value;
		}
		value = BuildMesh(atlasIndex, lod switch
		{
			0 => new Vector2Int(15, 47), 
			1 => new Vector2Int(4, 10), 
			_ => throw new ArgumentOutOfRangeException("lod", lod, null), 
		}, meshHeightMapTexture, meshHeightMapDimensions.x, meshHeightMapDimensions.y);
		value.name = key;
		_meshes[key] = value;
		return value;
	}

	private static string KeyForMesh(int atlasIndex, Vector2 intendedSize, int lod)
	{
		return $"{atlasIndex}-lod{lod}";
	}

	private static Texture2D ResizeSubsetOfTexture(Texture2D source, int sourceX, int sourceY, int sourceWidth, int sourceHeight, int targetWidth, int targetHeight)
	{
		Texture2D obj = new Texture2D(targetWidth, targetHeight, TextureFormat.R8, mipChain: false)
		{
			hideFlags = HideFlags.DontSave
		};
		RenderTexture temporary = RenderTexture.GetTemporary(targetWidth, targetHeight, 8, RenderTextureFormat.R8);
		Vector2 scale = new Vector2((float)sourceWidth / (float)source.width, (float)sourceHeight / (float)source.height);
		Vector2 offset = new Vector2((float)sourceX / (float)source.width, (float)sourceY / (float)source.height);
		RenderTexture active = RenderTexture.active;
		Graphics.Blit(source, temporary, scale, offset);
		RenderTexture.active = temporary;
		obj.ReadPixels(new Rect(0f, 0f, targetWidth, targetHeight), 0, 0);
		obj.Apply();
		RenderTexture.active = active;
		RenderTexture.ReleaseTemporary(temporary);
		return obj;
	}

	private static Mesh BuildMesh(int atlasIndex, Vector2Int vertexDimensions, Texture2D heightMapAtlas, int heightMapColumns, int heightMapRows)
	{
		int x = vertexDimensions.x;
		int y = vertexDimensions.y;
		int num = atlasIndex % heightMapColumns;
		int num2 = atlasIndex / heightMapColumns;
		int num3 = heightMapAtlas.width / heightMapColumns;
		int num4 = heightMapAtlas.height / heightMapRows;
		Texture2D texture2D = ResizeSubsetOfTexture(heightMapAtlas, num * num3, num2 * num4, num3, num4, x + 1, y + 1);
		Color[] pixels = texture2D.GetPixels();
		UnityEngine.Object.Destroy(texture2D);
		for (int i = 0; i <= x; i++)
		{
			int num5 = 0;
			pixels[i + num5 * (x + 1)] = Color.black;
			num5 = y;
			pixels[i + num5 * (x + 1)] = Color.black;
		}
		for (int j = 0; j <= y; j++)
		{
			int num6 = 0;
			pixels[num6 + j * (x + 1)] = Color.black;
			num6 = x;
			pixels[num6 + j * (x + 1)] = Color.black;
		}
		Vector3[] array = new Vector3[(x + 1) * (y + 1)];
		Vector2[] array2 = new Vector2[array.Length];
		int[] array3 = new int[x * y * 6];
		int num7 = 0;
		int num8 = 0;
		for (int k = 0; k <= y; k++)
		{
			for (int l = 0; l <= x; l++)
			{
				float num9 = (float)l / (float)x;
				float num10 = (float)k / (float)y;
				float r = pixels[l + k * (x + 1)].r;
				array[num7] = new Vector3(num9 - 0.5f, r, num10 - 0.5f);
				array2[num7] = new Vector2((float)l / (float)x, (float)k / (float)y);
				if (l < x && k < y)
				{
					array3[num8] = num7;
					array3[num8 + 1] = num7 + x + 1;
					array3[num8 + 2] = num7 + 1;
					array3[num8 + 3] = num7 + 1;
					array3[num8 + 4] = num7 + x + 1;
					array3[num8 + 5] = num7 + x + 2;
					num8 += 6;
				}
				num7++;
			}
		}
		Mesh mesh = new Mesh();
		mesh.name = "Aggregate Mesh";
		mesh.vertices = array;
		mesh.uv = array2;
		mesh.triangles = array3;
		mesh.RecalculateNormals();
		mesh.RecalculateTangents();
		mesh.RecalculateBounds();
		return mesh;
	}
}
