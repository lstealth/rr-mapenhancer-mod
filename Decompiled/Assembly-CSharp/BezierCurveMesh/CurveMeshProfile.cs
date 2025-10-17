using UnityEngine;

namespace BezierCurveMesh;

[CreateAssetMenu(fileName = "Curve Mesh", menuName = "Railroader/Curve Mesh Profile", order = 0)]
public class CurveMeshProfile : ScriptableObject
{
	public MeshFilter meshFilter;

	public VectorAxis forwardAxis = VectorAxis.Y;

	public Vector3 vertexScale = Vector3.one;
}
