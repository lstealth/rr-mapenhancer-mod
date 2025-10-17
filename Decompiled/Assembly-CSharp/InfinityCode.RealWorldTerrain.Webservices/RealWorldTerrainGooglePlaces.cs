using System;
using System.Collections.Generic;
using System.Text;
using InfinityCode.RealWorldTerrain.ExtraTypes;
using InfinityCode.RealWorldTerrain.Webservices.Base;
using InfinityCode.RealWorldTerrain.Webservices.Results;
using InfinityCode.RealWorldTerrain.XML;
using UnityEngine;

namespace InfinityCode.RealWorldTerrain.Webservices;

public class RealWorldTerrainGooglePlaces : RealWorldTerrainTextWebServiceBase
{
	public abstract class RequestParams
	{
		public abstract string typePath { get; }

		public abstract void AppendParams(StringBuilder url);
	}

	public class NearbyParams : RequestParams
	{
		public double? longitude;

		public double? latitude;

		public int? radius;

		public string keyword;

		public string name;

		public string types;

		public int? minprice;

		public int? maxprice;

		public bool? opennow;

		public RankBy? rankBy;

		public string pagetoken;

		public bool? zagatselected;

		public Vector2 lnglat
		{
			get
			{
				return new Vector2((float)longitude.Value, (float)latitude.Value);
			}
			set
			{
				longitude = value.x;
				latitude = value.y;
			}
		}

		public override string typePath => "nearbysearch";

		public NearbyParams(double longitude, double latitude, int radius)
		{
			this.longitude = longitude;
			this.latitude = latitude;
			this.radius = radius;
		}

		public NearbyParams(Vector2 lnglat, int radius)
		{
			this.lnglat = lnglat;
			this.radius = radius;
		}

		public NearbyParams(string pagetoken)
		{
			this.pagetoken = pagetoken;
		}

		public override void AppendParams(StringBuilder url)
		{
			if (latitude.HasValue && longitude.HasValue)
			{
				url.Append("&location=").Append(latitude.Value).Append(",")
					.Append(longitude.Value);
			}
			if (radius.HasValue)
			{
				url.Append("&radius=").Append(radius.Value);
			}
			if (!string.IsNullOrEmpty(keyword))
			{
				url.Append("&keyword=").Append(keyword);
			}
			if (!string.IsNullOrEmpty(name))
			{
				url.Append("&name=").Append(name);
			}
			if (!string.IsNullOrEmpty(types))
			{
				url.Append("&types=").Append(types);
			}
			if (minprice.HasValue)
			{
				url.Append("&minprice=").Append(minprice.Value);
			}
			if (maxprice.HasValue)
			{
				url.Append("&maxprice=").Append(maxprice.Value);
			}
			if (opennow.HasValue)
			{
				url.Append("&opennow");
			}
			if (rankBy.HasValue)
			{
				url.Append("&rankby=").Append(rankBy.Value);
			}
			if (!string.IsNullOrEmpty(pagetoken))
			{
				url.Append("&pagetoken=").Append(RealWorldTerrainWWW.EscapeURL(pagetoken));
			}
			if (zagatselected.HasValue && zagatselected.Value)
			{
				url.Append("&zagatselected");
			}
		}
	}

	public class TextParams : RequestParams
	{
		public string query;

		public double? longitude;

		public double? latitude;

		public int? radius;

		public string language;

		public string types;

		public int? minprice;

		public int? maxprice;

		public bool? opennow;

		public string pagetoken;

		public bool? zagatselected;

		public Vector2 lnglat
		{
			get
			{
				return new Vector2((float)longitude.Value, (float)latitude.Value);
			}
			set
			{
				longitude = value.x;
				latitude = value.y;
			}
		}

		public override string typePath => "textsearch";

		public TextParams(string query)
		{
			this.query = query;
		}

