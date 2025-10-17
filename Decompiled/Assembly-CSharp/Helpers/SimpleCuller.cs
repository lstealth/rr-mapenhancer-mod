using UnityEngine;

namespace Helpers;

public class SimpleCuller : MonoBehaviour
{
	[Tooltip("Radius of the object being culled.")]
	public float radius = 10f;

	[Tooltip("Distance beyond which this object should be culled deactivated.")]
	public float distance = 500f;

	private CullingGroup _cullingGroup;

	private BoundingSphere[] _cullingSpheres;

	private void Start()
	{
		_cullingSpheres = new BoundingSphere[1];
		_cullingSpheres[0] = new BoundingSphere(base.transform.position, radius);
		_cullingGroup = new CullingGroup();
		_cullingGroup.SetBoundingSpheres(_cullingSpheres);
		_cullingGroup.SetBoundingSphereCount(1);
		_cullingGroup.onStateChanged = CullingGroupStateChanged;
		_cullingGroup.SetBoundingDistances(new float[1] { distance });
		_cullingGroup.SetDistanceReferencePoint(Camera.main.transform);
		if (WorldTransformer.TryGetShared(out var shared))
		{
			shared.OnDidMove += OnWorldDidMove;
		}
	}

	private void Update()
	{
		if (base.transform.hasChanged)
		{
			OnTransformDidChange();
			base.transform.hasChanged = false;
		}
		if (_cullingGroup.targetCamera == null)
		{
			_cullingGroup.targetCamera = Camera.main;
		}
	}

	private void OnDestroy()
	{
		_cullingGroup?.Dispose();
		_cullingGroup = null;
		if (WorldTransformer.TryGetShared(out var shared))
		{
			shared.OnDidMove -= OnWorldDidMove;
		}
	}

	private void OnValidate()
	{
		if (_cullingSpheres != null)
		{
			_cullingSpheres[0].radius = radius;
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.magenta;
		Gizmos.DrawWireSphere(base.transform.position, radius);
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(base.transform.position, distance);
	}

	private void CullingGroupStateChanged(CullingGroupEvent sphere)
	{
		bool isVisible = sphere.isVisible;
		base.gameObject.SetActive(isVisible);
	}

	private void OnWorldDidMove(Vector3 offset)
	{
		OnTransformDidChange();
	}

	private void OnTransformDidChange()
	{
		_cullingSpheres[0].position = base.transform.position;
	}
}
