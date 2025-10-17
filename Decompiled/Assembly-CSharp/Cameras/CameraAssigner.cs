using UnityEngine;

namespace Cameras;

public class CameraAssigner : MonoBehaviour
{
	public CameraSelector.CameraIdentifier cameraIdentifier;

	private void Awake()
	{
		ICameraSelectable component = GetComponent<ICameraSelectable>();
		CameraSelector.shared.SetCamera(cameraIdentifier, component);
	}
}