		public override void AppendParams(StringBuilder url)
		{
			if (latitude.HasValue && longitude.HasValue)
			{
				url.Append("&location=").Append(latitude.Value).Append(",")
					.Append(longitude.Value);
			}
			if (radius.HasValue)
			{
				url.Append("&radius=").Append(radius.Value);
			}
			if (!string.IsNullOrEmpty(types))
			{
				url.Append("&types=").Append(types);
			}
			if (!string.IsNullOrEmpty(query))
			{
				url.Append("&query=").Append(RealWorldTerrainWWW.EscapeURL(query));
			}
			if (!string.IsNullOrEmpty(language))
			{
				url.Append("&language=").Append(language);
			}
			if (minprice.HasValue)
			{
				url.Append("&minprice=").Append(minprice.Value);
			}
			if (maxprice.HasValue)
			{
				url.Append("&maxprice=").Append(maxprice.Value);
			}
			if (opennow.HasValue && opennow.Value)
			{
				url.Append("&opennow");
			}
			if (!string.IsNullOrEmpty(pagetoken))
			{
				url.Append("&pagetoken=").Append(pagetoken);
			}
			if (zagatselected.HasValue && zagatselected.Value)
			{
				url.Append("&zagatselected");
			}
		}
	}

	public class RadarParams : RequestParams
	{
		public double? longitude;

		public double? latitude;

		public int? radius;

		public string keyword;

		public string name;

		public string types;

		public int? minprice;

		public int? maxprice;

		public bool? opennow;

		public bool? zagatselected;

		public Vector2 lnglat
		{
			get
			{
				return new Vector2((float)longitude.Value, (float)latitude.Value);
			}
			set
			{
				longitude = value.x;
				latitude = value.y;
			}
		}

		public override string typePath => "radarsearch";

		public RadarParams(double longitude, double latitude, int radius)
		{
			this.longitude = longitude;
			this.latitude = latitude;
			this.radius = radius;
		}

		public RadarParams(Vector2 lnglat, int radius)
		{
			this.lnglat = lnglat;
			this.radius = radius;
		}

		public override void AppendParams(StringBuilder url)
		{
			if (latitude.HasValue && longitude.HasValue)
			{
				url.Append("&location=").Append(latitude.Value).Append(",")
					.Append(longitude.Value);
			}
			if (radius.HasValue)
			{
				url.Append("&radius=").Append(radius.Value);
			}
			if (!string.IsNullOrEmpty(keyword))
			{
				url.Append("&keyword=").Append(keyword);
			}
			if (!string.IsNullOrEmpty(name))
			{
				url.Append("&name=").Append(name);
			}
			if (!string.IsNullOrEmpty(types))
			{
				url.Append("&types=").Append(types);
			}
			if (minprice.HasValue)
			{
				url.Append("&minprice=").Append(minprice.Value);
			}
			if (maxprice.HasValue)
			{
				url.Append("&maxprice=").Append(maxprice.Value);
			}
			if (opennow.HasValue && opennow.Value)
			{
				url.Append("&opennow");
			}
			if (zagatselected.HasValue && zagatselected.Value)
			{
				url.Append("&zagatselected");
			}
		}
	}

	public enum RankBy
	{
		prominence,
		distance
	}

	protected RealWorldTerrainGooglePlaces(string key, RequestParams p)
	{
		_status = RequestStatus.downloading;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendFormat("https://maps.googleapis.com/maps/api/place/{0}/xml?sensor=false", p.typePath);
		if (!string.IsNullOrEmpty(key))
		{
			stringBuilder.Append("&key=").Append(key);
		}
		p.AppendParams(stringBuilder);
		www = new RealWorldTerrainWWW(stringBuilder.ToString());
		RealWorldTerrainWWW realWorldTerrainWWW = www;
		realWorldTerrainWWW.OnComplete = (Action<RealWorldTerrainWWW>)Delegate.Combine(realWorldTerrainWWW.OnComplete, new Action<RealWorldTerrainWWW>(base.OnRequestComplete));
	}

