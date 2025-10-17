using System;
using InfinityCode.RealWorldTerrain.ExtraTypes;

namespace InfinityCode.RealWorldTerrain.Webservices.Base;

public abstract class RealWorldTerrainTextWebServiceBase : RealWorldTerrainWebServiceBase
{
	public Action<string> OnComplete;

	protected string _response;

	public string response => _response;

	public override void Destroy()
	{
		if (OnDispose != null)
		{
			OnDispose(this);
		}
		www = null;
		_response = string.Empty;
		_status = RequestStatus.disposed;
		customData = null;
		OnComplete = null;
		OnFinish = null;
	}

	protected void OnRequestComplete(RealWorldTerrainWWW www)
	{
		if (www != null && www.isDone)
		{
			_status = (string.IsNullOrEmpty(www.error) ? RequestStatus.success : RequestStatus.error);
			_response = ((_status == RequestStatus.success) ? www.text : www.error);
			if (OnComplete != null)
			{
				OnComplete(_response);
			}
			if (OnFinish != null)
			{
				OnFinish(this);
			}
			_status = RequestStatus.disposed;
			_response = null;
			base.www = null;
			customData = null;
		}
	}
}
