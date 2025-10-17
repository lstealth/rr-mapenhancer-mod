using System;
using System.Xml;
using UnityEngine;

namespace InfinityCode.RealWorldTerrain;

[Serializable]
public class RealWorldTerrainPOI
{
	public string title;

	public double x;

	public double y;

	public float altitude;

	public GameObject prefab;

	public RealWorldTerrainPOI()
	{
	}

	public RealWorldTerrainPOI(string title, double x, double y, float altitude = 0f)
	{
		this.title = title;
		this.x = x;
		this.y = y;
		this.altitude = altitude;
	}

	public RealWorldTerrainPOI(XmlNode node)
	{
		try
		{
			x = RealWorldTerrainXMLExt.GetAttribute<double>(node, "x");
			y = RealWorldTerrainXMLExt.GetAttribute<double>(node, "y");
			title = node.InnerText;
		}
		catch (Exception ex)
		{
			Debug.Log(ex.Message);
			throw;
		}
	}
}
