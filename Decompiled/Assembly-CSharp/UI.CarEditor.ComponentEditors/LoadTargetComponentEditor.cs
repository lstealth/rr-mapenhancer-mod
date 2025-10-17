using Helpers;
using Model.Definition.Components;
using RLD;
using UnityEngine;

namespace UI.CarEditor.ComponentEditors;

public class LoadTargetComponentEditor : ComponentEditor
{
	private LoadTargetComponent LoadTargetComponent => (LoadTargetComponent)Component;

	private void OnRenderObject()
	{
		(Vector3, Quaternion) componentWorldPositionRotation = GetComponentWorldPositionRotation();
		Vector3 item = componentWorldPositionRotation.Item1;
		Quaternion item2 = componentWorldPositionRotation.Item2;
		GizmoLineMaterial get = Singleton<GizmoLineMaterial>.Get;
		get.ResetValuesToSensibleDefaults();
		get.SetColor(new Color(1f, 0.5f, 0.25f));
		get.SetPass(0);
		float radius = LoadTargetComponent.Radius;
		CapsuleMesh.DrawCapsuleY(item, item2, radius, radius * 2f);
	}
}