	public static RealWorldTerrainGooglePlaces FindNearby(Vector2 lnglat, int radius, string key, string keyword = null, string name = null, string types = null, int minprice = -1, int maxprice = -1, bool opennow = false, RankBy rankBy = RankBy.prominence)
	{
		NearbyParams nearbyParams = new NearbyParams(lnglat, radius)
		{
			keyword = keyword,
			name = name,
			types = types
		};
		if (minprice != -1)
		{
			nearbyParams.minprice = minprice;
		}
		if (maxprice != -1)
		{
			nearbyParams.maxprice = maxprice;
		}
		if (opennow)
		{
			nearbyParams.opennow = true;
		}
		if (rankBy != RankBy.prominence)
		{
			nearbyParams.rankBy = rankBy;
		}
		return new RealWorldTerrainGooglePlaces(key, nearbyParams);
	}

	public static RealWorldTerrainGooglePlaces FindNearby(string key, NearbyParams p)
	{
		return new RealWorldTerrainGooglePlaces(key, p);
	}

	public static RealWorldTerrainGooglePlaces FindText(string query, string key, Vector2 lnglat = default(Vector2), int radius = -1, string language = null, string types = null, int minprice = -1, int maxprice = -1, bool opennow = false)
	{
		TextParams textParams = new TextParams(query)
		{
			language = language,
			types = types
		};
		if (lnglat != default(Vector2))
		{
			textParams.lnglat = lnglat;
		}
		if (radius != -1)
		{
			textParams.radius = radius;
		}
		if (minprice != -1)
		{
			textParams.minprice = minprice;
		}
		if (maxprice != -1)
		{
			textParams.maxprice = maxprice;
		}
		if (opennow)
		{
			textParams.opennow = true;
		}
		return new RealWorldTerrainGooglePlaces(key, textParams);
	}

	public static RealWorldTerrainGooglePlaces FindText(string key, TextParams p)
	{
		return new RealWorldTerrainGooglePlaces(key, p);
	}

	public static RealWorldTerrainGooglePlaces FindRadar(Vector2 lnglat, int radius, string key, string keyword = null, string name = null, string types = null, int minprice = -1, int maxprice = -1, bool opennow = false)
	{
		RadarParams radarParams = new RadarParams(lnglat, radius)
		{
			keyword = keyword,
			name = name,
			types = types
		};
		if (minprice != -1)
		{
			radarParams.minprice = minprice;
		}
		if (maxprice != -1)
		{
			radarParams.maxprice = maxprice;
		}
		if (opennow)
		{
			radarParams.opennow = true;
		}
		return new RealWorldTerrainGooglePlaces(key, radarParams);
	}

	public static RealWorldTerrainGooglePlaces FindRadar(string key, RadarParams p)
	{
		return new RealWorldTerrainGooglePlaces(key, p);
	}

	public static RealWorldTerrainPlacesResult[] GetResults(string response)
	{
		string nextPageToken;
		return GetResults(response, out nextPageToken);
	}

	public static RealWorldTerrainPlacesResult[] GetResults(string response, out string nextPageToken)
	{
		nextPageToken = null;
		try
		{
			RealWorldTerrainXML realWorldTerrainXML = RealWorldTerrainXML.Load(response);
			if (realWorldTerrainXML.Find<string>("//status") != "OK")
			{
				return null;
			}
			nextPageToken = realWorldTerrainXML.Find<string>("//next_page_token");
			RealWorldTerrainXMLList realWorldTerrainXMLList = realWorldTerrainXML.FindAll("//result");
			List<RealWorldTerrainPlacesResult> list = new List<RealWorldTerrainPlacesResult>(realWorldTerrainXMLList.count);
			foreach (RealWorldTerrainXML item in realWorldTerrainXMLList)
			{
				list.Add(new RealWorldTerrainPlacesResult(item));
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
