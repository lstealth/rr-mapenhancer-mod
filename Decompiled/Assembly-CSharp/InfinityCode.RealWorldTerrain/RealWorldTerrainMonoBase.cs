using System;
using System.Collections.Generic;
using UnityEngine;

namespace InfinityCode.RealWorldTerrain;

public abstract class RealWorldTerrainMonoBase : MonoBehaviour
{
	public Bounds bounds;

	public bool generateGrass;

	public bool generatedRivers;

	public bool generateRoads;

	public bool generateTextures;

	public bool generateTrees;

	public bool generatedBuildings;

	public bool generatedGrass;

	public bool generatedTextures;

	public bool generatedTrees;

	public double maxElevation;

	public double minElevation;

	public RealWorldTerrainPrefsBase prefs;

	public Vector3 scale;

	public Vector3 size;

	public double topLatitude;

	public double topMercator;

	public double leftLongitude;

	public double leftMercator;

	public double bottomLatitude;

	public double bottomMercator;

	public double rightLongitude;

	public double rightMercator;

	public double width;

	public double height;

	private Dictionary<string, object> customFields;

	public double mercatorWidth;

	public double mercatorHeight;

	public object this[string key]
	{
		get
		{
			if (customFields == null)
			{
				return null;
			}
			if (!customFields.TryGetValue(key, out var value))
			{
				return null;
			}
			return value;
		}
		set
		{
			if (customFields == null)
			{
				customFields = new Dictionary<string, object>();
			}
			if (value != null)
			{
				customFields[key] = value;
			}
			else
			{
				customFields.Remove(key);
			}
		}
	}

	public void ClearCustomFields()
	{
		customFields = null;
	}

	public bool Contains(Vector2 coordinates)
	{
		return Contains(coordinates.x, coordinates.y);
	}

	public bool Contains(double lng, double lat)
	{
		if (leftLongitude <= lng && rightLongitude >= lng && topLatitude >= lat)
		{
			return bottomLatitude <= lat;
		}
		return false;
	}

	public double GetAltitudeByCoordinates(double lng, double lat)
	{
		if (!Contains(lng, lat))
		{
			return 0.0;
		}
		GetWorldPosition(lng, lat, out var worldPosition);
		Bounds bounds = new Bounds(this.bounds.center + base.transform.position, this.bounds.size);
		Vector3 vector = worldPosition - bounds.min;
		if (prefs.resultType == RealWorldTerrainResultType.terrain)
		{
			RealWorldTerrainItem itemByWorldPosition = GetItemByWorldPosition(worldPosition);
			return (double)(vector.y / itemByWorldPosition.terrainData.size.y) * (itemByWorldPosition.maxElevation - itemByWorldPosition.minElevation);
		}
		return (double)(worldPosition.y / bounds.size.y) * (maxElevation - minElevation) + minElevation;
	}

	[Obsolete("Use GetAltitudeByCoordinates")]
	public double GetAltitudeByLocation(double lng, double lat)
	{
		return GetAltitudeByCoordinates(lng, lat);
	}

	public double GetAltitudeByWorldPosition(Vector3 worldPosition)
	{
		Bounds bounds = new Bounds(this.bounds.center + base.transform.position, this.bounds.size);
		Vector3 vector = worldPosition - bounds.min;
		if (vector.x < 0f || vector.z < 0f)
		{
			return 0.0;
		}
		if (vector.x > bounds.size.x || vector.z > bounds.size.z)
		{
			return 0.0;
		}
		if (prefs.resultType == RealWorldTerrainResultType.terrain)
		{
			RealWorldTerrainItem itemByWorldPosition = GetItemByWorldPosition(worldPosition);
			return (double)(vector.y / itemByWorldPosition.terrainData.size.y) * (itemByWorldPosition.maxElevation - itemByWorldPosition.minElevation);
		}
		return (double)(worldPosition.y / bounds.size.y) * (maxElevation - minElevation) + minElevation;
	}

	public bool GetCoordinatesUnderCursor(out Vector2 coordinates, Camera cam = null)
	{
		return GetCoordinatesByScreenPosition(Input.mousePosition, out coordinates, cam);
	}

	public bool GetCoordinatesByScreenPosition(Vector2 screenPosition, out Vector2 coordinates, Camera cam = null)
	{
		if (cam == null)
		{
			cam = Camera.main;
		}
		coordinates = Vector2.zero;
		RaycastHit[] array = Physics.RaycastAll(cam.ScreenPointToRay(screenPosition));
		for (int i = 0; i < array.Length; i++)
		{
			RaycastHit raycastHit = array[i];
			if ((raycastHit.collider is TerrainCollider || raycastHit.collider is MeshCollider) && raycastHit.transform.GetComponent<RealWorldTerrainItem>() != null)
			{
				return GetCoordinatesByWorldPosition(raycastHit.point, out coordinates);
			}
		}
		return false;
	}

