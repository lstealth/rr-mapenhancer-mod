using Model.Definition.Components.MapMasks;
using RLD;
using UnityEngine;

namespace UI.CarEditor.ComponentEditors;

public class CircleMapMaskComponentEditor : ComponentEditor
{
	private CircleMapMaskComponent MapMaskComponent => (CircleMapMaskComponent)Component;

	private void OnRenderObject()
	{
		(Vector3, Quaternion) componentWorldPositionRotation = GetComponentWorldPositionRotation();
		Vector3 item = componentWorldPositionRotation.Item1;
		Quaternion item2 = componentWorldPositionRotation.Item2;
		GizmoLineMaterial get = Singleton<GizmoLineMaterial>.Get;
		get.ResetValuesToSensibleDefaults();
		get.SetColor(new Color(1f, 1f, 0.25f));
		get.SetPass(0);
		CircleMapMaskComponent mapMaskComponent = MapMaskComponent;
		Matrix4x4 matrix = Matrix4x4.TRS(item, item2, new Vector3(mapMaskComponent.Radius, mapMaskComponent.Radius, mapMaskComponent.Radius));
		matrix *= Matrix4x4.Rotate(Quaternion.Euler(-90f, 0f, 0f));
		Graphics.DrawMeshNow(Singleton<MeshPool>.Get.UnitWireCircleXY, matrix);
	}
}
