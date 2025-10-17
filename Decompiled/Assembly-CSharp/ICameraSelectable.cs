using UnityEngine;

public interface ICameraSelectable
{
	GameObject gameObject { get; }

	Transform CameraContainer { get; }

	Vector3 GroundPosition { get; }

	void SetSelected(bool selected, Camera camera);
}
