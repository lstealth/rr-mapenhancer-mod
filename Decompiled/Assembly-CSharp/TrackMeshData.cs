using UnityEngine;

public static class TrackMeshData
{
	internal struct EndCapData
	{
		public Vector3[] Vertices;

		public Vector2[] UVs;

		public int[] Triangles;

		public EndCapData(Vector3[] vertices, Vector2[] uVs, int[] triangles)
		{
			Vertices = vertices;
			UVs = uVs;
			Triangles = triangles;
		}
	}

	internal static EndCapData endCapData = new EndCapData(new Vector3[22]
	{
		new Vector3(0.07841457f, -0.1772163f, 0f),
		new Vector3(0.0782f, -0.1665f, 0f),
		new Vector3(0.0255f, -0.1515f, 0f),
		new Vector3(0f, -0.1772163f, 0f),
		new Vector3(0.0135f, -0.1401101f, 0f),
		new Vector3(0f, -3.238033E-05f, 0f),
		new Vector3(0.0095f, -0.07059012f, 0f),
		new Vector3(0.01219803f, -0.04320481f, 0f),
		new Vector3(0.03820186f, -0.03295477f, 0f),
		new Vector3(0.03605155f, -0.005019967f, 0f),
		new Vector3(0.02986189f, -0.000997003f, 0f),
		new Vector3(-0.0782f, -0.1665f, 0f),
		new Vector3(-0.07841457f, -0.1772163f, 0f),
		new Vector3(0f, -0.1772163f, 0f),
		new Vector3(-0.0255f, -0.1515f, 0f),
		new Vector3(-0.0135f, -0.1401101f, 0f),
		new Vector3(0f, -3.238033E-05f, 0f),
		new Vector3(-0.0095f, -0.07059012f, 0f),
		new Vector3(-0.01219803f, -0.04320481f, 0f),
		new Vector3(-0.03820186f, -0.03295477f, 0f),
		new Vector3(-0.03605155f, -0.005019967f, 0f),
		new Vector3(-0.02986189f, -0.000997003f, 0f)
	}, new Vector2[22]
	{
		new Vector2(0.4503886f, 0.01094368f),
		new Vector2(0.4508587f, 0.02260903f),
		new Vector2(0.5132077f, 0.03878839f),
		new Vector2(0.5421237f, 0.01094374f),
		new Vector2(0.5273842f, 0.05435317f),
		new Vector2(0.5421237f, 0.2182273f),
		new Vector2(0.5311398f, 0.1356836f),
		new Vector2(0.5278535f, 0.1677209f),
		new Vector2(0.4974323f, 0.1797121f),
		new Vector2(0.4999478f, 0.2123924f),
		new Vector2(0.507189f, 0.2170988f),
		new Vector2(0.4508587f, 0.02260903f),
		new Vector2(0.4503886f, 0.01094368f),
		new Vector2(0.5421237f, 0.01094374f),
		new Vector2(0.5132077f, 0.03878839f),
		new Vector2(0.5273842f, 0.05435317f),
		new Vector2(0.5421237f, 0.2182273f),
		new Vector2(0.5311398f, 0.1356836f),
		new Vector2(0.5278535f, 0.1677209f),
		new Vector2(0.4974323f, 0.1797121f),
		new Vector2(0.4999478f, 0.2123924f),
		new Vector2(0.507189f, 0.2170988f)
	}, new int[54]
	{
		0, 1, 2, 3, 0, 2, 2, 4, 3, 4,
		5, 3, 4, 6, 5, 6, 7, 5, 7, 8,
		5, 8, 9, 5, 9, 10, 5, 11, 12, 13,
		13, 14, 11, 13, 15, 14, 13, 16, 15, 16,
		17, 15, 16, 18, 17, 16, 19, 18, 16, 20,
		19, 16, 21, 20
	});
}
