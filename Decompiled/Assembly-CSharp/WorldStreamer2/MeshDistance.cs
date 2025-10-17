using System;
using System.Collections.Generic;

namespace WorldStreamer2;

[Serializable]
public class MeshDistance
{
	public List<MeshMaterials> meshMaterials;

	public float distance = 1000f;

	public float cellSize = 10f;

	public bool on = true;
}
