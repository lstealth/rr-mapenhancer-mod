using System;
using Helpers;
using Model.Definition.Data;
using RLD;
using UnityEngine;

namespace UI.CarEditor.DefinitionEditors;

public class SteamLocomotiveDefinitionEditor : DefinitionEditor
{
	private SteamLocomotiveDefinition _definition;

	private Func<TransformReference, (Vector3, Quaternion)> _getParentPositionRotation;

	private static Mesh _unitSegmentY;

	public void Configure(SteamLocomotiveDefinition definition, Func<TransformReference, (Vector3, Quaternion)> getParentPositionRotation)
	{
		_definition = definition;
		_getParentPositionRotation = getParentPositionRotation;
	}

	private void OnRenderObject()
	{
		if (_unitSegmentY == null)
		{
			_unitSegmentY = LineMesh.CreateLine(Vector3.zero, new Vector3(0f, 1f, 0f), Color.white);
		}
		(Vector3, Quaternion) tuple = _getParentPositionRotation(null);
		Vector3 item = tuple.Item1;
		Quaternion worldRotation = tuple.Item2;
		Vector3 worldPosition = item.GameToWorld();
		GizmoLineMaterial boxWireMaterial = Singleton<GizmoLineMaterial>.Get;
		boxWireMaterial.ResetValuesToSensibleDefaults();
		DrawRay(_definition.PositionHead, 1f, 0.75f * Color.green);
		DrawRay(_definition.PositionTail, 1f, 0.75f * Color.red);
		for (int i = 0; i < _definition.Wheelsets.Count; i++)
		{
			SteamLocomotiveDefinition.Wheelset wheelset = _definition.Wheelsets[i];
			float offset = wheelset.Offset;
			float length = wheelset.Length;
			int numberOfAxles = wheelset.NumberOfAxles;
			float num = wheelset.Diameter / 2f;
			float num2 = ((numberOfAxles <= 1) ? 0f : (length / (float)(numberOfAxles - 1)));
			for (int j = 0; j < numberOfAxles; j++)
			{
				float num3 = (float)j * num2;
				DrawWheel(offset - length / 2f + num3, num, num, 0.75f * Color.cyan);
			}
		}
		void DrawRay(float num4, float height, Color color)
		{
			boxWireMaterial.SetColor(color);
			boxWireMaterial.SetPass(0);
			Graphics.DrawMeshNow(_unitSegmentY, Matrix4x4.TRS(worldPosition + worldRotation * (Vector3.forward * num4), worldRotation, Vector3.one * height));
		}
		void DrawWheel(float num4, float height, float radius, Color color)
		{
			boxWireMaterial.SetColor(color);
			boxWireMaterial.SetPass(0);
			CapsuleMesh.DrawCircleXY(worldPosition + worldRotation * (Vector3.forward * num4 + Vector3.up * height + Vector3.left * 0.8f), Quaternion.Euler(0f, 90f, 0f) * worldRotation, radius);
		}
	}
}
