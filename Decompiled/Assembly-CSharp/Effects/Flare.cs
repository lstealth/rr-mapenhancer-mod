using Helpers.Culling;
using UnityEngine;
using UnityEngine.VFX;

namespace Effects;

public class Flare : MonoBehaviour, CullingManager.ICullingEventHandler
{
	public Light lightSource;

	public VisualEffect visualEffect;

	public FuseeRenderer fuseeRenderer;

	private CullingManager.Token _cullingToken;

	private Renderer[] _renderers;

	private void OnEnable()
	{
		_renderers = GetComponentsInChildren<Renderer>();
		_cullingToken = CullingManager.Flare.AddSphere(base.transform, 10f, this);
	}

	private void OnDisable()
	{
		_cullingToken?.Dispose();
	}

	public void CullingSphereStateChanged(bool isVisible, int distanceBand)
	{
		lightSource.enabled = isVisible && distanceBand <= 0;
		visualEffect.enabled = isVisible && distanceBand <= 0;
		Renderer[] renderers = _renderers;
		for (int i = 0; i < renderers.Length; i++)
		{
			renderers[i].enabled = distanceBand <= 1;
		}
		fuseeRenderer.enabled = isVisible && distanceBand <= 1;
	}

	public void RequestUpdateCullingPosition()
	{
		_cullingToken.UpdatePosition(base.transform);
	}
}
