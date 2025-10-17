using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace BezierCurveMesh;

[Serializable]
public class GenerateMeshParameters
{
	public VectorAxis forwardAxis;

	[FormerlySerializedAs("scale")]
	public Vector3 vertexScale;
}
