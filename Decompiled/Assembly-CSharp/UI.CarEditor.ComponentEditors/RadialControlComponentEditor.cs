using Helpers;
using Model.Definition.Components;
using RLD;
using UnityEngine;

namespace UI.CarEditor.ComponentEditors;

public class RadialControlComponentEditor : ComponentEditor
{
	private RadialControlComponent ControlComponent => (RadialControlComponent)Component;

	private void OnRenderObject()
	{
		(Vector3, Quaternion) componentWorldPositionRotation = GetComponentWorldPositionRotation();
		Vector3 item = componentWorldPositionRotation.Item1;
		Quaternion item2 = componentWorldPositionRotation.Item2;
		GizmoLineMaterial get = Singleton<GizmoLineMaterial>.Get;
		get.ResetValuesToSensibleDefaults();
		get.SetColor(new Color(1f, 0.5f, 0.25f));
		get.SetPass(0);
		CapsuleMesh.DrawCircleXY(item, item2, ControlComponent.Radius);
		if (ControlComponent.Collider != null)
		{
			DrawCollider(ControlComponent.Collider, item, item2);
		}
	}

	private void DrawCollider(ColliderDescriptor colliderDescriptor, Vector3 worldPosition, Quaternion worldRotation)
	{
		GizmoLineMaterial get = Singleton<GizmoLineMaterial>.Get;
		get.ResetValuesToSensibleDefaults();
		get.SetColor(new Color(0.25f, 1f, 0.25f));
		get.SetPass(0);
		if (colliderDescriptor is CapsuleColliderDescriptor capsuleColliderDescriptor)
		{
			Quaternion quaternion = capsuleColliderDescriptor.Axis switch
			{
				CapsuleColliderDescriptor.ColliderAxis.X => Quaternion.Euler(0f, 0f, 90f), 
				CapsuleColliderDescriptor.ColliderAxis.Y => Quaternion.identity, 
				CapsuleColliderDescriptor.ColliderAxis.Z => Quaternion.Euler(90f, 0f, 0f), 
				_ => Quaternion.identity, 
			};
			CapsuleMesh.DrawCapsuleY(worldPosition + worldRotation * capsuleColliderDescriptor.Center, worldRotation * quaternion, capsuleColliderDescriptor.Radius, capsuleColliderDescriptor.Height);
		}
	}
}
