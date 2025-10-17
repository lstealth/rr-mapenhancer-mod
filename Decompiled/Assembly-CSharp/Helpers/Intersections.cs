using UnityEngine;

namespace Helpers;

public static class Intersections
{
	public static bool Intersects(Ray ray, Sphere s, out float distance, out Vector3 intersection)
	{
		Vector3 origin = ray.origin;
		Vector3 direction = ray.direction;
		Vector3 vector = origin - s.center;
		float num = Vector3.Dot(vector, direction);
		float num2 = Vector3.Dot(vector, vector) - s.radius * s.radius;
		distance = 0f;
		intersection = Vector3.zero;
		if (num2 > 0f && num > 0f)
		{
			return false;
		}
		float num3 = num * num - num2;
		if (num3 < 0f)
		{
			return false;
		}
		distance = 0f - num - Mathf.Sqrt(num3);
		if (distance < 0f)
		{
			distance = 0f;
		}
		intersection = origin + distance * direction;
		return true;
	}

	private static bool Intersects(this Ray ray, Sphere sphere, out float tmin, out float tmax)
	{
		tmin = 0f;
		tmax = 0f;
		Vector3 vector = ray.origin - sphere.center;
		float num = Vector3.Dot(ray.direction, ray.direction);
		float num2 = 2f * Vector3.Dot(vector, ray.direction);
		float num3 = Vector3.Dot(vector, vector) - sphere.radius * sphere.radius;
		float num4 = num2 * num2 - 4f * num * num3;
		if (num4 < 0f)
		{
			return false;
		}
		tmin = (0f - num2 - Mathf.Sqrt(num4)) / (2f * num);
		tmax = (0f - num2 + Mathf.Sqrt(num4)) / (2f * num);
		if (tmin > tmax)
		{
			float num5 = tmin;
			tmin = tmax;
			tmax = num5;
		}
		return true;
	}

	public static bool Intersects(this Ray ray, Capsule capsule, out Vector3 p1, out Vector3 p2, out Vector3 n1, out Vector3 n2)
	{
		p1 = Vector3.zero;
		p2 = Vector3.zero;
		n1 = Vector3.zero;
		n2 = Vector3.zero;
		Vector3 vector = capsule.b - capsule.a;
		Vector3 vector2 = ray.origin - capsule.a;
		float num = Vector3.Dot(vector, ray.direction);
		float num2 = Vector3.Dot(vector, vector2);
		float num3 = Vector3.Dot(vector, vector);
		float num4 = num / num3;
		float num5 = num2 / num3;
		Vector3 vector3 = ray.direction - vector * num4;
		Vector3 vector4 = vector2 - vector * num5;
		float num6 = Vector3.Dot(vector3, vector3);
		float num7 = 2f * Vector3.Dot(vector3, vector4);
		float num8 = Vector3.Dot(vector4, vector4) - capsule.radius * capsule.radius;
		if (num6 == 0f)
		{
			Sphere sphere = default(Sphere);
			sphere.center = capsule.a;
			sphere.radius = capsule.radius;
			Sphere sphere2 = default(Sphere);
			sphere2.center = capsule.b;
			sphere2.radius = capsule.radius;
			if (!ray.Intersects(sphere, out float tmin, out float tmax) || !ray.Intersects(sphere2, out float tmin2, out float tmax2))
			{
				return false;
			}
			if (tmin < tmin2)
			{
				p1 = ray.origin + ray.direction * tmin;
				n1 = p1 - capsule.a;
				n1.Normalize();
			}
			else
			{
				p1 = ray.origin + ray.direction * tmin2;
				n1 = p1 - capsule.b;
				n1.Normalize();
			}
			if (tmax > tmax2)
			{
				p2 = ray.origin + ray.direction * tmax;
				n2 = p2 - capsule.a;
				n2.Normalize();
			}
			else
			{
				p2 = ray.origin + ray.direction * tmax2;
				n2 = p2 - capsule.b;
				n2.Normalize();
			}
			return true;
		}
		float num9 = num7 * num7 - 4f * num6 * num8;
		if (num9 < 0f)
		{
			return false;
		}
		float num10 = (0f - num7 - Mathf.Sqrt(num9)) / (2f * num6);
		float num11 = (0f - num7 + Mathf.Sqrt(num9)) / (2f * num6);
		if (num10 > num11)
		{
			float num12 = num10;
			num10 = num11;
			num11 = num12;
		}
		float num13 = num10 * num4 + num5;
		if (num13 < 0f)
		{
			Sphere sphere3 = default(Sphere);
			sphere3.center = capsule.a;
			sphere3.radius = capsule.radius;
			if (!ray.Intersects(sphere3, out float tmin3, out float _))
			{
				return false;
			}
			p1 = ray.origin + ray.direction * tmin3;
			n1 = p1 - capsule.a;
			n1.Normalize();
		}
		else if (num13 > 1f)
		{
			Sphere sphere4 = default(Sphere);
			sphere4.center = capsule.b;
			sphere4.radius = capsule.radius;
			if (!ray.Intersects(sphere4, out float tmin4, out float _))
			{
				return false;
			}
			p1 = ray.origin + ray.direction * tmin4;
			n1 = p1 - capsule.b;
			n1.Normalize();
		}
		else
		{
			p1 = ray.origin + ray.direction * num10;
			Vector3 vector5 = capsule.a + vector * num13;
			n1 = p1 - vector5;
			n1.Normalize();
		}
		float num14 = num11 * num4 + num5;
		if (num14 < 0f)
		{
			Sphere sphere5 = default(Sphere);
			sphere5.center = capsule.a;
			sphere5.radius = capsule.radius;
			if (!ray.Intersects(sphere5, out float _, out float tmax5))
			{
				return false;
			}
			p2 = ray.origin + ray.direction * tmax5;
			n2 = p2 - capsule.a;
			n2.Normalize();
		}
		else if (num14 > 1f)
		{
			Sphere sphere6 = default(Sphere);
			sphere6.center = capsule.b;
			sphere6.radius = capsule.radius;
			if (!ray.Intersects(sphere6, out float _, out float tmax6))
			{
				return false;
			}
			p2 = ray.origin + ray.direction * tmax6;
			n2 = p2 - capsule.b;
			n2.Normalize();
		}
		else
		{
			p2 = ray.origin + ray.direction * num11;
			Vector3 vector6 = capsule.a + vector * num14;
			n2 = p2 - vector6;
			n2.Normalize();
		}
		return true;
	}
}
