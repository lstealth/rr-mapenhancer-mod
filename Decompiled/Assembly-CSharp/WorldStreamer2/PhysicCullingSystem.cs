using UnityEngine;

namespace WorldStreamer2;

[RequireComponent(typeof(Rigidbody))]
public class PhysicCullingSystem : MonoBehaviour
{
	[Tooltip("Max view distance is referred from camera to terrain center point")]
	public float physicDistance = 10000f;

	private float sphereSize = 0.5f;

	private Rigidbody rigidbody;

	private CullingGroup group;

	private BoundingSphere[] spheres = new BoundingSphere[1000];

	private Camera mainCamera;

	[HideInInspector]
	public Vector3 velocity;

	[HideInInspector]
	public Vector3 angularVelocity;

	public bool gizmo = true;

	private void Start()
	{
		rigidbody = GetComponent<Rigidbody>();
		group = new CullingGroup();
		group.targetCamera = Camera.main;
		spheres[0] = new BoundingSphere(base.transform.position, sphereSize);
		group.SetBoundingSpheres(spheres);
		group.SetBoundingSphereCount(1);
		group.onStateChanged = StateChangedMethod;
		group.SetBoundingDistances(new float[1] { physicDistance });
		mainCamera = Camera.main;
		group.SetDistanceReferencePoint(Camera.main.transform);
		Invoke("CheckVisibility", 0.1f);
	}

	private void OnDrawGizmosSelected()
	{
		if (gizmo)
		{
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(base.transform.position, physicDistance);
		}
	}

	private void CheckVisibility()
	{
		bool flag = false;
		if (group.GetDistance(0) == 0)
		{
			flag = true;
		}
		if (!flag)
		{
			StartMovement();
		}
	}

	public void Update()
	{
		if (mainCamera != Camera.main)
		{
			mainCamera = Camera.main;
		}
		group.SetDistanceReferencePoint(Camera.main.transform);
		spheres[0].position = base.transform.position;
	}

	private void StateChangedMethod(CullingGroupEvent evt)
	{
		bool flag = false;
		if (group.GetDistance(0) == 0)
		{
			flag = true;
		}
		if (flag)
		{
			StopMovement();
		}
		else
		{
			StartMovement();
		}
	}

	private void OnDisable()
	{
		if (group != null)
		{
			group.Dispose();
			group = null;
		}
	}

	private void StopMovement()
	{
		velocity = rigidbody.velocity;
		angularVelocity = rigidbody.angularVelocity;
		rigidbody.isKinematic = false;
	}

	private void StartMovement()
	{
		rigidbody.isKinematic = true;
		rigidbody.velocity = velocity;
		rigidbody.angularVelocity = angularVelocity;
	}
}
