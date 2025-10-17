using InfinityCode.RealWorldTerrain.XML;
using UnityEngine;

namespace InfinityCode.RealWorldTerrain.Webservices.Results;

public class RealWorldTerrainGooglePlaceDetailsResult
{
	public string formatted_address;

	public string formatted_phone_number;

	public Vector2 location;

	public string icon;

	public string id;

	public string international_phone_number;

	public string name;

	public RealWorldTerrainXML node;

	public RealWorldTerrainPlacesResult.Photo[] photos;

	public string place_id;

	public int price_level = -1;

	public float rating;

	public string reference;

	public string[] types;

	public string url;

	public string utc_offset;

	public string vicinity;

	public string website;

	public RealWorldTerrainGooglePlaceDetailsResult()
	{
	}

	public RealWorldTerrainGooglePlaceDetailsResult(RealWorldTerrainXML node)
	{
		this.node = node;
		formatted_address = node.Get("formatted_address");
		formatted_phone_number = node.Get("formatted_phone_number");
		RealWorldTerrainXML realWorldTerrainXML = node.Find("geometry/location");
		if (!realWorldTerrainXML.isNull)
		{
			location = new Vector2(realWorldTerrainXML.Get<float>("lng"), realWorldTerrainXML.Get<float>("lat"));
		}
		icon = node.Get("icon");
		id = node.Get("id");
		international_phone_number = node.Get("international_phone_number");
		name = node.Get("name");
		RealWorldTerrainXMLList realWorldTerrainXMLList = node.FindAll("photo");
		photos = new RealWorldTerrainPlacesResult.Photo[realWorldTerrainXMLList.count];
		for (int i = 0; i < realWorldTerrainXMLList.count; i++)
		{
			photos[i] = new RealWorldTerrainPlacesResult.Photo(realWorldTerrainXMLList[i]);
		}
		place_id = node.Get<string>("place_id");
		price_level = node.Get("price_level", -1);
		rating = node.Get<float>("rating");
		reference = node.Get("reference");
		RealWorldTerrainXMLList realWorldTerrainXMLList2 = node.FindAll("type");
		types = new string[realWorldTerrainXMLList2.count];
		for (int j = 0; j < realWorldTerrainXMLList2.count; j++)
		{
			types[j] = realWorldTerrainXMLList2[j].Value();
		}
		url = node.Get("url");
		utc_offset = node.Get("utc_offset");
		vicinity = node.Get("vicinity");
		website = node.Get("website");
	}
}
