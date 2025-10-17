using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Console;

public sealed class CollapsedConsole : MonoBehaviour, IConsoleChild
{
	public int maxVisible = 4;

	public float durationVisible = 4f;

	public IConsoleChildDelegate Console;

	private Coroutine _removeExpiredCoroutine;

	private readonly List<ConsoleLine> _lines = new List<ConsoleLine>();

	private void OnEnable()
	{
		LayoutLines();
		StartCoroutine(OnEnableCoroutine());
	}

	private void OnDisable()
	{
		if (_removeExpiredCoroutine != null)
		{
			StopCoroutine(_removeExpiredCoroutine);
		}
		_removeExpiredCoroutine = null;
	}

	private IEnumerator OnEnableCoroutine()
	{
		yield return null;
		_removeExpiredCoroutine = StartCoroutine(RemoveExpired());
	}

	public void WillDisable()
	{
		foreach (ConsoleLine line in _lines)
		{
			if (!(line.Label == null))
			{
				Console.Recycle(line.Label);
				line.Label = null;
			}
		}
	}

	private IEnumerator RemoveExpired()
	{
		while (true)
		{
			float num = Time.unscaledTime - durationVisible;
			int i;
			for (i = 0; i < _lines.Count && !(_lines[i].Timestamp < num); i++)
			{
			}
			bool num2 = i != _lines.Count;
			RecycleAtAndAfter(i);
			if (num2)
			{
				LayoutLines();
			}
			yield return new WaitForSeconds(0.5f);
		}
	}

	private void LayoutLines()
	{
		if (!base.isActiveAndEnabled)
		{
			return;
		}
		RecycleAtAndAfter(maxVisible);
		float num = 0f;
		for (int num2 = _lines.Count - 1; num2 >= 0; num2--)
		{
			ConsoleLine consoleLine = _lines[num2];
			Console.CreateLabelIfNeeded(consoleLine, base.transform);
			Vector3 size = consoleLine.Label.textBounds.size;
			if (size.y < 0f)
			{
				Debug.LogWarning($"textBoundsSize at {num2} is {size:F3}, skipping");
			}
			else
			{
				consoleLine.Label.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, num);
				num -= size.y;
			}
		}
	}

	public void Add(Console.Entry entry)
	{
		_lines.Insert(0, new ConsoleLine(null, entry.Text, Time.unscaledTime));
		LayoutLines();
	}

	private void RecycleAtAndAfter(int linesIndex)
	{
		for (int i = linesIndex; i < _lines.Count; i++)
		{
			ConsoleLine consoleLine = _lines[i];
			Console.Recycle(consoleLine.Label);
		}
		int num = _lines.Count - linesIndex;
		if (num > 0)
		{
			_lines.RemoveRange(linesIndex, num);
		}
	}
}
