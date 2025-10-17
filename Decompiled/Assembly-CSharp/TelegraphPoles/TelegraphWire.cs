using UnityEngine;

namespace TelegraphPoles;

[RequireComponent(typeof(MeshRenderer))]
public class TelegraphWire : MonoBehaviour
{
	private Vector3 _a;

	private Vector3 _b;

	private Material _material;

	private MeshRenderer _meshRenderer;

	public void Configure(Vector3 a, Vector3 b)
	{
		_a = a;
		_b = b;
		Vector3 position = Vector3.Lerp(_a, _b, 0.5f);
		base.transform.SetPositionAndRotation(position, Quaternion.LookRotation(_b - _a, Vector3.up));
		DestroyMaterial();
		if ((object)_meshRenderer == null)
		{
			_meshRenderer = GetComponent<MeshRenderer>();
		}
		_material = new Material(_meshRenderer.sharedMaterial);
		_material.hideFlags = HideFlags.DontSave;
		UpdatePositions();
		_meshRenderer.sharedMaterial = _material;
	}

	private void OnDestroy()
	{
		DestroyMaterial();
	}

	private void DestroyMaterial()
	{
		if (_material != null)
		{
			Object.DestroyImmediate(_material);
			_material = null;
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawLine(_a, _b);
	}

	public void WorldDidMove(Vector3 offset)
	{
		_a += offset;
		_b += offset;
		UpdatePositions();
	}

	private void UpdatePositions()
	{
		if (!(_material == null))
		{
			_material.SetVector("_Position0", base.transform.InverseTransformPoint(_a));
			_material.SetVector("_Position1", base.transform.InverseTransformPoint(_b));
			Bounds bounds = new Bounds(_a, Vector3.zero);
			bounds.Encapsulate(_b);
			bounds.Expand(Vector3.up * 5f);
			_meshRenderer.bounds = bounds;
		}
	}
}
