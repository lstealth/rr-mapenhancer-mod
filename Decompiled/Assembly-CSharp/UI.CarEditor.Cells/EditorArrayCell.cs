using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI.CarEditor.Cells;

public class EditorArrayCell : EditorNestedCell
{
	[SerializeField]
	private Button addButton;

	[SerializeField]
	private Button removeButton;

	private Action _onAdd;

	private Action _onRemove;

	private void Awake()
	{
		addButton.onClick.AddListener(delegate
		{
			_onAdd?.Invoke();
		});
		removeButton.onClick.AddListener(delegate
		{
			_onRemove?.Invoke();
		});
	}

	public void Configure(string text, int count, Action onAdd, Action onRemove)
	{
		_onAdd = onAdd;
		_onRemove = onRemove;
		Configure($"{text} [{count}]");
	}
}
