using Model;
using UI;
using UnityEngine;

public class DroneController : MonoBehaviour, ICameraSelectable
{
	[SerializeField]
	private Transform cameraTransform;

	public Transform CameraContainer => cameraTransform;

	public Vector3 GroundPosition => cameraTransform.position;

	private void FixedUpdate()
	{
		if (GameInput.MovementInputEnabled)
		{
			float axis = Input.GetAxis("Horizontal");
			float axis2 = Input.GetAxis("Vertical");
			float num = (Input.GetKey(KeyCode.E) ? 1 : 0);
			float num2 = (Input.GetKey(KeyCode.Q) ? (-1) : 0);
			Vector3 localPosition = cameraTransform.localPosition;
			float num3 = Time.deltaTime * MovementInput.CalculateSpeedFromInput(10f, 100f, 500f);
			Vector3 normalized = new Vector3(axis, 0f, axis2).normalized;
			normalized = Quaternion.Euler(0f, base.transform.localRotation.eulerAngles.y, 0f) * normalized;
			normalized *= num3;
			base.transform.localPosition += normalized;
			Vector3 normalized2 = new Vector3(0f, num + num2, 0f).normalized;
			normalized2 *= num3;
			localPosition += normalized2;
			cameraTransform.localPosition = localPosition;
		}
	}

	public void SetSelected(bool selected, Camera maybeCamera)
	{
		base.gameObject.SetActive(selected);
	}

	public void JumpToCar(Car car)
	{
		Debug.Log("JumpToCar not implemented on DroneController");
	}

	GameObject ICameraSelectable.get_gameObject()
	{
		return base.gameObject;
	}
}
