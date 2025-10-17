using System;
using InfinityCode.RealWorldTerrain.ExtraTypes;

namespace InfinityCode.RealWorldTerrain.Webservices;

public abstract class RealWorldTerrainWebServiceBase
{
	public enum RequestStatus
	{
		downloading,
		success,
		error,
		disposed
	}

	public Action<RealWorldTerrainWebServiceBase> OnDispose;

	public Action<RealWorldTerrainWebServiceBase> OnFinish;

	public object customData;

	protected RequestStatus _status;

	protected RealWorldTerrainWWW www;

	public RequestStatus status => _status;

	public abstract void Destroy();
}