	public bool GetCoordinatesByScreenPosition(Vector2 screenPosition, out double longitude, out double latitude, out double altitude, Camera cam = null)
	{
		if (cam == null)
		{
			cam = Camera.main;
		}
		longitude = (latitude = (altitude = 0.0));
		RaycastHit[] array = Physics.RaycastAll(cam.ScreenPointToRay(screenPosition));
		for (int i = 0; i < array.Length; i++)
		{
			RaycastHit raycastHit = array[i];
			if ((raycastHit.collider is TerrainCollider || raycastHit.collider is MeshCollider) && raycastHit.transform.GetComponent<RealWorldTerrainItem>() != null)
			{
				return GetCoordinatesByWorldPosition(raycastHit.point, out longitude, out latitude, out altitude);
			}
		}
		return false;
	}

	public bool GetCoordinatesByWorldPosition(Vector3 worldPosition, out Vector2 coordinates)
	{
		coordinates = default(Vector2);
		double longitude;
		double latitude;
		bool coordinatesByWorldPosition = GetCoordinatesByWorldPosition(worldPosition, out longitude, out latitude);
		coordinates.x = (float)longitude;
		coordinates.y = (float)latitude;
		return coordinatesByWorldPosition;
	}

	public bool GetCoordinatesByWorldPosition(Vector3 worldPosition, out double longitude, out double latitude)
	{
		Bounds bounds = new Bounds(this.bounds.center + base.transform.position, this.bounds.size);
		double num = (worldPosition.x - bounds.min.x) / bounds.size.x;
		double num2 = (bounds.max.z - worldPosition.z) / bounds.size.z;
		double mx = (rightMercator - leftMercator) * num + leftMercator;
		double my = (bottomMercator - topMercator) * num2 + topMercator;
		RealWorldTerrainUtils.MercatToLatLong(mx, my, out longitude, out latitude);
		return bounds.Contains(worldPosition);
	}

	public bool GetCoordinatesByWorldPosition(Vector3 worldPosition, out double longitude, out double latitude, out double altitude)
	{
		altitude = 0.0;
		Bounds bounds = new Bounds(this.bounds.center + base.transform.position, this.bounds.size);
		double num = (worldPosition.x - bounds.min.x) / bounds.size.x;
		double num2 = (bounds.max.z - worldPosition.z) / bounds.size.z;
		double mx = (rightMercator - leftMercator) * num + leftMercator;
		double my = (bottomMercator - topMercator) * num2 + topMercator;
		RealWorldTerrainUtils.MercatToLatLong(mx, my, out longitude, out latitude);
		if (bounds.min.x > worldPosition.x || bounds.max.x < worldPosition.x || bounds.min.z > worldPosition.z || bounds.max.z < worldPosition.z)
		{
			return false;
		}
		Vector3 vector = worldPosition - bounds.min;
		if (prefs.resultType == RealWorldTerrainResultType.terrain)
		{
			RealWorldTerrainItem itemByWorldPosition = GetItemByWorldPosition(worldPosition);
			altitude = (double)((vector.y + itemByWorldPosition.transform.position.y) / itemByWorldPosition.terrainData.size.y) * (itemByWorldPosition.maxElevation - itemByWorldPosition.minElevation) + itemByWorldPosition.minElevation;
		}
		else
		{
			altitude = (double)(worldPosition.y / bounds.size.y) * (maxElevation - minElevation) + minElevation;
		}
		return true;
	}

	public abstract RealWorldTerrainItem GetItemByWorldPosition(Vector3 worldPosition);

	public abstract bool GetWorldPosition(double lng, double lat, out Vector3 worldPosition);

	public bool GetWorldPosition(double lng, double lat, double altitude, out Vector3 worldPosition)
	{
		bool worldPosition2 = GetWorldPosition(lng, lat, out worldPosition);
		if (worldPosition2)
		{
			worldPosition.y = (float)((double)bounds.size.y * ((altitude - minElevation) / (maxElevation - minElevation)));
		}
		return worldPosition2;
	}

	public abstract bool GetWorldPosition(Vector2 coordinates, out Vector3 worldPosition);

	public void SetCoordinates(double x1, double y1, double x2, double y2, double tlx, double tly, double brx, double bry)
	{
		leftMercator = x1;
		topMercator = y1;
		rightMercator = x2;
		bottomMercator = y2;
		leftLongitude = tlx;
		rightLongitude = brx;
		topLatitude = tly;
		bottomLatitude = bry;
		width = rightLongitude - leftLongitude;
		height = bottomLatitude - topLatitude;
		mercatorWidth = rightMercator - leftMercator;
		mercatorHeight = bottomMercator - topMercator;
	}
}
