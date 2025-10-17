using System.Collections.Generic;
using Core;
using UnityEngine;

public class SDFBezierDemo : MonoBehaviour
{
	private Material _material;

	private void Awake()
	{
		MeshRenderer component = GetComponent<MeshRenderer>();
		_material = new Material(component.sharedMaterial);
		_material.hideFlags = HideFlags.DontSave;
		component.sharedMaterial = _material;
		SetupShader();
	}

	[ContextMenu("Set Shader Uniforms")]
	public void SetupShader()
	{
		List<BezierCurve> list = new List<BezierCurve>();
		float y = 0.1f;
		float y2 = 0.5f;
		float y3 = 0.9f;
		list.Add(new BezierCurve(new Vector3(0.1f, y, 0.1f), new Vector3(0.3f, y, 0.3f), new Vector3(0.5f, y2, 0.2f), new Vector3(0.5f, y2, 0.5f), Vector3.up, Vector3.up));
		list.Add(new BezierCurve(new Vector3(0.5f, y2, 0.5f), new Vector3(0.5f, y2, 0.7f), new Vector3(0.7f, y3, 0.9f), new Vector3(0.9f, y3, 0.9f), Vector3.up, Vector3.up));
		Vector4[] array = new Vector4[list.Count * 4];
		for (int i = 0; i < list.Count; i++)
		{
			BezierCurve bezierCurve = list[i];
			array[i * 4] = bezierCurve.P0;
			array[i * 4 + 1] = bezierCurve.P1;
			array[i * 4 + 2] = bezierCurve.P2;
			array[i * 4 + 3] = bezierCurve.P3;
		}
		_material.SetVectorArray("_Curves", array);
		_material.SetInt("_CurvesCount", list.Count);
	}
}
