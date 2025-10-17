using Serilog;
using UnityEngine;

namespace Helpers;

[DefaultExecutionOrder(-1000)]
public class WorldTransformerTarget : MonoBehaviour
{
	private void Awake()
	{
		WorldTransformerTargetList.Targets.Add(base.transform);
		Transform transform = base.transform;
		Vector3 vector = transform.position.GameToWorld();
		if (vector != transform.position)
		{
			Log.Debug("WorldTransformerTarget Catch-up {name}", base.name);
			transform.position = vector;
		}
	}

	private void OnDestroy()
	{
		WorldTransformerTargetList.Targets.Remove(base.transform);
	}
}
