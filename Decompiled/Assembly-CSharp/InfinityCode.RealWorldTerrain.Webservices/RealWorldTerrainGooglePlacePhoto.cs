using System;
using System.Text;
using InfinityCode.RealWorldTerrain.ExtraTypes;
using UnityEngine;

namespace InfinityCode.RealWorldTerrain.Webservices;

public class RealWorldTerrainGooglePlacePhoto : RealWorldTerrainWebServiceBase
{
	public Action<Texture2D> OnComplete;

	private RealWorldTerrainGooglePlacePhoto(string key, string photo_reference, int? maxWidth, int? maxHeight)
	{
		StringBuilder stringBuilder = new StringBuilder("https://maps.googleapis.com/maps/api/place/photo?key=").Append(key);
		stringBuilder.Append("&photo_reference=").Append(photo_reference);
		if (maxWidth.HasValue)
		{
			stringBuilder.Append("&maxwidth=").Append(maxWidth);
		}
		if (maxHeight.HasValue)
		{
			stringBuilder.Append("&maxheight=").Append(maxHeight);
		}
		if (!maxWidth.HasValue && !maxHeight.HasValue)
		{
			stringBuilder.Append("&maxwidth=").Append(800);
		}
		www = new RealWorldTerrainWWW(stringBuilder.ToString());
		RealWorldTerrainWWW realWorldTerrainWWW = www;
		realWorldTerrainWWW.OnComplete = (Action<RealWorldTerrainWWW>)Delegate.Combine(realWorldTerrainWWW.OnComplete, new Action<RealWorldTerrainWWW>(OnRequestComplete));
	}

	private void OnRequestComplete(RealWorldTerrainWWW www)
	{
		if (www == null || !www.isDone)
		{
			return;
		}
		_status = (string.IsNullOrEmpty(www.error) ? RequestStatus.success : RequestStatus.error);
		if (OnComplete != null)
		{
			if (_status == RequestStatus.success)
			{
				Texture2D texture2D = new Texture2D(1, 1);
				www.LoadImageIntoTexture(texture2D);
				OnComplete(texture2D);
			}
			else
			{
				OnComplete(null);
			}
		}
		if (OnFinish != null)
		{
			OnFinish(this);
		}
		_status = RequestStatus.disposed;
		customData = null;
		base.www = null;
	}

	public static RealWorldTerrainGooglePlacePhoto Download(string key, string photo_reference, int? maxWidth = null, int? maxHeight = null)
	{
		return new RealWorldTerrainGooglePlacePhoto(key, photo_reference, maxWidth, maxHeight);
	}

	public override void Destroy()
	{
		if (OnDispose != null)
		{
			OnDispose(this);
		}
		www = null;
		_status = RequestStatus.disposed;
		customData = null;
		OnComplete = null;
		OnFinish = null;
	}
}
