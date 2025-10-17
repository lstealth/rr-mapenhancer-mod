using UnityEngine;

[AddComponentMenu("Camera/Simple Smooth Mouse Look")]
public class SimpleSmoothMouseLook : MonoBehaviour
{
	private Vector2 _mouseAbsolute;

	private Vector2 _smoothMouse;

	public Vector2 clampInDegrees = new Vector2(360f, 180f);

	public Vector2 sensitivity = new Vector2(2f, 2f);

	public Vector2 smoothing = new Vector2(3f, 3f);

	public Vector2 targetDirection;

	public Vector2 targetCharacterDirection;

	public GameObject characterBody;

	private void Start()
	{
		Synchronize();
	}

	public void Synchronize()
	{
		targetDirection = base.transform.localRotation.eulerAngles;
		if ((bool)characterBody)
		{
			targetCharacterDirection = characterBody.transform.localRotation.eulerAngles;
		}
		_mouseAbsolute = default(Vector2);
		_smoothMouse = default(Vector2);
	}

	private void Update()
	{
		if (Input.GetMouseButton(1))
		{
			Quaternion quaternion = Quaternion.Euler(targetDirection);
			Quaternion quaternion2 = Quaternion.Euler(targetCharacterDirection);
			Vector2 a = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
			a = Vector2.Scale(a, new Vector2(sensitivity.x * smoothing.x, sensitivity.y * smoothing.y));
			_smoothMouse.x = Mathf.Lerp(_smoothMouse.x, a.x, 1f / smoothing.x);
			_smoothMouse.y = Mathf.Lerp(_smoothMouse.y, a.y, 1f / smoothing.y);
			_mouseAbsolute += _smoothMouse;
			if (clampInDegrees.x < 360f)
			{
				_mouseAbsolute.x = Mathf.Clamp(_mouseAbsolute.x, (0f - clampInDegrees.x) * 0.5f, clampInDegrees.x * 0.5f);
			}
			if (clampInDegrees.y < 360f)
			{
				_mouseAbsolute.y = Mathf.Clamp(_mouseAbsolute.y, (0f - clampInDegrees.y) * 0.5f, clampInDegrees.y * 0.5f);
			}
			base.transform.localRotation = Quaternion.AngleAxis(0f - _mouseAbsolute.y, quaternion * Vector3.right) * quaternion;
			if ((bool)characterBody)
			{
				Quaternion quaternion3 = Quaternion.AngleAxis(_mouseAbsolute.x, Vector3.up);
				characterBody.transform.localRotation = quaternion3 * quaternion2;
			}
			else
			{
				Quaternion quaternion4 = Quaternion.AngleAxis(_mouseAbsolute.x, base.transform.InverseTransformDirection(Vector3.up));
				base.transform.localRotation *= quaternion4;
			}
		}
	}
}
