using UnityEngine;

namespace Helpers;

public class CapsuleGeometry : MonoBehaviour
{
	public Capsule capsule;

	public bool IsIntersectedBy(Ray ray)
	{
		Capsule capsule = new Capsule(base.transform.TransformPoint(this.capsule.a), base.transform.TransformPoint(this.capsule.b), base.transform.TransformPoint(new Vector3(this.capsule.radius, 0f, 0f)).x - base.transform.position.x);
		Vector3 p;
		Vector3 p2;
		Vector3 n;
		Vector3 n2;
		return ray.Intersects(capsule, out p, out p2, out n, out n2);
	}
}
