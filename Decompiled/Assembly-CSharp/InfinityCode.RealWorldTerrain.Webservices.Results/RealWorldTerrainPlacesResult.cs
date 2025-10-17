using System;
using System.Collections.Generic;
using InfinityCode.RealWorldTerrain.XML;
using UnityEngine;

namespace InfinityCode.RealWorldTerrain.Webservices.Results;

public class RealWorldTerrainPlacesResult
{
	public class Photo
	{
		public int width;

		public int height;

		public string photo_reference;

		public string[] html_attributions;

		public Photo()
		{
		}

		public Photo(RealWorldTerrainXML node)
		{
			try
			{
				width = node.Get<int>("width");
				height = node.Get<int>("height");
				photo_reference = node["photo_reference"].Value();
				List<string> list = new List<string>();
				foreach (RealWorldTerrainXML item in node.FindAll("html_attributions"))
				{
					list.Add(item.Value());
				}
				html_attributions = list.ToArray();
			}
			catch (Exception)
			{
			}
		}

		public RealWorldTerrainGooglePlacePhoto Download(string key, int? maxWidth = null, int? maxHeight = null)
		{
			if (!maxWidth.HasValue)
			{
				maxWidth = width;
			}
			if (!maxHeight.HasValue)
			{
				maxHeight = height;
			}
			return RealWorldTerrainGooglePlacePhoto.Download(key, photo_reference, maxWidth, maxHeight);
		}
	}

	public Vector2 location;

	public string icon;

	public string id;

	public string formatted_address;

	public string name;

	public string place_id;

	public string reference;

	public string[] types;

	public string vicinity;

	public int price_level = -1;

	public float rating;

	public bool open_now;

	public string scope;

	public string[] weekday_text;

	public Photo[] photos;

	public RealWorldTerrainPlacesResult()
	{
	}

	public RealWorldTerrainPlacesResult(RealWorldTerrainXML node)
	{
		List<Photo> list = new List<Photo>();
		List<string> list2 = new List<string>();
		List<string> list3 = new List<string>();
		foreach (RealWorldTerrainXML item in node)
		{
			if (item.name == "name")
			{
				name = item.Value();
			}
			else if (item.name == "id")
			{
				id = item.Value();
			}
			else if (item.name == "vicinity")
			{
				vicinity = item.Value();
			}
			else if (item.name == "type")
			{
				list2.Add(item.Value());
			}
			else if (item.name == "geometry")
			{
				location = RealWorldTerrainXML.GetVector2FromNode(item[0]);
			}
			else if (item.name == "rating")
			{
				rating = item.Value<float>();
			}
			else if (item.name == "icon")
			{
				icon = item.Value();
			}
			else if (item.name == "reference")
			{
				reference = item.Value();
			}
			else if (item.name == "place_id")
			{
				place_id = item.Value();
			}
			else if (item.name == "scope")
			{
				scope = item.Value();
			}
			else if (item.name == "price_level")
			{
				price_level = item.Value<int>();
			}
			else if (item.name == "formatted_address")
			{
				formatted_address = item.Value();
			}
			else if (item.name == "opening_hours")
			{
				open_now = item.Get<string>("open_now") == "true";
				foreach (RealWorldTerrainXML item2 in item.FindAll("weekday_text"))
				{
					list3.Add(item2.Value());
				}
			}
			else if (item.name == "photo")
			{
				list.Add(new Photo(item));
			}
			else
			{
				Debug.Log(item.name);
			}
		}
		photos = list.ToArray();
		types = list2.ToArray();
		weekday_text = list3.ToArray();
	}
}
