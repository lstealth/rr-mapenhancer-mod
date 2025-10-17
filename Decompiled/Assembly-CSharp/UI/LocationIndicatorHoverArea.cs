using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI;

public class LocationIndicatorHoverArea : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, IPointerClickHandler
{
	private LocationIndicatorController _controller;

	public readonly List<LocationIndicatorController.Descriptor> descriptors = new List<LocationIndicatorController.Descriptor>();

	public readonly List<string> spanIds = new List<string>();

	private readonly List<string> _addedTokens = new List<string>();

	private readonly List<string> _segmentTokens = new List<string>();

	public event Action OnClick;

	private void Awake()
	{
		_controller = LocationIndicatorController.Shared;
	}

	private void OnDestroy()
	{
		Cleanup();
	}

	private void OnDisable()
	{
		Cleanup();
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		foreach (LocationIndicatorController.Descriptor descriptor in descriptors)
		{
			_addedTokens.Add(_controller.Add(descriptor));
		}
		if (spanIds.Count > 0)
		{
			_segmentTokens.Add(SegmentIndicatorController.Shared.Add(spanIds));
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		Cleanup();
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		this.OnClick?.Invoke();
	}

	private void Cleanup()
	{
		foreach (string addedToken in _addedTokens)
		{
			_controller.Remove(addedToken);
		}
		_addedTokens.Clear();
		foreach (string segmentToken in _segmentTokens)
		{
			SegmentIndicatorController.Shared.Remove(segmentToken);
		}
		_segmentTokens.Clear();
	}
}
