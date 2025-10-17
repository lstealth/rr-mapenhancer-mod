using System;
using System.Collections.Generic;
using System.Text;
using InfinityCode.RealWorldTerrain.ExtraTypes;
using InfinityCode.RealWorldTerrain.Webservices.Base;
using InfinityCode.RealWorldTerrain.Webservices.Results;
using InfinityCode.RealWorldTerrain.XML;
using UnityEngine;

namespace InfinityCode.RealWorldTerrain.Webservices;

public class RealWorldTerrainGoogleGeocoding : RealWorldTerrainTextWebServiceBase
{
	public abstract class RequestParams
	{
		public string key;

		public string language;

		public string client;

		public string signature;

		internal virtual void GenerateURL(StringBuilder url)
		{
			if (!string.IsNullOrEmpty(key))
			{
				url.Append("&key=").Append(key);
			}
			if (!string.IsNullOrEmpty(language))
			{
				url.Append("&language=").Append(language);
			}
			if (!string.IsNullOrEmpty(client))
			{
				url.Append("&client=").Append(client);
			}
			if (!string.IsNullOrEmpty(signature))
			{
				url.Append("&signature=").Append(signature);
			}
		}
	}

	public class GeocodingParams : RequestParams
	{
		public string address;

		public string components;

		public RealWorldTerrainGeoRect bounds;

		public string region;

		public GeocodingParams()
		{
		}

		public GeocodingParams(string address)
		{
			this.address = address;
		}

		internal override void GenerateURL(StringBuilder url)
		{
			base.GenerateURL(url);
			if (!string.IsNullOrEmpty(address))
			{
				url.Append("&address=").Append(RealWorldTerrainWWW.EscapeURL(address));
			}
			if (!string.IsNullOrEmpty(components))
			{
				url.Append("&components=").Append(components);
			}
			if (bounds != null)
			{
				url.Append("&bounds=").Append(bounds.bottom).Append(",")
					.Append(bounds.left)
					.Append("|")
					.Append(bounds.top)
					.Append(",")
					.Append(bounds.right);
			}
			if (!string.IsNullOrEmpty(region))
			{
				url.Append("&region=").Append(region);
			}
		}
	}

	public class ReverseGeocodingParams : RequestParams
	{
		public double? longitude;

		public double? latitude;

		public string placeId;

		public string result_type;

		public string location_type;

		public Vector2? location
		{
			get
			{
				return new Vector2(longitude.HasValue ? ((float)longitude.Value) : 0f, latitude.HasValue ? ((float)latitude.Value) : 0f);
			}
			set
			{
				if (value.HasValue)
				{
					longitude = value.Value.x;
					latitude = value.Value.y;
				}
				else
				{
					longitude = null;
					latitude = null;
				}
			}
		}

		public ReverseGeocodingParams(double longitude, double latitude)
		{
			this.longitude = longitude;
			this.latitude = latitude;
		}

		public ReverseGeocodingParams(Vector2 location)
		{
			this.location = location;
		}

		public ReverseGeocodingParams(string placeId)
		{
			this.placeId = placeId;
		}

		internal override void GenerateURL(StringBuilder url)
		{
			base.GenerateURL(url);
			if (longitude.HasValue && latitude.HasValue)
			{
				url.Append("&latlng=").Append(latitude.Value.ToString(RealWorldTerrainCultureInfo.numberFormat)).Append(",")
					.Append(longitude.Value.ToString(RealWorldTerrainCultureInfo.numberFormat));
			}
			else
			{
				if (string.IsNullOrEmpty(placeId))
				{
					throw new Exception("You must specify latitude and longitude, location, or placeId.");
				}
				url.Append("&placeId=").Append(placeId);
			}
			if (!string.IsNullOrEmpty(result_type))
			{
				url.Append("&result_type=").Append(result_type);
			}
			if (!string.IsNullOrEmpty(location_type))
			{
				url.Append("&location_type=").Append(location_type);
			}
		}
	}

	private RealWorldTerrainGoogleGeocoding(RequestParams p)
	{
		_status = RequestStatus.downloading;
		StringBuilder stringBuilder = new StringBuilder("https://maps.googleapis.com/maps/api/geocode/xml?sensor=false");
		p.GenerateURL(stringBuilder);
		www = new RealWorldTerrainWWW(stringBuilder.ToString());
		RealWorldTerrainWWW realWorldTerrainWWW = www;
		realWorldTerrainWWW.OnComplete = (Action<RealWorldTerrainWWW>)Delegate.Combine(realWorldTerrainWWW.OnComplete, new Action<RealWorldTerrainWWW>(base.OnRequestComplete));
	}

	public static RealWorldTerrainGoogleGeocoding Find(RequestParams p)
	{
		return new RealWorldTerrainGoogleGeocoding(p);
	}

	public static RealWorldTerrainGoogleGeocodingResult[] GetResults(string response)
	{
		try
		{
			RealWorldTerrainXML realWorldTerrainXML = RealWorldTerrainXML.Load(response);
			if (realWorldTerrainXML.Find<string>("//status") != "OK")
			{
				return null;
			}
			List<RealWorldTerrainGoogleGeocodingResult> list = new List<RealWorldTerrainGoogleGeocodingResult>();
			foreach (RealWorldTerrainXML item in realWorldTerrainXML.FindAll("//result"))
			{
				list.Add(new RealWorldTerrainGoogleGeocodingResult(item));
			}
			return list.ToArray();
		}
		catch (Exception ex)
		{
			Debug.Log(ex.Message + "\n" + ex.StackTrace);
		}
		return null;
	}
}
