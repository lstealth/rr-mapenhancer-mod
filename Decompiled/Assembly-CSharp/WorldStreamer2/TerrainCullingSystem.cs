using UnityEngine;

namespace WorldStreamer2;

public class TerrainCullingSystem : MonoBehaviour
{
	[Tooltip("Max view distance is referred from camera to terrain center point")]
	public float renderingDistance = 10000f;

	private float sphereSize = 0.5f;

	private Terrain terrain;

	private CullingGroup group;

	private BoundingSphere[] spheres = new BoundingSphere[1000];

	private Vector3 offsetVector;

	private Vector3 offsetVectorUp;

	private Camera mainCamera;

	private int heightSphereNumber;

	private void Start()
	{
		terrain = GetComponent<Terrain>();
		if (terrain != null)
		{
			if (terrain.terrainData.size.x > terrain.terrainData.size.z)
			{
				sphereSize = terrain.terrainData.size.x * 0.75f;
			}
			else
			{
				sphereSize = terrain.terrainData.size.z * 0.75f;
			}
			offsetVector = new Vector3(terrain.terrainData.size.x, 0f, terrain.terrainData.size.z) * 0.5f;
			group = new CullingGroup();
			group.targetCamera = Camera.main;
			heightSphereNumber = 2 * (int)(terrain.terrainData.size.y / sphereSize);
			heightSphereNumber = Mathf.Max(1, heightSphereNumber);
			offsetVectorUp = new Vector3(0f, sphereSize * 0.5f, 0f);
			for (int i = 0; i < heightSphereNumber; i++)
			{
				spheres[i] = new BoundingSphere(base.transform.position + offsetVector + i * offsetVectorUp, sphereSize);
			}
			group.SetBoundingSpheres(spheres);
			group.SetBoundingSphereCount(heightSphereNumber);
			group.onStateChanged = StateChangedMethod;
			group.SetBoundingDistances(new float[1] { renderingDistance });
			mainCamera = Camera.main;
			group.SetDistanceReferencePoint(Camera.main.transform);
			Invoke("CheckVisibility", 0.1f);
		}
		else
		{
			Debug.LogError("TerrainCullingSystem: no terrain on game object " + base.gameObject.name);
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		for (int i = 0; i < heightSphereNumber; i++)
		{
			Gizmos.DrawWireSphere(base.transform.position + offsetVector + i * offsetVectorUp, sphereSize);
		}
	}

	private void CheckVisibility()
	{
		bool flag = false;
		for (int i = 0; i < heightSphereNumber; i++)
		{
			if (group.IsVisible(i))
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			terrain.drawHeightmap = false;
			terrain.drawTreesAndFoliage = false;
		}
	}

	public void Update()
	{
		for (int i = 0; i < heightSphereNumber; i++)
		{
			spheres[i] = new BoundingSphere(base.transform.position + offsetVector + i * offsetVectorUp, sphereSize);
		}
		if (mainCamera == null)
		{
			mainCamera = Camera.main;
			if (mainCamera != null)
			{
				group.SetDistanceReferencePoint(mainCamera.transform);
			}
		}
	}

	private void StateChangedMethod(CullingGroupEvent evt)
	{
		bool flag = false;
		for (int i = 0; i < heightSphereNumber; i++)
		{
			if (group.IsVisible(i))
			{
				flag = true;
				break;
			}
		}
		if (flag)
		{
			terrain.drawHeightmap = true;
			terrain.drawTreesAndFoliage = true;
		}
		else
		{
			terrain.drawHeightmap = false;
			terrain.drawTreesAndFoliage = false;
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
}
