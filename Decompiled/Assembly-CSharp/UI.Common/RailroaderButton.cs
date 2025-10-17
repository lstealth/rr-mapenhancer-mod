using Helpers;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Common;

public class RailroaderButton : Button
{
	private Color _defaultTextColor = new Color(0.8509804f, 66f / 85f, 0.6509804f, 1f);

	private Color _disabledTextColor = new Color(0.8509804f, 66f / 85f, 0.6509804f, 0.5f);

	private bool? _wasInteractable;

	protected override void Awake()
	{
		base.Awake();
		if (base.interactable)
		{
			_defaultTextColor = this.TMPText().color;
			_disabledTextColor = _defaultTextColor;
			_disabledTextColor.a *= 0.5f;
		}
	}

	protected override void DoStateTransition(SelectionState state, bool instant)
	{
		base.DoStateTransition(state, instant);
		if (base.interactable != _wasInteractable)
		{
			this.TMPText().color = (base.interactable ? _defaultTextColor : _disabledTextColor);
			_wasInteractable = base.interactable;
		}
	}
}
