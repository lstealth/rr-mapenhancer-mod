using UnityEngine;

namespace UI;

public class BillboardLocationIndicator : MonoBehaviour
{
	public Callout callout;

	public Canvas canvas;

	private Camera _camera;

	private RectTransform _rectTransform;

	private void Awake()
	{
		_camera = Camera.main;
		canvas.transform.localEulerAngles = new Vector3(0f, 180f, 0f);
		_rectTransform = canvas.GetComponent<RectTransform>();
	}

	public void UpdatePosition()
	{
		Vector3 position = _camera.transform.position;
		Vector3 vector = position - base.transform.position;
		vector.x = (vector.z = 0f);
		base.transform.LookAt(position - vector);
		float magnitude = (position - canvas.transform.position).magnitude;
		float num = Mathf.Lerp(0.03f, 0.1f, Mathf.Clamp01(Mathf.InverseLerp(20f, 100f, magnitude)));
		float num2 = Mathf.Lerp(7f, 20f, Mathf.Clamp01(Mathf.InverseLerp(20f, 200f, magnitude)));
		_rectTransform.localScale = new Vector3(num, num, 1f);
		_rectTransform.localPosition = Vector3.up * num2;
	}
}
