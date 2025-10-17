using System.Collections.Generic;
using Helpers.Culling;
using UnityEngine;

namespace Track.Signals.Panel;

public class CTCPanelCuller : MonoBehaviour, CullingManager.ICullingEventHandler
{
	private CullingManager.Token _cullingToken;

	private HashSet<Renderer> _cullRenderers;

	private HashSet<Canvas> _cullCanvases;

	private void Awake()
	{
		_cullRenderers = new HashSet<Renderer>();
		_cullRenderers.UnionWith(GetComponentsInChildren<Renderer>());
		_cullCanvases = new HashSet<Canvas>();
		_cullCanvases.UnionWith(GetComponentsInChildren<Canvas>());
	}

	private void OnEnable()
	{
		_cullingToken = CullingManager.CTC.AddSphere(base.transform, 10f, this);
	}

	private void OnDisable()
	{
		_cullingToken?.Dispose();
	}

	public void CullingSphereStateChanged(bool isVisible, int distanceBand)
	{
		bool flag = isVisible && distanceBand < 1;
		foreach (Renderer cullRenderer in _cullRenderers)
		{
			cullRenderer.enabled = flag;
		}
		foreach (Canvas cullCanvase in _cullCanvases)
		{
			cullCanvase.enabled = flag;
		}
	}

	public void RequestUpdateCullingPosition()
	{
		_cullingToken.UpdatePosition(base.transform);
	}
}
