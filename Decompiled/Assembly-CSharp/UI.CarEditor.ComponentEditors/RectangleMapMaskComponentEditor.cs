using Model.Definition.Components.MapMasks;
using RLD;
using UnityEngine;

namespace UI.CarEditor.ComponentEditors;

public class RectangleMapMaskComponentEditor : ComponentEditor
{
	private RectangleMapMaskComponent MapMaskComponent => (RectangleMapMaskComponent)Component;

	private void OnRenderObject()
	{
		(Vector3, Quaternion) componentWorldPositionRotation = GetComponentWorldPositionRotation();
		Vector3 item = componentWorldPositionRotation.Item1;
		Quaternion item2 = componentWorldPositionRotation.Item2;
		GizmoLineMaterial get = Singleton<GizmoLineMaterial>.Get;
		get.ResetValuesToSensibleDefaults();
		get.SetColor(new Color(1f, 1f, 0.25f));
		get.SetPass(0);
		RectangleMapMaskComponent mapMaskComponent = MapMaskComponent;
		Graphics.DrawMeshNow(Singleton<MeshPool>.Get.UnitWireBox, Matrix4x4.TRS(item, item2, new Vector3(mapMaskComponent.Size.x, 0f, mapMaskComponent.Size.y)));
	}
}
