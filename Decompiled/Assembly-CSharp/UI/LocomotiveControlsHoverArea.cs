using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core;
using Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI;

[RequireComponent(typeof(RectTransform))]
public class LocomotiveControlsHoverArea : MonoBehaviour
{
	public Image backgroundImage;

	public TMP_Text hoverText;

	private Color _backgroundImageColor0;

	private RectTransform _rectTransform;

	private Coroutine _checkCoroutine;

	private void Awake()
	{
		_rectTransform = GetComponent<RectTransform>();
		_backgroundImageColor0 = backgroundImage.color;
		backgroundImage.color = Color.clear;
	}

	private void OnEnable()
	{
		hoverText.text = null;
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
			bool isOver = IsOver();
			if (isOver != wasOver)
			{
				Color target = (isOver ? _backgroundImageColor0 : Color.clear);
				for (float i = 0f; i < 0.5f; i += Time.deltaTime)
				{
					backgroundImage.color = Color.Lerp(backgroundImage.color, target, 0.2f);
					yield return null;
				}
				backgroundImage.color = target;
				wasOver = isOver;
				if (isOver)
				{
					hoverText.text = SummaryText();
				}
				else
				{
					hoverText.text = null;
				}
			}
			yield return new WaitForSecondsRealtime(0.1f);
		}
	}

	public static string SummaryText()
	{
		List<Car> list = TrainController.Shared.SelectedTrain.ToList();
		int count = list.Count;
		int num = CalculateTonnage(list);
		int num2 = Mathf.CeilToInt(CalculateLengthInMeters(list) * 3.28084f);
		return string.Format("{0}, {1:N0} tons, {2:N0} ft", count.Pluralize("car"), num, num2);
	}

	private static float CalculateLengthInMeters(List<Car> cars)
	{
		float num = 0f;
		foreach (Car car in cars)
		{
			num += car.carLength;
		}
		return num + 1f * (float)(cars.Count - 1);
	}

	private static int CalculateTonnage(IEnumerable<Car> cars)
	{
		int num = 0;
		foreach (Car car in cars)
		{
			num += Mathf.CeilToInt(car.Weight / 2000f);
		}
		return num;
	}

	private bool IsOver()
	{
		Vector3 mousePosition = Input.mousePosition;
		Vector3 point = _rectTransform.InverseTransformPoint(mousePosition);
		return _rectTransform.rect.Contains(point);
	}
}
