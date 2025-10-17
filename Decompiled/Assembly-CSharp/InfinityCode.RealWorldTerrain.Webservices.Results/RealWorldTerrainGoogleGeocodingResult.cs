using System;
using System.Collections.Generic;
using InfinityCode.RealWorldTerrain.XML;
using UnityEngine;

namespace InfinityCode.RealWorldTerrain.Webservices.Results;

[Serializable]
public class RealWorldTerrainGoogleGeocodingResult
{
	[Serializable]
	public class AddressComponent
	{
		public string[] types;

		public string long_name;

		public string short_name;

		public AddressComponent()
		{
		}

		public AddressComponent(RealWorldTerrainXML node)
		{
			List<string> list = new List<string>();
			foreach (RealWorldTerrainXML item in node)
			{
				if (item.name == "long_name")
				{
					long_name = item.Value();
				}
				else if (item.name == "short_name")
				{
					short_name = item.Value();
				}
				else if (item.name == "type")
				{
					list.Add(item.Value());
				}
				else
				{
					Debug.Log(item.name);
				}
			}
			types = list.ToArray();
		}

		public override string ToString()
		{
			return "RealWorldTerrainGoogleGeocodingResult.AddressComponent. Types: {" + string.Join(",", types) + "}, Long name: {" + long_name + "}, Short name: {" + short_name + "}";
		}
	}

	public AddressComponent[] address_components;

	public string[] types;

	public string formatted_address;

	public string[] postcode_localities;

	public Vector2 geometry_location;

	public string geometry_location_type;

	public Vector2 geometry_viewport_northeast;

	public Vector2 geometry_viewport_southwest;

	public string place_id;

	public Vector2 geometry_bounds_northeast;

	public Vector2 geometry_bounds_southwest;

	public bool partial_match;

	public RealWorldTerrainGoogleGeocodingResult()
	{
	}

	public RealWorldTerrainGoogleGeocodingResult(RealWorldTerrainXML node)
	{
		List<AddressComponent> list = new List<AddressComponent>();
		List<string> list2 = new List<string>();
		List<string> list3 = new List<string>();
		foreach (RealWorldTerrainXML item in node)
		{
			if (item.name == "type")
			{
				list2.Add(item.Value());
			}
			else if (item.name == "place_id")
			{
				place_id = item.Value();
			}
			else if (item.name == "formatted_address")
			{
				formatted_address = item.Value();
			}
			else if (item.name == "address_component")
			{
				list.Add(new AddressComponent(item));
			}
			else if (item.name == "geometry")
			{
				foreach (RealWorldTerrainXML item2 in item)
				{
					if (item2.name == "location")
					{
						geometry_location = RealWorldTerrainXML.GetVector2FromNode(item2);
					}
					else if (item2.name == "location_type")
					{
						geometry_location_type = item2.Value();
					}
					else if (item2.name == "viewport")
					{
						geometry_viewport_northeast = RealWorldTerrainXML.GetVector2FromNode(item2["northeast"]);
						geometry_viewport_southwest = RealWorldTerrainXML.GetVector2FromNode(item2["southwest"]);
					}
					else if (item2.name == "bounds")
					{
						geometry_bounds_northeast = RealWorldTerrainXML.GetVector2FromNode(item2["northeast"]);
						geometry_bounds_southwest = RealWorldTerrainXML.GetVector2FromNode(item2["southwest"]);
					}
					else
					{
						Debug.Log(item.name);
					}
				}
			}
			else if (item.name == "partial_match")
			{
				partial_match = item.Value() == "true";
			}
			else
			{
				Debug.Log(item.name);
			}
		}
		address_components = list.ToArray();
		types = list2.ToArray();
		postcode_localities = list3.ToArray();
	}
}
