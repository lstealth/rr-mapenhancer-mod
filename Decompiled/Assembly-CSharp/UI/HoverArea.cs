using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace UI;

[RequireComponent(typeof(RectTransform))]
public class HoverArea : MonoBehaviour
{
	public UnityEvent<bool> onHover;

	private RectTransform _rectTransform;

	private Coroutine _checkCoroutine;

	private void Awake()
	{
		_rectTransform = GetComponent<RectTransform>();
	}

	private void OnEnable()
	{
		_checkCoroutine = StartCoroutine(MouseCursorChecker());
	}

	private void OnDisable()
	{
		StopCoroutine(_checkCoroutine);
		_checkCoroutine = null;
	}

	private IEnumerator MouseCursorChecker()
	{
		bool wasOver = false;
		while (true)
		{
			bool flag = IsOver();
			if (flag != wasOver)
			{
				onHover?.Invoke(flag);
				wasOver = flag;
			}
			yield return new WaitForSecondsRealtime(0.1f);
		}
	}

	private bool IsOver()
	{
		Vector3 mousePosition = Input.mousePosition;
		Vector3 point = _rectTransform.InverseTransformPoint(mousePosition);
		return _rectTransform.rect.Contains(point);
	}
}
