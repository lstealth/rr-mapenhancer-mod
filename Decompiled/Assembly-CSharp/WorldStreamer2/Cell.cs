using System;
using System.Collections.Generic;
using UnityEngine;

namespace WorldStreamer2;

[Serializable]
public class Cell
{
	public List<Matrix4x4> matrices;

	public Matrix4x4[] matricesArray;

	public Bounds bounds;

	public Vector4 size;
}
