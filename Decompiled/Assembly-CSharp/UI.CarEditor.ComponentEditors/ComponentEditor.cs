using System;
using Helpers;
using Model.Definition;
using Model.Definition.Data;
using RLD;
using Serilog;
using UnityEngine;

namespace UI.CarEditor.ComponentEditors;

public class ComponentEditor : MonoBehaviour
{
	protected Model.Definition.Component Component;

	public Action OnValueChanged;

	private ObjectTransformGizmo _moveGizmo;

	private GameObject _moveGizmoTarget;

	private Func<TransformReference, (Vector3, Quaternion)> _getParentPositionRotation;

	private void OnDisable()
	{
		if (_moveGizmo != null)
		{
			MonoSingleton<RTGizmosEngine>.Get.RemoveGizmo(_moveGizmo.Gizmo);
			_moveGizmo = null;
		}
		if (_moveGizmoTarget != null)
		{
			UnityEngine.Object.Destroy(_moveGizmoTarget);
			_moveGizmoTarget = null;
		}
	}

	private void FixedUpdate()
	{
		if (_moveGizmoTarget == null)
		{
			return;
		}
		try
		{
			var (position, rotation) = GetComponentWorldPositionRotation();
			_moveGizmoTarget.transform.position = position;
			_moveGizmoTarget.transform.rotation = rotation;
			_moveGizmo.RefreshPositionAndRotation();
		}
		catch (Exception exception)
		{
			Log.Error(exception, "Exception in FixedUpdate");
		}
	}

	public void Configure(Model.Definition.Component component, Func<TransformReference, (Vector3, Quaternion)> getParentPositionRotation)
	{
		Component = component;
		_getParentPositionRotation = getParentPositionRotation;
		_moveGizmoTarget = new GameObject("Move Target");
		var (position, rotation) = GetComponentWorldPositionRotation();
		_moveGizmoTarget.transform.position = position;
		_moveGizmoTarget.transform.rotation = rotation;
		TransformGizmoListenerRepeater transformGizmoListenerRepeater = _moveGizmoTarget.AddComponent<TransformGizmoListenerRepeater>();
		transformGizmoListenerRepeater.onTransformed = (Action<Gizmo>)Delegate.Combine(transformGizmoListenerRepeater.onTransformed, (Action<Gizmo>)delegate(Gizmo gizmo)
		{
			SetComponentPositionRotation(Component, gizmo.Transform.Position3D, gizmo.Transform.Rotation3D);
		});
		_moveGizmo = MonoSingleton<RTGizmosEngine>.Get.CreateObjectMoveGizmo();
		_moveGizmo.SetTransformSpace(GizmoSpace.Local);
		_moveGizmo.SetTargetObject(_moveGizmoTarget);
	}

	protected (Vector3, Quaternion) GetComponentWorldPositionRotation()
	{
		(Vector3, Quaternion) parentPositionRotation = GetParentPositionRotation(Component);
		Vector3 item = parentPositionRotation.Item1;
		Quaternion item2 = parentPositionRotation.Item2;
		Vector3 item3 = WorldTransformer.GameToWorld(item) + item2 * Component.Transform.Position;
		Quaternion item4 = item2 * Component.Transform.Rotation;
		return (item3, item4);
	}

	protected void SetComponentPositionRotation(Model.Definition.Component comp, Vector3 worldPosition, Quaternion worldRotation)
	{
		(Vector3, Quaternion) parentPositionRotation = GetParentPositionRotation(comp);
		Vector3 item = parentPositionRotation.Item1;
		Quaternion quaternion = Quaternion.Inverse(parentPositionRotation.Item2);
		Vector3 vector = worldPosition - WorldTransformer.GameToWorld(item);
		if (vector.magnitude > 100f)
		{
			Debug.LogError($"Local Position rejected, too large: {vector} - missing BodyTransform w/ unset motion snapshot?");
			return;
		}
		comp.Transform = new PositionRotationScale(quaternion * vector, quaternion * worldRotation, comp.Transform.Scale);
		OnValueChanged?.Invoke();
	}

	private (Vector3, Quaternion) GetParentPositionRotation(Model.Definition.Component comp)
	{
		return _getParentPositionRotation(comp.Parent);
	}
}
