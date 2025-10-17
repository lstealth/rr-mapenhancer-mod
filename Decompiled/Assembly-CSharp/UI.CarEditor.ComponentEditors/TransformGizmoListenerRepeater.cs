using System;
using RLD;
using UnityEngine;

namespace UI.CarEditor.ComponentEditors;

public class TransformGizmoListenerRepeater : MonoBehaviour, IRTTransformGizmoListener
{
	public Action<Gizmo> onTransformed;

	public Func<Gizmo, bool> onCanBeTransformed;

	public bool OnCanBeTransformed(Gizmo transformGizmo)
	{
		if (onCanBeTransformed == null)
		{
			return true;
		}
		return onCanBeTransformed(transformGizmo);
	}

	public void OnTransformed(Gizmo transformGizmo)
	{
		onTransformed?.Invoke(transformGizmo);
	}
}
