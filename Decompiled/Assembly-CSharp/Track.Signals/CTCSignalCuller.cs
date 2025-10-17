using System.Collections.Generic;
using Helpers.Culling;
using UnityEngine;

namespace Track.Signals;

public class CTCSignalCuller : MonoBehaviour, CullingManager.ICullingEventHandler
{
	public Transform[] models;

	private CullingManager.Token _cullingToken;

	private HashSet<Renderer> _cullRenderers;

	private void OnEnable()
	{
		_cullingToken = CullingManager.Signal.AddSphere(base.transform, 10f, this);
	}

	private void OnDisable()
	{
		_cullingToken?.Dispose();
	}

	public void CullingSphereStateChanged(bool isVisible, int distanceBand)
	{
		if (_cullRenderers == null)
		{
			_cullRenderers = new HashSet<Renderer>();
			Transform[] array = models;
			foreach (Transform transform in array)
			{
				_cullRenderers.UnionWith(transform.GetComponentsInChildren<Renderer>());
			}
		}
		bool flag = isVisible && distanceBand < 1;
		foreach (Renderer cullRenderer in _cullRenderers)
		{
			cullRenderer.enabled = flag;
		}
	}

	public void RequestUpdateCullingPosition()
	{
		_cullingToken.UpdatePosition(base.transform);
	}
}
