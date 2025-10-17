using System;
using System.Text;
using InfinityCode.RealWorldTerrain.ExtraTypes;
using InfinityCode.RealWorldTerrain.Webservices.Base;
using InfinityCode.RealWorldTerrain.Webservices.Results;
using InfinityCode.RealWorldTerrain.XML;

namespace InfinityCode.RealWorldTerrain.Webservices;

public class RealWorldTerrainGooglePlaceDetails : RealWorldTerrainTextWebServiceBase
{
	protected RealWorldTerrainGooglePlaceDetails(string key, string place_id, string reference, string language)
	{
		_status = RequestStatus.downloading;
		StringBuilder stringBuilder = new StringBuilder("https://maps.googleapis.com/maps/api/place/details/xml?sensor=false&key=").Append(key);
		if (!string.IsNullOrEmpty(place_id))
		{
			stringBuilder.Append("&placeid=").Append(place_id);
		}
		if (!string.IsNullOrEmpty(reference))
		{
			stringBuilder.Append("&reference=").Append(reference);
		}
		if (!string.IsNullOrEmpty(language))
		{
			stringBuilder.Append("&language=").Append(language);
		}
		www = new RealWorldTerrainWWW(stringBuilder.ToString());
		RealWorldTerrainWWW realWorldTerrainWWW = www;
		realWorldTerrainWWW.OnComplete = (Action<RealWorldTerrainWWW>)Delegate.Combine(realWorldTerrainWWW.OnComplete, new Action<RealWorldTerrainWWW>(base.OnRequestComplete));
	}

	public static RealWorldTerrainGooglePlaceDetails FindByPlaceID(string key, string place_id, string language = null)
	{
		return new RealWorldTerrainGooglePlaceDetails(key, place_id, null, language);
	}

	public static RealWorldTerrainGooglePlaceDetails FindByReference(string key, string reference, string language = null)
	{
		return new RealWorldTerrainGooglePlaceDetails(key, null, reference, language);
	}

	public static RealWorldTerrainGooglePlaceDetailsResult GetResult(string response)
	{
		try
		{
			RealWorldTerrainXML realWorldTerrainXML = RealWorldTerrainXML.Load(response);
			if (realWorldTerrainXML.Find<string>("//status") != "OK")
			{
				return null;
			}
			return new RealWorldTerrainGooglePlaceDetailsResult(realWorldTerrainXML["result"]);
		}
		catch
		{
		}
		return null;
	}
}
