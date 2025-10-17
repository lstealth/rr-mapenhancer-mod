using UnityEngine;

namespace Helpers;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshCollider))]
public class RuntimeCylinderMesh : MonoBehaviour
{
	[Range(0f, 100f)]
	public float radius;

	[Range(0f, 100f)]
	public float height;

	private void OnValidate()
	{
		GenerateMesh();
	}

	private void Start()
	{
		GenerateMesh();
	}

	private void GenerateMesh()
	{
		Mesh sharedMesh = CylinderMeshBuilder.BuildCapless(Matrix4x4.Scale(new Vector3(height, radius * 2f, radius * 2f)));
		GetComponent<MeshCollider>().sharedMesh = sharedMesh;
		MeshFilter component = GetComponent<MeshFilter>();
		if (component != null)
		{
			component.sharedMesh = sharedMesh;
		}
	}
}
