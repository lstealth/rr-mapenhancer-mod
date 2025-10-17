using System;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Common;

[RequireComponent(typeof(RectTransform))]
public class NavigationController : MonoBehaviour
{
	private struct StackFrame
	{
		public readonly INavigationView NavigationView;

		public readonly Vector2 Size;

		public StackFrame(INavigationView navigationView)
		{
			NavigationView = navigationView;
			Size = navigationView.RectTransform.rect.size;
		}
	}

	private readonly List<StackFrame> _stack = new List<StackFrame>();

	private RectTransform _rectTransform;

	private void Awake()
	{
		_rectTransform = GetComponent<RectTransform>();
	}

	private void Transition(StackFrame from, StackFrame to, Action onComplete)
	{
		CanvasGroup canvasGroup = PreparedCanvasGroup(from.NavigationView.RectTransform.gameObject);
		CanvasGroup toCanvasGroup = PreparedCanvasGroup(to.NavigationView.RectTransform.gameObject);
		canvasGroup.alpha = 1f;
		canvasGroup.interactable = false;
		toCanvasGroup.alpha = 0f;
		toCanvasGroup.interactable = false;
		toCanvasGroup.gameObject.SetActive(value: true);
		LeanTween.alphaCanvas(canvasGroup, 0f, 0.075f).setOnComplete((Action)delegate
		{
			onComplete();
			LeanTween.alphaCanvas(toCanvasGroup, 1f, 0.075f).setOnComplete((Action)delegate
			{
				toCanvasGroup.interactable = true;
			});
		});
		LeanTween.value(base.gameObject, _rectTransform.rect.size, to.Size, 0.15f).setEaseInOutCubic().setOnUpdate(delegate(Vector2 size)
		{
			_rectTransform.SetSizeWithCurrentAnchors(size);
		});
	}

	public void Push(INavigationView view)
	{
		StackFrame stackFrame = new StackFrame(view);
		_stack.Add(stackFrame);
		view.RectTransform.SetParent(_rectTransform, worldPositionStays: false);
		view.WillAppear();
		view.RectTransform.gameObject.SetActive(value: true);
		view.RectTransform.SetFrameFillParent();
		if (_stack.Count >= 2)
		{
			List<StackFrame> stack = _stack;
			StackFrame last = stack[stack.Count - 2];
			last.NavigationView.WillDisappear();
			Transition(last, stackFrame, delegate
			{
				last.NavigationView.RectTransform.gameObject.SetActive(value: false);
			});
		}
		else
		{
			_rectTransform.SetSizeWithCurrentAnchors(stackFrame.Size);
		}
	}

	public void Pop()
	{
		if (_stack.Count > 1)
		{
			List<StackFrame> stack = _stack;
			StackFrame last = stack[stack.Count - 1];
			last.NavigationView.WillDisappear();
			List<StackFrame> stack2 = _stack;
			StackFrame to = stack2[stack2.Count - 2];
			to.NavigationView.WillAppear();
			Transition(last, to, delegate
			{
				_stack.RemoveAt(_stack.Count - 1);
				last.NavigationView.DidPop();
				UnityEngine.Object.Destroy(last.NavigationView.RectTransform.gameObject);
			});
		}
	}

	private CanvasGroup PreparedCanvasGroup(GameObject go)
	{
		CanvasGroup component = go.GetComponent<CanvasGroup>();
		if (component == null)
		{
			return go.AddComponent<CanvasGroup>();
		}
		return component;
	}
}
