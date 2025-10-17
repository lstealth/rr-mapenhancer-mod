using Helpers;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Tags;

public class TagCallout : MonoBehaviour
{
	public Callout callout;

	public RectTransform canvasRectTransform;

	public Image[] colorImages;

	public LocationIndicatorHoverArea locationIndicatorHoverArea;

	public float canvasScale = 1f;

	public float yOffset = 5f;

	private Camera _camera;

	private Rigidbody _rigidbody;

	private void Awake()
	{
		_rigidbody = base.gameObject.AddKinematicRigidbody();
	}

	private void Update()
	{
		UpdateRotationAndScale();
	}

	public void SetPosition(Vector3 worldPosition, bool immediate = false)
	{
		if (immediate)
		{
			base.transform.position = worldPosition;
			_rigidbody.position = worldPosition;
			UpdateRotationAndScale();
		}
		else
		{
			_rigidbody.MovePosition(worldPosition);
		}
	}

	private void UpdateRotationAndScale()
	{
		if (_camera == null)
		{
			_camera = Camera.main;
		}
		if (!(_camera == null))
		{
			float y = Quaternion.LookRotation(_camera.transform.position - canvasRectTransform.position).eulerAngles.y;
			canvasRectTransform.rotation = Quaternion.Euler(0f, y, 0f);
			canvasRectTransform.localPosition = Vector3.up * yOffset;
			canvasRectTransform.localScale = canvasScale * Vector3.one;
		}
	}
}
