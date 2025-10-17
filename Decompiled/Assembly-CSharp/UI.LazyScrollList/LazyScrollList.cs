using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.LazyScrollList;

[RequireComponent(typeof(ScrollRect))]
public class LazyScrollList : MonoBehaviour
{
	public GameObject cellPrefab;

	private List<object> _data = new List<object>();

	private readonly List<ILazyScrollListCell> _visibleCells = new List<ILazyScrollListCell>();

	private CellPool<ILazyScrollListCell> _cellPool;

	private float _cellHeight;

	private ScrollRect _scrollRect;

	private RectTransform _scrollRectTransform;

	private float _lastNormalizedY;

	private float _lastScrollRectContentWidth;

	public IReadOnlyCollection<ILazyScrollListCell> VisibleCells => _visibleCells;

	private void Awake()
	{
		Prepare();
	}

	private void Update()
	{
		bool flag = Math.Abs(_lastScrollRectContentWidth - _scrollRect.content.rect.width) > 0.001f;
		float y = _scrollRect.normalizedPosition.y;
		if (!(Math.Abs(y - _lastNormalizedY) < 1E-06f) || flag)
		{
			_lastNormalizedY = y;
			if (flag)
			{
				SetData(_data);
			}
			else
			{
				RefreshList();
			}
		}
	}

	private void Prepare()
	{
		if (!(_scrollRectTransform != null))
		{
			if ((object)_scrollRect == null)
			{
				_scrollRect = GetComponent<ScrollRect>();
			}
			_scrollRectTransform = _scrollRect.GetComponent<RectTransform>();
			_cellPool = new CellPool<ILazyScrollListCell>(CreateCell, 10);
		}
	}

	public void SetData(List<object> data)
	{
		Prepare();
		RectTransform component = cellPrefab.GetComponent<RectTransform>();
		if (Vector2.Distance(component.pivot, new Vector2(0f, 1f)) > 0.001f)
		{
			Debug.LogError($"Cell prefab {cellPrefab.name} has unexpected pivot: {component.pivot:F3}", cellPrefab);
		}
		_cellHeight = Mathf.Max(1f, component.rect.height);
		_data = data;
		float y = (float)data.Count * _cellHeight;
		_scrollRect.content.sizeDelta = new Vector2(0f, y);
		for (int num = _visibleCells.Count - 1; num >= 0; num--)
		{
			ILazyScrollListCell obj = _visibleCells[num];
			_cellPool.ReturnObject(obj);
		}
		_visibleCells.Clear();
		RefreshList();
	}

	private void RefreshList()
	{
		GetVisibleCellRange(out var startIndex, out var endIndex);
		for (int num = _visibleCells.Count - 1; num >= 0; num--)
		{
			ILazyScrollListCell lazyScrollListCell = _visibleCells[num];
			int listIndex = lazyScrollListCell.ListIndex;
			if (startIndex > listIndex || listIndex > endIndex)
			{
				_cellPool.ReturnObject(lazyScrollListCell);
				_visibleCells.RemoveAt(num);
			}
		}
		float width = _scrollRect.content.rect.width;
		for (int i = startIndex; i <= endIndex; i++)
		{
			if (!HasVisibleCell(i))
			{
				ILazyScrollListCell lazyScrollListCell2 = _cellPool.GetObject();
				_visibleCells.Add(lazyScrollListCell2);
				RectTransform rectTransform = lazyScrollListCell2.RectTransform;
				rectTransform.anchoredPosition = new Vector2(0f, (float)(-i) * _cellHeight);
				Vector2 sizeDelta = rectTransform.sizeDelta;
				sizeDelta.x = width;
				rectTransform.sizeDelta = sizeDelta;
				lazyScrollListCell2.Configure(i, _data[i]);
			}
		}
		_lastScrollRectContentWidth = width;
	}

	private void GetVisibleCellRange(out int startIndex, out int endIndex)
	{
		GetStartEndIndexFloat(out var startIndex2, out var endIndex2);
		startIndex = Mathf.Max(0, Mathf.FloorToInt(startIndex2 - 1f));
		endIndex = Mathf.Min(_data.Count - 1, Mathf.CeilToInt(endIndex2 + 1f));
	}

	private void GetStartEndIndexFloat(out float startIndex, out float endIndex)
	{
		if (_cellHeight == 0f)
		{
			startIndex = 0f;
			endIndex = -1f;
			return;
		}
		float y = _scrollRect.content.anchoredPosition.y;
		float num = _scrollRectTransform.rect.height / _cellHeight;
		startIndex = Mathf.Max(y / _cellHeight, 0f);
		endIndex = Mathf.Min(startIndex + num, _data.Count);
	}

	public void ScrollToCell(int index)
	{
		GetStartEndIndexFloat(out var startIndex, out var endIndex);
		float num;
		if ((float)index < startIndex)
		{
			num = 0f - ((float)index - startIndex);
		}
		else
		{
			if (!((float)index > endIndex - 1f))
			{
				return;
			}
			num = Mathf.Floor(endIndex) - (float)index;
		}
		float num2 = num * _cellHeight;
		float height = _scrollRect.content.rect.height;
		float height2 = _scrollRect.viewport.rect.height;
		float num3 = num2 / (height - height2);
		_scrollRect.verticalNormalizedPosition = Mathf.Clamp01(_scrollRect.verticalNormalizedPosition + num3);
	}

	private bool HasVisibleCell(int i)
	{
		foreach (ILazyScrollListCell visibleCell in _visibleCells)
		{
			if (visibleCell.ListIndex == i)
			{
				return true;
			}
		}
		return false;
	}

	private ILazyScrollListCell CreateCell()
	{
		return UnityEngine.Object.Instantiate(cellPrefab, _scrollRect.content).GetComponent<ILazyScrollListCell>();
	}
}
