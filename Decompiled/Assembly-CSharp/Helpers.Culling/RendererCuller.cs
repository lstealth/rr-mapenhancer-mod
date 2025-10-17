using System;
using System.Linq;
using UnityEngine;

namespace Helpers.Culling;

public class RendererCuller : MonoBehaviour, CullingManager.ICullingEventHandler
{
	private enum CullingState
	{
		Unknown,
		Visible,
		NotVisible
	}

	private enum CullingManagerName
	{
		Scenery
	}

	private CullingManager.Token _cullingToken;

	private CullingState _cullingState;

	private Renderer[] _cullRenderers;

	[SerializeField]
	private CullingManagerName cullingManagerName;

	[Range(0f, 2f)]
	[SerializeField]
	public int visibleDistanceBand = 1;

	[Range(0f, 10000f)]
	[SerializeField]
	public float radius = 10f;

	private void Awake()
	{
		_cullRenderers = (from r in base.transform.GetComponentsInChildren<Renderer>()
			where r.enabled
			select r).ToArray();
		Renderer[] cullRenderers = _cullRenderers;
		for (int num = 0; num < cullRenderers.Length; num++)
		{
			cullRenderers[num].enabled = _cullingState == CullingState.Visible;
		}
	}

	private void OnEnable()
	{
		ResetCulling();
	}

	private void OnDisable()
	{
		_cullingToken.Dispose();
		_cullingToken = null;
	}

	private void ResetCulling()
	{
		_cullingToken?.Dispose();
		if (cullingManagerName == CullingManagerName.Scenery)
		{
			CullingManager scenery = CullingManager.Scenery;
			CullingManager cullingManager = scenery;
			_cullingToken = cullingManager.AddSphere(base.transform, radius, this);
			return;
		}
		throw new ArgumentOutOfRangeException("cullingManagerName", $"Unexpected cullingManagerName: {cullingManagerName}");
	}

	public void CullingSphereStateChanged(bool isVisible, int distanceBand)
	{
		CullingState cullingState = ((isVisible && distanceBand <= visibleDistanceBand) ? CullingState.Visible : CullingState.NotVisible);
		if (cullingState != _cullingState)
		{
			_cullingState = cullingState;
			bool flag = _cullingState == CullingState.Visible;
			Renderer[] cullRenderers = _cullRenderers;
			for (int i = 0; i < cullRenderers.Length; i++)
			{
				cullRenderers[i].enabled = flag;
			}
		}
	}

	public void RequestUpdateCullingPosition()
	{
		_cullingToken.UpdatePosition(base.transform);
	}
}
