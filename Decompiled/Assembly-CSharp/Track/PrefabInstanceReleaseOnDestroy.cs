using UnityEngine;

namespace Track;

public class PrefabInstanceReleaseOnDestroy : MonoBehaviour
{
	private PrefabInstancer _prefabInstancer;

	private object _token;

	public void Configure(PrefabInstancer prefabInstancer, object token)
	{
		_prefabInstancer = prefabInstancer;
		_token = token;
	}

	private void OnDestroy()
	{
		_prefabInstancer.Release(_token);
		_prefabInstancer = null;
		_token = null;
	}
}
